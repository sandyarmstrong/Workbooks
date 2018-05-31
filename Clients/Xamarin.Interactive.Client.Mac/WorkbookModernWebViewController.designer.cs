using Foundation;
using WebKit;

namespace Xamarin.Interactive.Client.Mac
{
    [Register ("WorkbookModernWebViewController")]
    partial class WorkbookModernWebViewController
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
