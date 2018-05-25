//
// Authors:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Interactive.Core;
using Xamarin.ProcessControl;

namespace Xamarin.Interactive.Client.Windows
{
    class ClientServerService
    {
        static ClientServerService sharedInstance;
        public static ClientServerService SharedInstance {
            get {
                if (sharedInstance == null)
                    sharedInstance = new ClientServerService ();
                return sharedInstance;
            }
        }

        readonly Task<Uri> serverLaunchTask;

        ClientServerService ()
        {
            serverLaunchTask = LaunchServerAsync ();
        }

        public Task<Uri> GetUriAsync () => serverLaunchTask;

        Task<Uri> LaunchServerAsync ()
        {
            var tcs = new TaskCompletionSource<Uri> ();

            // TODO: Add to InteractiveInstallation
            FilePath serverAssembly = @"C:\Users\sandy\xam-git\workbooks\Clients\Xamarin.Interactive.Client.Web\bin\Debug\netcoreapp2.0\workbooks-server.dll";

            Uri clientServerUri = null;
            var clientServerLaunched = false;

            void HandleOutput (ConsoleRedirection.Segment segment)
            {
                if (segment.FileDescriptor != ConsoleRedirection.FileDescriptor.Output)
                    return;
                // TODO: Remove. This is just for testing convenience right now.
                Console.Write (segment.Data);

                // TODO: Is this ever localized?
                if (clientServerUri == null) {
                    const string nowListening = "Now listening on: ";
                    var i = segment.Data.IndexOf (nowListening);
                    if (i < 0)
                        return;
                    var url = segment
                        .Data
                        .Substring (i + nowListening.Length)
                        .Split ('\n') [0]
                        .Trim ();
                    clientServerUri = new Uri (url);
                }

                // TODO: Is this ever localized?
                const string applicationStarted = "Application started.";
                clientServerLaunched = clientServerLaunched ||
                    (clientServerUri != null && segment.Data.Contains (applicationStarted));

                if (clientServerLaunched)
                    Task.Delay (500).ContinueWith (t => {
                        tcs.TrySetResult (clientServerUri);
                    });
            }

            // TODO: Support launching packaged server
            var exec = new Exec (
                ProcessArguments.FromCommandAndArguments (
                    "dotnet",
                    new string [] {
                        "exec",
                        serverAssembly,
                        $"--ppid={Process.GetCurrentProcess ().Id}"
                    }),
                ExecFlags.RedirectStdout | ExecFlags.RedirectStderr,
                HandleOutput,
                workingDirectory: serverAssembly.ParentDirectory.ParentDirectory.ParentDirectory.ParentDirectory);
            exec.RunAsync ().Forget ();

            return tcs.Task;
        }
    }
}
