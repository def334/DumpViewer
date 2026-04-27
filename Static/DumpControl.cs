using DumpViewer.View.ExceptionViews;
using DumpViewer.View.ModuleViews;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Documents;

namespace DumpViewer.Static
{
    public static class DumpControl
    {

        unsafe private static DumpFileInfo fileCache = new DumpFileInfo();
        public static DumpFileInfo FileCache
        {
            get
            {
                if (!IsDumpFileOpen)
                    return default;
                return fileCache;
            }
        }

        unsafe private static IntPtr cachePtr = IntPtr.Zero;
        public static IntPtr CachePtr
        {
            get => cachePtr;
            private set => cachePtr = value;
        }

        public static event EventHandler? FileClosed;

        public static bool IsDumpFileOpen { get; private set; } = false;

        private static unsafe nuint RvaToVa(nuint rva)
        {
            unsafe
            {
                byte* basePtr = (byte*)fileCache.MappedFile.data;
                byte* offsetPtr = basePtr + rva;
                return (nuint)offsetPtr;
            }
        }

        private static bool IsRangeValid(nuint offset, nuint length)
        {
            if (!IsDumpFileOpen)
                return false;

            unsafe
            {

                ulong fileSize = FileCache.fileSize;
                if (length >= fileSize)
                    return false;

                return fileSize >= (offset - (nuint)fileCache.MappedFile.data);
            }
        }
        public static unsafe bool TryReadStructure<T>(nuint VA, out T? value) where T : unmanaged
        {
            value = null;

            nuint size = (nuint)sizeof(T);
            if (!IsRangeValid(VA, size))
                return false;

            byte* src = (byte*)VA;
            value = Unsafe.ReadUnaligned<T>(src);
            return true;
        }

        public static bool OpenFromDumpInfo(in IntPtr info)
        {
            unsafe
            {
                fileCache = Marshal.PtrToStructure<DumpFileInfo>(info);
                if (fileCache.HEADER == null || fileCache.MappedFile.size == 0)
                {
                    Close();
                    return false;
                }


                cachePtr = info;
                IsDumpFileOpen = true;
            }
            return true;
        }

        public static void Close()
        {
            bool wasOpen = IsDumpFileOpen || cachePtr != IntPtr.Zero;

            if (cachePtr != IntPtr.Zero)
                Marshal.FreeHGlobal(cachePtr);

            cachePtr = IntPtr.Zero;
            fileCache = default;
            IsDumpFileOpen = false;

            if (wasOpen)
                RaiseFileClosedSafely();
        }

        private static void RaiseFileClosedSafely()
        {
            Delegate[] handlers = FileClosed?.GetInvocationList() ?? Array.Empty<Delegate>();
            for (int i = 0; i < handlers.Length; i++)
            {
                if (handlers[i] is not EventHandler handler)
                    continue;

                try
                {
                    handler(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"FileClosed handler failed: {ex}");
                }
            }
        }

        public static string? GetBugCheck()
        {
            if (!DumpControl.IsDumpFileOpen)
                return null;

            StringBuilder retString = new StringBuilder();

            unsafe
            {
                KernelDumpHeader64 header = fileCache.HEADER->FULL_DUMP64.kernelHeader;

                retString.AppendLine($"Bug Check Code: 0x{header.BugCheckCode:X}");
                for (int i = 0; i < 4; i++)
                {
                    retString.AppendLine($"Bug Check Parameter {i + 1}: 0x{(i == 0 ? header.BugCheckParameter1 : i == 1 ? header.BugCheckParameter2 : i == 2 ? header.BugCheckParameter3 : header.BugCheckParameter4):X}");
                }

            }
            return retString.ToString();
        }

