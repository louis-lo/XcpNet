using System;
using System.Runtime.InteropServices;

namespace XcpClient.Windows
{
    internal static class WinApi
    {
        public const int SW_SHOW = 5;
        public const int SW_RESTORE = 9;
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        public const int WH_KEYBOARD_LL = 13;
        public const int WH_MOUSE_LL = 14;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_LBUTTONDOWN = 0x0201;
        [StructLayout(LayoutKind.Sequential)]
        public class KeyboardHookStruct
        {
            /// <summary>
            /// 定一个虚拟键码。该代码必须有一个价值的范围1至254
            /// </summary>
            public int vkCode;
            /// <summary>
            /// 指定的硬件扫描码的关键
            /// </summary>
            public int scanCode;
            /// <summary>
            /// 键标志
            /// </summary>
            public int flags;
            /// <summary>
            /// 指定的时间戳记的这个讯息
            /// </summary>
            public int time;
            /// <summary>
            /// 指定额外信息相关的信息
            /// </summary>
            public int dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int x;
            public int y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public class MouseLLHookStruct
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        public delegate int HOOKPROC(int nCode, int wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HOOKPROC lpfn, IntPtr hMod, int dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(IntPtr hhk, int nCode, int wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);


        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        public const uint ES_CONTINUOUS = 0x80000000;
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint flags);


        public const uint SPI_SETSCREENSAVEACTIVE = 0x0011;
        public const uint SPIF_SENDWININICHANGE = 0x0002;
        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(uint uiAction, bool uiParam, ref bool pvParam, uint fWinIni);


        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            /// <summary>
            /// 设置结构体块容量
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            public int cbSize;
            /// <summary>
            /// 捕获的时间
            /// </summary>
            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        public static long GetLastInputTime()
        {
            LASTINPUTINFO vLastInputInfo = new LASTINPUTINFO();
            vLastInputInfo.cbSize = Marshal.SizeOf(vLastInputInfo);
            if (!GetLastInputInfo(ref vLastInputInfo))
                return 0L;
            else
                return Environment.TickCount - vLastInputInfo.dwTime;
        }
    }
}
