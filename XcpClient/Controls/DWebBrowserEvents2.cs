using System;
using System.Runtime.InteropServices;

namespace XcpClient.Controls
{
    [ComImport]
    [Guid("34A715A0-6587-11D0-924A-0020AFC7AC4D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    [TypeLibType(TypeLibTypeFlags.FHidden)]
    internal interface DWebBrowserEvents2
    {
        [DispId(250)]
        void BeforeNavigate2([In, MarshalAs(UnmanagedType.IDispatch)] object pDisp, [In] ref object URL, [In] ref object flags, [In] ref object targetFrameName, [In] ref object postData, [In] ref object headers, [In, Out] ref bool cancel);

        [DispId(273)]
        void NewWindow3([In, Out, MarshalAs(UnmanagedType.IDispatch)] ref object pDisp, [In, Out] ref bool cancel, [In] ref object flags, [In] ref object URLContext, [In] ref object URL);
    }
}