        public static bool GetModuleLists(ref ObservableCollection<ModuleItem>? modules)
        {
            if (!IsDumpFileOpen || modules == null)
                return false;

            if(!FileSystem.GetModules(cachePtr, out nint BufferAdder, out nint BufferCount))
                return false;

             modules.Clear();

            unsafe
            {
                MiniDumpModule* moduleArray = (MiniDumpModule*)BufferAdder;
                for (int i = 0; i < BufferCount; i++)
                {
                    MiniDumpModule* module = &moduleArray[i];

                    string[] moduleData = new string[4];
                    moduleData[0] = Format.GetDumpPath((char*)RvaToVa(module->ModuleNameRva + 4));
                    moduleData[1] = $"{module->SizeOfImage / 1024d / 1024d:F1} MB";
                    moduleData[2] = Format.GetModuleVersion((nuint)module->VSFILEINFO);
                    moduleData[3] = $"0x{module->BaseOfImage:X}";

                    modules.Add(new ModuleItem(moduleData));
                }
            }

            return true;

        }

        public static MINIDUMP_EXCEPTION_STREAM? GetExceptionStream()
        {
            if(!DumpControl.IsDumpFileOpen)
                return null;

            unsafe
            {
                if(fileCache.isKernelDump == 0)
                {
                    if(!FileSystem.GetException(cachePtr, out nint exceptionBuffer))
                    {
                        return null;
                    }

                    return Marshal.PtrToStructure<MINIDUMP_EXCEPTION_STREAM>((IntPtr)exceptionBuffer);
                }
                else
                {
                    KernelDumpHeader64 header = fileCache.HEADER->FULL_DUMP64.kernelHeader;
                    if (header.MiniDumpFields == 0)
                        return null;
                    // In a full dump, the exception stream is located immediately after the header
                    byte* basePtr = (byte*)fileCache.HEADER;
                    byte* exceptionStreamPtr = basePtr + sizeof(KernelDumpHeader64);
                    return Marshal.PtrToStructure<MINIDUMP_EXCEPTION_STREAM>((IntPtr)exceptionStreamPtr);
                }
            }
        }

        public static ExceptionRecord64? GetException()
        {
            if (!DumpControl.IsDumpFileOpen)
                return null;

            unsafe
            {

                if(fileCache.isKernelDump == 0)
                {
                    MINIDUMP_EXCEPTION_STREAM? exceptionStream = GetExceptionStream();
                    if (exceptionStream == null)
                        return null;
                    return exceptionStream.Value.ExceptionRecord;
                }

                KernelDumpHeader64 header = fileCache.HEADER->FULL_DUMP64.kernelHeader;
                return header.Exception;
            }
        }


        public static List<string>? GetFileStatus()
        {
            if (!IsDumpFileOpen)
                return null;
            List<string> status = new List<string>();

            unsafe
            {
                DumpFileInfo* infoPtr = (DumpFileInfo*)cachePtr;
                status.Add(Format.GetDumpPath((char*)infoPtr->path));
                status.Add($"{infoPtr->fileSize / 1024d / 1024d:F1} MB");
                status.Add(Format.GetDumpTypeString(infoPtr->DumpMode));
                status.Add(Format.FileTimeToDateTime(infoPtr->creationTime).ToString("yyyy-MM-dd HH:mm:ss"));
            }

            return status;
        }

        public static StringBuilder[]? GetDumpHeader()
        {
            if (!IsDumpFileOpen)
                return null;

            return FormatDumpHeader();
        }

        enum Filelayout : int
        {
            DumpHeader,
            Exception,

            enumSize,
        }

