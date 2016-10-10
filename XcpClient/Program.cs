using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using XcpClient.Windows;

namespace XcpClient
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool created;
            using (Mutex mutex = new Mutex(true, "XCP.CLIENT", out created))
            {
                if (created)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new ClientForm());
                }
                else
                {
                    IntPtr hWnd = SharedHwnd.Get();
                    if(hWnd != null)
                    {
                        if (!WinApi.IsWindowVisible(hWnd))
                            WinApi.ShowWindow(hWnd, WinApi.SW_SHOW);
                        else if (WinApi.IsIconic(hWnd))
                            WinApi.ShowWindow(hWnd, WinApi.SW_RESTORE);
                        WinApi.SetForegroundWindow(hWnd);
                    }
                }
            }
        }
    }
}
