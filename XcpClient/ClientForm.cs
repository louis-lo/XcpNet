using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using XcpClient.Controls;
using XcpClient.Windows;

namespace XcpClient
{
    public partial class ClientForm : Form
    {
        private enum ShowMode
        {
            Ads,
            Web,
        }

        private static WinApi.HOOKPROC _hook13Proc;
        private static WinApi.HOOKPROC _hook14Proc;
        private SysHook _hook13;
        private SysHook _hook14;
        private ReaderWriterLockSlim _modeLock;
        private ShowMode _mode;
        private DateTime _lastTime;
        
        public ClientForm()
        {
            InitializeComponent();
#if (!DEBUG)
            this.TopMost = true;
#endif
            _hook13Proc = new WinApi.HOOKPROC(KeyboardHookProc);
            _hook14Proc = new WinApi.HOOKPROC(MouseHookProc);
            _hook13 = new SysHook(WinApi.WH_KEYBOARD_LL, _hook13Proc);
            _hook14 = new SysHook(WinApi.WH_MOUSE_LL, _hook14Proc);
            _modeLock = new ReaderWriterLockSlim();
            _mode = ShowMode.Web;
            _lastTime = DateTime.MinValue;
        }
        
        private ShowMode Mode
        {
            get { return _mode; }
            set
            {
                if (_mode != value)
                {
                    _modeLock.EnterWriteLock();
                    try
                    {
                        if (_mode != value)
                        {
                            switch (value)
                            {
                                case ShowMode.Ads:
                                    {
                                        if (ads.ShowNext(out _lastTime))
                                        {
                                            _mode = value;
                                            ads.Show();
                                            browser.Hide();
                                        }
                                    }
                                    break;
                                case ShowMode.Web:
                                    {
                                        ads.Stop();
                                        _mode = value;
                                        //if (!browser.IsBusy)
                                        //    browser.Refresh();
                                        browser.Show();
                                        ads.Hide();
                                    }
                                    break;
                            }
                        }
                    }
                    finally
                    {
                        _modeLock.ExitWriteLock();
                    }
                }
            }
        }

        private int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            bool handled = false;
            if (nCode >= 0)
            {
                Keys keyData;
                WinApi.KeyboardHookStruct MyKeyboardHookStruct = (WinApi.KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(WinApi.KeyboardHookStruct));
                if (wParam == WinApi.WM_KEYDOWN || wParam == WinApi.WM_SYSKEYDOWN)
                    keyData = (Keys)MyKeyboardHookStruct.vkCode;
                else if (wParam == WinApi.WM_KEYUP || wParam == WinApi.WM_SYSKEYUP)
                    keyData = (Keys)MyKeyboardHookStruct.vkCode;
                else
                    keyData = Keys.None;
                if (keyData == Keys.Escape 
                    || (keyData == Keys.LWin || keyData == Keys.RWin)
                    || ((ModifierKeys & Keys.Alt) != 0 && keyData == Keys.F4)
                    || ((ModifierKeys & Keys.Alt) != 0 && keyData == Keys.Tab)
                    || ((ModifierKeys & Keys.Control) != 0 && (ModifierKeys & Keys.Alt) != 0 && keyData == Keys.Delete)
                    || ((ModifierKeys & Keys.Control) != 0 && (ModifierKeys & Keys.Alt) != 0 && keyData == Keys.Q)
                    )
                {
                    handled = true;
                    if ((ModifierKeys & Keys.Control) != 0 && (ModifierKeys & Keys.Alt) != 0 && keyData == Keys.Q)
                        Close();
                }
            }
            if (!handled)
                return _hook13.CallNext(nCode, wParam, lParam);
            return 1;
        }
        private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == WinApi.WM_LBUTTONDOWN && Mode == ShowMode.Ads)
                    Mode = ShowMode.Web;
            }
            return _hook14.CallNext(nCode, wParam, lParam);
        }

        private async void ClientForm_Load(object sender, EventArgs e)
        {
            SharedHwnd.Set(Handle);

            browser.Url = new Uri("http://www.xcpnet.com");
            await ads.LoadConfig(null);
            Mode = ShowMode.Ads;
        }

        private void browser_BeforeNewWindow(object sender, WebBrowserNavigatingEventArgs e)
        {
            e.Cancel = true;
            ((WebControl)sender).Navigate(e.Url);
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            switch (Mode)
            {
                case ShowMode.Ads:
                    {
                        if (DateTime.Now >= _lastTime)
                            ads.ShowNext(out _lastTime);
                    }
                    break;
                case ShowMode.Web:
                    {
                        if (WinApi.GetLastInputTime() > 1000 * 3)
                        {
                            Mode = ShowMode.Ads;
                            browser.Document.Cookie = null;
                        }
                    }
                    break;
            }
        }
    }
}
