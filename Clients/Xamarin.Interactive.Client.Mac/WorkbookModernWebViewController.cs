using System;
using System.Threading.Tasks;

using AppKit;
using Foundation;
using ObjCRuntime;
using WebKit;

using Newtonsoft.Json;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class WorkbookModernWebViewController : SessionViewController
    {
        // Called when created from unmanaged code
        public WorkbookModernWebViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            // TODO: Check prefs, and see if we can access private Inspector API like before
            webView.Configuration.Preferences.SetValueForKey (
                FromObject (true),
                (NSString)"developerExtrasEnabled");

            var userContentController = webView.Configuration.UserContentController;
            userContentController.AddScriptMessageHandler (new ScriptMessageHandler (this), "workbooks");
            var script = new WKUserScript (
                (NSString)"xiexports.holla('sup');",
                WKUserScriptInjectionTime.AtDocumentEnd,
                isForMainFrameOnly: true);
            userContentController.AddUserScript (script);
        }

        public override void ViewDidAppear ()
        {
            base.ViewDidAppear ();

            LoadWorkbookAppAsync ().Forget ();
        }

        async Task LoadWorkbookAppAsync ()
        {
            var uri = await ClientServerService.SharedInstance.GetUriAsync ();
            webView.LoadRequest (new NSUrlRequest (new NSUrl (uri.AbsoluteUri)));

            // TODO: Need to keep disposal ticket?
            Session.Subscribe (HandleUserAction);

            await Task.Delay (10000);
            webView.EvaluateJavaScript (
                "xiexports.holla('after timeout');",
                (result, error) => {
                    Log.Debug ("TAG", $"eval result: {result}");
                });
        }

        readonly static JsonSerializerSettings jsonSettings = new ExternalInteractiveJsonSerializerSettings ();

        void HandleUserAction (UserAction action)
        {
            // TODO: Serialization settings and whatnot, enums, blah blah blah
            var actionJson = JsonConvert.SerializeObject (action, jsonSettings);
            webView.EvaluateJavaScript (
                $"xiexports.sendAction({actionJson});",
                (result, error) => {
                    if (error != null)
                        Log.Error ("TAG", new NSErrorException (error));
                });
        }

        #region Command Selectors

        public override bool RespondsToSelector (Selector sel)
        {
            switch (sel.Name) {
            case "runAllSubmissions:":
                return Session.SessionKind != ClientSessionKind.LiveInspection && Session.CanEvaluate;
            case "addPackage:":
                return Session.CanAddPackages;
            }

            return base.RespondsToSelector (sel);
        }

        [Export ("runAllSubmissions:")]
        void RunAllSubmissions (NSObject sender)
            => Session.PostAction (UserActionKind.RunAll);

        [Export ("addPackage:")]
        void AddPackage (NSObject sender)
        {
            Session.PostAction (UserActionKind.AddPackages);
        }

        //[Export ("RoutedCommand_Execute_NuGetPackageNode_Remove:parameter:")]
        //void RemovePackage (NSObject sender, RoutedCommand.ParameterProxy parameter)
        //{
        //    //var node = (NuGetPackageNode)parameter.Value;
        //    //Session.Workbook.Packages.RemovePackage (
        //    //(InteractivePackage)node.RepresentedObject);
        //}

#endregion
    }

    class ScriptMessageHandler : WKScriptMessageHandler
    {
        readonly WorkbookModernWebViewController controller;

        public ScriptMessageHandler (WorkbookModernWebViewController controller)
        {
            this.controller = controller ?? throw new ArgumentNullException (nameof (controller));
        }

        public override void DidReceiveScriptMessage (WKUserContentController userContentController, WKScriptMessage message)
        {
            Log.Debug ("TAG", message.Body.ToString ());
            if (message.Body is NSArray array) {
                for (nuint i = 0; i < array.Count; i++) {
                    var item = array.GetItem<NSObject> (i);
                    Log.Debug ("TAG", $"{item.GetType ()} {item}");
                }
            }

            if ((message.Body as NSString) == "sessionReady") {
                var path = "/Users/sandy/Documents/workbooks/mcc.workbook";
                controller.Session.PostAction (new LoadWorkbookAction (
                    controller.Session,
                    path,
                    System.IO.File.ReadAllText (path)));
            }
        }
    }
}