        private static StringBuilder[] FormatDumpHeader()
        {
            StringBuilder[] sb = new StringBuilder[(int)Filelayout.enumSize];

            for (int i = 0; i < (int)Filelayout.enumSize; i++)
            {
                sb[i] = new StringBuilder();
            }

            ref StringBuilder dumpHeaderBuilder = ref sb[(int)Filelayout.DumpHeader];
            ref StringBuilder exceptionBuilder = ref sb[(int)Filelayout.Exception];

            unsafe
            {
                dumpHeaderBuilder.AppendLine($"Dump Path: {Format.GetDumpPath((char*)fileCache.path)}");
                dumpHeaderBuilder.AppendLine($"Dump Type: {Format.GetDumpTypeString(fileCache.DumpMode)}");
                dumpHeaderBuilder.AppendLine($"File Size: {fileCache.fileSize / 1024d / 1024d:F1} MB");
                dumpHeaderBuilder.AppendLine($"File Attributes: {Format.GetFileAttributesString(fileCache.fileAttributes)}");
                dumpHeaderBuilder.AppendLine($"Creation Time: {Format.FileTimeToDateTime(fileCache.creationTime)}");
                dumpHeaderBuilder.AppendLine($"Last Write Time: {Format.FileTimeToDateTime(fileCache.lastWriteTime)}");

                if (fileCache.isKernelDump == 1)
                {
                    KernelDumpHeader64 header = fileCache.HEADER->FULL_DUMP64.kernelHeader;
                    dumpHeaderBuilder.AppendLine($"Major Version: {header.MajorVersion}");
                    dumpHeaderBuilder.AppendLine($"Minor Version: {header.MinorVersion}");
                    dumpHeaderBuilder.AppendLine($"Machine Image Type: {header.MachineImageType}");
                    dumpHeaderBuilder.AppendLine($"Number of Processors: {header.NumberProcessors}");
                    exceptionBuilder.AppendLine($"Bug Check Code: 0x{header.BugCheckCode:X}");
                    for (int i = 0; i < 4; i++)
                    {
                        exceptionBuilder.AppendLine($"Bug Check Parameter {i + 1}: 0x{(i == 0 ? header.BugCheckParameter1 : i == 1 ? header.BugCheckParameter2 : i == 2 ? header.BugCheckParameter3 : header.BugCheckParameter4):X}");
                    }
                    dumpHeaderBuilder.AppendLine($"System Up Time: {header.SystemUpTime} ms");
                    dumpHeaderBuilder.AppendLine($"Mini Dump Fields: 0x{header.MiniDumpFields:X}");
                    dumpHeaderBuilder.AppendLine($"Product Type: {header.ProductType}");
                    dumpHeaderBuilder.AppendLine($"Suite Mask: 0x{header.SuiteMask:X}");
                }
                else
                {
                    MinidumpHeader header = fileCache.HEADER->MINI_DUMP64.miniHeader;
                    dumpHeaderBuilder.AppendLine($"Signature: 0x{header.Signature:X}");
                    dumpHeaderBuilder.AppendLine($"Version: 0x{header.Version:X}");
                    dumpHeaderBuilder.AppendLine($"Number of Streams: {header.NumberOfStreams}");
                    dumpHeaderBuilder.AppendLine($"Stream Directory RVA: 0x{header.StreamDirectoryRva:X}");
                    dumpHeaderBuilder.AppendLine($"CheckSum: 0x{header.CheckSum:X}");
                    dumpHeaderBuilder.AppendLine($"TimeDateStamp: 0x{header.TimeDateStamp:X}");
                    dumpHeaderBuilder.AppendLine($"Flags: 0x{header.Flags:X}");

                    MINIDUMP_EXCEPTION_STREAM? exception = GetExceptionStream();
                    if (exception == null)
                    {
                        exceptionBuilder.AppendLine("No exception information available.");
                    }
                    else
                    {
                        ExceptionRecord64? record64 = exception.Value.ExceptionRecord;
                        exceptionBuilder.AppendLine($"Thread ID: 0x{exception.Value.ThreadId:X8}");
                        exceptionBuilder.AppendLine($"Exception Code: 0x{record64.Value.ExceptionCode:X8}");
                        exceptionBuilder.AppendLine($"Exception Flags: 0x{record64.Value.ExceptionFlags:X8}");
                        exceptionBuilder.AppendLine($"Exception Record: 0x{record64.Value.ExceptionRecord:X}");
                        exceptionBuilder.AppendLine($"Exception Address: 0x{record64.Value.ExceptionAddress:X}");
                    }

                }
            }

            return sb;
        }
    }
}
