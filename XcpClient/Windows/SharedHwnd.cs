using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace XcpClient.Windows
{
    internal static class SharedHwnd
    {
        private const string _FileName = "lock";
        private const string _Name = "XCP.CLIENT.LOCK";

        public static void Set(IntPtr hWnd)
        {
            try
            {
                using (MemoryMappedFile file = MemoryMappedFile.CreateFromFile(FileSystem.GetPath(_FileName), FileMode.Create, _Name, IntPtr.Size))
                {
                    using (MemoryMappedViewAccessor view = file.CreateViewAccessor(0, IntPtr.Size))
                    {
                        if (IntPtr.Size == 4)
                            view.Write(0, hWnd.ToInt32());
                        else
                            view.Write(0, hWnd.ToInt64());
                    }
                }
            }
            catch (Exception
#if(DEBUG)
            ex
#endif
            )
            {
            }
        }
        public static IntPtr Get()
        {
            try
            {
                using (MemoryMappedFile file = MemoryMappedFile.CreateFromFile(FileSystem.GetPath(_FileName), FileMode.Open, _Name, IntPtr.Size))
                {
                    using (MemoryMappedViewAccessor view = file.CreateViewAccessor(0, IntPtr.Size))
                    {
                        if (IntPtr.Size == 4)
                            return new IntPtr(view.ReadInt32(0));
                        else
                            return new IntPtr(view.ReadInt64(0));
                    }
                }
            }
            catch (Exception
#if(DEBUG)
            ex
#endif
            )
            {
            }
            return IntPtr.Zero;
        }
    }
}
