using System;
using System.Diagnostics;

namespace XcpClient.Windows
{
    internal sealed class SysHook : IDisposable
    {
        private bool disposed;
        private IntPtr _hook;

        public SysHook(int type, WinApi.HOOKPROC proc)
        {
            disposed = false;
            _hook = WinApi.SetWindowsHookEx(type, proc, WinApi.GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName), 0);
        }

        public int CallNext(int nCode, int wParam, IntPtr lParam)
        {
            return WinApi.CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (_hook != IntPtr.Zero)
                {
                    WinApi.UnhookWindowsHookEx(_hook);
                    _hook = IntPtr.Zero;
                }
                disposed = true;
            }
        }
        ~SysHook()
        {
            Dispose(false);
        }
    }
}
