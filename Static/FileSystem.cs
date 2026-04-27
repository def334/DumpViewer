using DumpViewer.View.ModuleViews;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace DumpViewer.Static
{
    public static partial class FileSystem
    {
        [DllImport("DumpViewerDLL.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "OpenDumpFile")]
        unsafe private static extern bool OpenDumpFile(string filePath, int mode, int keepResident, IntPtr outInfo);

        [DllImport("DumpViewerDLL.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CloseDumpFile")]
        unsafe private static extern void CloseDumpFile(IntPtr info);

        [DllImport("DumpViewerDLL.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetMappedModules")]
        unsafe public static extern bool GetMappedModules(IntPtr dumpInfo, out IntPtr moduleList, out IntPtr moduleCount);

        [DllImport("DumpViewerDLL.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetMappedException")]
        unsafe public static extern bool GetMappedException(IntPtr dumpInfo, out IntPtr exceptionStream);

        [DllImport("DumpViewerDLL.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetMappedThreads")]
        unsafe public static extern bool GetMappedThreads(IntPtr dumpInfo, out IntPtr threadList, out IntPtr threadCount);

        public static void Close()
        {
            CloseDumpFile(DumpControl.CachePtr);
            DumpControl.Close();
        }

        public static bool OpenDump(string filePath, int mode = 1, int keepResident = 0)
        {
            if (DumpControl.IsDumpFileOpen)
                Close();

            try
            {
                IntPtr outInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<DumpFileInfo>());
                if (outInfoPtr == IntPtr.Zero)
                    throw new OutOfMemoryException("Contain to Null Ptr");

                string allocate_ptr = new(filePath);

                bool result = OpenDumpFile(allocate_ptr, mode, keepResident, outInfoPtr);
                if (result == false)
                {
                    Marshal.FreeHGlobal(outInfoPtr);
                    System.Windows.MessageBox.Show($"File not found", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return false;
                }

                DumpControl.OpenFromDumpInfo(in outInfoPtr);

                return result;
            }
            catch (OutOfMemoryException ex)
            {
                System.Windows.MessageBox.Show($"Failed to allocate memory.: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return false;
            }

        }

        public static bool GetModules(IntPtr dumpInfo, out IntPtr moduleBuffer, out IntPtr moduleCount)
        {
            moduleBuffer = IntPtr.Zero;
            moduleCount = IntPtr.Zero;

            if (dumpInfo == IntPtr.Zero)
            {
                return false;
            }

            return GetMappedModules(dumpInfo, out moduleBuffer, out moduleCount);
        }

        public static bool GetException(IntPtr dumpInfo, out IntPtr exceptionBuffer)
        {
            exceptionBuffer = IntPtr.Zero;

            if (dumpInfo == IntPtr.Zero)
            {
                return false;
            }

            return GetMappedException(dumpInfo, out exceptionBuffer);
        }

        public static bool GetThreads(IntPtr dumpInfo, out IntPtr threadBuffer, out IntPtr threadCount)
        {
            threadBuffer = IntPtr.Zero;
            threadCount = IntPtr.Zero;

            if (dumpInfo == IntPtr.Zero)
            {
                return false;
            }

            return GetMappedThreads(dumpInfo, out threadBuffer, out threadCount);
        }

    }
}