using System;
using System.Threading.Tasks;

using AppKit;
using Foundation;
using WebKit;

using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Client.Mac
{
    sealed partial class WorkbookModernWebViewController : NSViewController
    {
        #region Constructors

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
            userContentController.AddScriptMessageHandler (new ScriptMessageHandler (), "workbooks");
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

            await Task.Delay (10000);
            webView.EvaluateJavaScript (
                "xiexports.holla('after timeout');",
                (result, error) => {
                    Log.Debug ("TAG", $"eval result: {result}");
                });
        }

        #endregion
    }

    class ScriptMessageHandler : WKScriptMessageHandler
    {
        public override void DidReceiveScriptMessage (WKUserContentController userContentController, WKScriptMessage message)
        {
            Log.Debug ("TAG", message.Body.ToString ());
            if (message.Body is NSArray array) {
                for (nuint i = 0; i < array.Count; i++) {
                    var item = array.GetItem<NSObject> (i);
                    Log.Debug ("TAG", $"{item.GetType ()} {item}");
                }
            }
        }
    }
}
