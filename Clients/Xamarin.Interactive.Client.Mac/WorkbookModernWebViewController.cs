using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Xamarin.Interactive.Client.Mac
{
    public partial class WorkbookModernWebViewController : NSViewController
    {
        #region Constructors

        // Called when created from unmanaged code
        public WorkbookModernWebViewController (IntPtr handle) : base (handle)
        {
            //
        }

        public override void ViewDidAppear ()
        {
            base.ViewDidAppear ();

            ClientServerService.SharedInstance.GetUriAsync ().ContinueWith (
                t => MainThread.Post (() => webView.LoadRequest (new NSUrlRequest (new NSUrl (t.Result.AbsoluteUri)))));
        }

        #endregion
    }
}
