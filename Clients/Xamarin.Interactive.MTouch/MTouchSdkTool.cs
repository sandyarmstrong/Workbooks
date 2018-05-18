//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

using Xamarin.Interactive.I18N;

namespace Xamarin.Interactive.MTouch
{
    public class MlaunchNotFoundException : Exception {}

    public static class MTouchSdkTool
    {
        const string TAG = nameof (MTouchSdkTool);

        const string XamarinStudioMlaunchPath = "/Applications/Xamarin Studio.app/Contents/Resources/lib/monodevelop/" +
            "AddIns/MonoDevelop.IPhone/mlaunch.app/Contents/MacOS/mlaunch";
        const string XamariniOSMlaunchPath = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current/bin/mlaunch";
        const string DefaultSdkRoot = "/Applications/Xcode.app";

        public static readonly Version RequiredMinimumXcodeVersion = new Version (9, 0);

        // These are in order of preference.
        static readonly string [] MlaunchPaths = {
            XamariniOSMlaunchPath,
            XamarinStudioMlaunchPath
        };

        public static string GetMlaunchPath ()
        {
            foreach (var mlaunchPath in MlaunchPaths)
                if (File.Exists (mlaunchPath))
                    return mlaunchPath;
            throw new MlaunchNotFoundException ();
        }

        static Task<string> RunToolAsync (string fileName, string arguments, int timeout = 5000)
        {
            var tcs = new TaskCompletionSource<string> ();

            var proc = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            proc.Exited += (o, args) => {
                Logging.Log.Info ("RunToolAsync", $"{fileName} exited");
                if (proc.ExitCode == 0) {
                    try {
                        tcs.TrySetResult (proc.StandardOutput.ReadToEnd ());
                    } catch (Exception e) {
                        tcs.TrySetException (e);
                    }
                } else
                    tcs.TrySetException (new Exception ($"'{fileName} {arguments}' exited with exit code {proc.ExitCode}"));
            };

            Logging.Log.Info ("RunToolAsync", $"{fileName} {arguments}");
            proc.Start ();

            if (timeout > 0) {
                Task.Run (() =>  {
                    if (!proc.WaitForExit (timeout)) {
                        Logging.Log.Info ("RunToolAsync", $"TIMEOUT {fileName} {arguments}");
                        tcs.TrySetException (new TimeoutException ());
                        proc.Kill ();
                    }
                });
            }

            return tcs.Task;
        }

        static async Task<string> RunToolWithRetriesAsync (
            string fileName,
            string arguments,
            int timeoutRetries = 3,
            int timeout = 5000)
        {
            for (var i = 0; i < timeoutRetries; i++) {
                try {
                    return await RunToolAsync (fileName, arguments, timeout);
                } catch (TimeoutException e) {
                    if (i < (timeoutRetries - 1)) {
                        Logging.Log.Info ("RunToolWithRetriesAsync", $"Failed with {e.GetType ()}, retrying");
                        Console.Error.WriteLine (e);
                    } else
                        throw e;
                }
            }
            throw new Exception ($"Giving up on {fileName} after ${timeoutRetries} timeouts");
        }

        public static async Task<string> GetXcodeSdkRootAsync ()
        {
            // Mimicking VSmac behavior, xcode-select is checked last
            var sdkRoot = (await GetXamarinStudioXcodeSdkRootAsync ()) ?? GetDefaultXCodeSdkRoot () ?? (await GetXcodeSelectXcodeSdkRootAsync ());
            if (sdkRoot == null)
                throw new Exception (Catalog.SharedStrings.XcodeNotFoundMessage);
            return sdkRoot;
        }

        public static async Task<Version> GetXcodeVersionAsync (string sdkRoot)
        {
            try {
                var plistXml = await RunToolWithRetriesAsync (
                    "plutil",
                    $"-extract CFBundleShortVersionString xml1 \"{sdkRoot}/Contents/Info.plist\" -o -");

                var doc = new XmlDocument ();
                doc.LoadXml (plistXml);
                var shortVersion = doc.SelectSingleNode ("/plist/string/text()")?.Value;

                return Version.Parse (shortVersion);
            } catch (Exception e) {
                Console.Error.WriteLine (e);
                return null;
            }
        }

