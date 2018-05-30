using Foundation;
using WebKit;

namespace Xamarin.Interactive.Client.Mac
{

    // Should subclass AppKit.NSView
    [Register ("WorkbookModernWebViewController")]
    public partial class WorkbookModernWebViewController
    {
        [Outlet]
        WKWebView webView { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (webView != null) {
                webView.Dispose ();
                webView = null;
            }
        }
    }
}
