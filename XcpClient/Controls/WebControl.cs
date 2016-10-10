using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows.Forms;

namespace XcpClient.Controls
{
    [ComVisible(true)]
    [DefaultProperty("Url")]
    [Docking(DockingBehavior.AutoDock)]
    [Description("Custom Web Browser")]
    [DefaultEvent("DocumentCompleted")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [Designer("System.Windows.Forms.Design.WebBrowserDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    internal class WebControl : WebBrowser
    {
        private AxHost.ConnectionPointCookie cookie;
        private WebBrowserExtendedEvents events;

        public event WebBrowserNavigatingEventHandler BeforeNavigate;
        public event WebBrowserNavigatingEventHandler BeforeNewWindow;

        protected override void CreateSink()
        {
            base.CreateSink();
            events = new WebBrowserExtendedEvents(this);
            cookie = new AxHost.ConnectionPointCookie(ActiveXInstance, events, typeof(DWebBrowserEvents2));
        }

        protected override void DetachSink()
        {
            if (null != cookie)
            {
                cookie.Disconnect();
                cookie = null;
            }
            base.DetachSink();
        }

        protected virtual void OnBeforeNavigate(WebBrowserNavigatingEventArgs e)
        {
            WebBrowserNavigatingEventHandler h = BeforeNavigate;
            if (null != h)
            {
                h(this, e);
            }
        }
        protected virtual void OnBeforeNewWindow(WebBrowserNavigatingEventArgs e)
        {
            WebBrowserNavigatingEventHandler h = BeforeNewWindow;
            if (null != h)
            {
                h(this, e);
            }
        }

        private sealed class WebBrowserExtendedEvents : StandardOleMarshalObject, DWebBrowserEvents2
        {
            private WebControl _browser;

            public WebBrowserExtendedEvents(WebControl browser)
            {
                _browser = browser;
            }

            public void BeforeNavigate2(object pDisp, ref object URL, ref object flags, ref object targetFrameName, ref object postData, ref object headers, ref bool cancel)
            {
                string uriString = (URL == null) ? string.Empty : ((string)URL);
                WebBrowserNavigatingEventArgs e = new WebBrowserNavigatingEventArgs(new Uri(uriString), (targetFrameName == null) ? string.Empty : ((string)targetFrameName));
                _browser.OnBeforeNavigate(e);
                cancel = e.Cancel;
            }

            public void NewWindow3(ref object pDisp, ref bool cancel, ref object flags, ref object URLContext, ref object URL)
            {
                string uriString = (URL == null) ? string.Empty : ((string)URL);
                WebBrowserNavigatingEventArgs e = new WebBrowserNavigatingEventArgs(new Uri(uriString), null);
                _browser.OnBeforeNewWindow(e);
                cancel = e.Cancel;
            }
        }
    }
}