        static string GetDefaultXCodeSdkRoot ()
            => Directory.Exists (DefaultSdkRoot) ? DefaultSdkRoot : null;

        static async Task<string> GetXcodeSelectXcodeSdkRootAsync ()
        {
            try {
                var path = await RunToolWithRetriesAsync ("xcode-select", "-p");

                while (path != null && Path.GetExtension (path) != ".app")
                    path = Path.GetDirectoryName (path);

                if (Directory.Exists (path))
                    return path;
            } catch (Exception e) {
                Console.Error.WriteLine (e);
            }

            return null;
        }

        static async Task<string> GetXamarinStudioXcodeSdkRootAsync ()
        {
            var settingsPlistPath = Path.Combine (
                Environment.GetFolderPath (Environment.SpecialFolder.Personal),
                "Library",
                "Preferences",
                "Xamarin",
                "Settings.plist");

            if (!File.Exists (settingsPlistPath))
                return null;

            try {
                var plistXml = await RunToolWithRetriesAsync (
                    "plutil",
                    $"-extract AppleSdkRoot xml1 \"{settingsPlistPath}\" -o -");

                var doc = new XmlDocument ();
                doc.LoadXml (plistXml);
                var path = doc.SelectSingleNode ("/plist/string/text()")?.Value;

                if (Directory.Exists (path))
                    return path;
            } catch (Exception e) {
                Logging.Log.Error ("GetXamarinStudioXcodeSdkRootAsync", e);
                Console.Error.WriteLine (e);
            }

            return null;
        }

        public static Task<MTouchListSimXml> MtouchListSimAsync (string sdkRoot)
        {
            if (sdkRoot == null)
                throw new ArgumentNullException (nameof (sdkRoot));

            var mlaunchPath = GetMlaunchPath ();
            var taskSource = new TaskCompletionSource<MTouchListSimXml> ();

            var tmpFile = Path.GetTempFileName ();
            var sdkRootArgs = $"-sdkroot \"{sdkRoot}\"";

            var mtouchProc = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = mlaunchPath,
                    Arguments = $"{sdkRootArgs} --listsim=\"{tmpFile}\"",
                },
                EnableRaisingEvents = true,
            };

            mtouchProc.Exited += (o, args) => {
                try {
                    if (mtouchProc.ExitCode != 0) {
                        taskSource.TrySetException (new Exception ("mlaunch failed"));
                        return;
                    }

                    var x = new XmlSerializer (typeof (MTouchListSimXml));
                    var mtouchInfo = x.Deserialize (File.OpenRead (tmpFile)) as MTouchListSimXml;
                    File.Delete (tmpFile);

                    taskSource.TrySetResult (mtouchInfo);
                } catch (Exception e) {
                    taskSource.TrySetException (e);
                }
            };

            mtouchProc.Start ();
            return taskSource.Task;
        }

        public static IEnumerable<MTouchListSimXml.SimDeviceElement> GetCompatibleDevices (MTouchListSimXml mtouchInfo)
        {
            if (mtouchInfo == null)
                throw new ArgumentNullException (nameof (mtouchInfo));

            var simInfo = mtouchInfo.Simulator;

            var iOSRuntime = simInfo.SupportedRuntimes
                .LastOrDefault (r => r.Name.StartsWith ("iOS", StringComparison.OrdinalIgnoreCase));

            return
                from d in simInfo.AvailableDevices
                where d.SimRuntime == iOSRuntime.Identifier
                join t in simInfo.SupportedDeviceTypes on d.SimDeviceType equals t.Identifier
                where t.ProductFamilyId == "IPhone"
                where t.Supports64Bits
                select d;
        }
    }
}