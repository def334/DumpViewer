using DumpViewer.Static;
using DumpViewer.View.ExceptionViews;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DumpViewer.Static
{
    public unsafe static class NativeConstants
    {
        public const int MaxPath = 260;

        public const int DumpHeader64Size = 0x2000; // 8192
        public const int UsermodeCrashdumpHeader64Size = 96;
        public const int FullDumpHeader64Size = DumpHeader64Size + UsermodeCrashdumpHeader64Size; // 8288
        public const int DumpHeaderUnionSize = FullDumpHeader64Size;

        public const int MappedFileBufferSize = 40; // x64
        public const int DumpFileInfoSize = 96;     // pointer-based DumpFileInfo (x64)

        public const int DmpPhysicalMemoryBlockSize64 = 700;
        public const int DmpContextRecordSize64 = 3000;
        public const int DmpHeaderCommentSize = 128;
        public const int DmpReserved0Size64 = 4016;
        public const int ExceptionMaxParameters = 15;
        public const int UserModeCount = 32;
    }

    public enum FileOpenMode : int
    {
        Default = 0,
        Byte = 1
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct FileTimeNative
    {
        [FieldOffset(0)] public uint dwLowDateTime;
        [FieldOffset(4)] public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MappedFileBuffer
    {
        [FieldOffset(0)] public IntPtr fileHandle;
        [FieldOffset(8)] public IntPtr mappingHandle;
        [FieldOffset(16)] public IntPtr data;
        [FieldOffset(24)] public nuint size;
        [FieldOffset(32)] public FileOpenMode mode;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MinidumpHeader
    {
        [FieldOffset(0)] public uint Signature;
        [FieldOffset(4)] public uint Version;
        [FieldOffset(8)] public uint NumberOfStreams;
        [FieldOffset(12)] public uint StreamDirectoryRva;
        [FieldOffset(16)] public uint CheckSum;
        [FieldOffset(20)] public uint Reserved;
        [FieldOffset(20)] public uint TimeDateStamp;
        [FieldOffset(24)] public ulong Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct UsermodeCrashdumpHeader64
    {
        public uint Signature;
        public uint ValidDump;
        public uint MajorVersion;
        public uint MinorVersion;
        public uint MachineImageType;
        public uint ThreadCount;
        public uint ModuleCount;
        public uint MemoryRegionCount;
        public ulong ThreadOffset;
        public ulong ModuleOffset;
        public ulong DataOffset;
        public ulong MemoryRegionOffset;
        public ulong DebugEventOffset;
        public ulong ThreadStateOffset;
        public ulong VersionInfoOffset;
        public ulong Spare1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct ExceptionRecord64
    {
        public uint ExceptionCode;
        public uint ExceptionFlags;
        public ulong ExceptionRecord;
        public ulong ExceptionAddress;
        public uint NumberParameters;
        public uint __unusedAlignment;

        public fixed ulong ExceptionInformation[NativeConstants.ExceptionMaxParameters];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    unsafe public struct MINIDUMP_EXCEPTION_STREAM
    {
        public UInt32 ThreadId;
        public UInt32 __alignment;
        public ExceptionRecord64 ExceptionRecord;
        public UInt64 ThreadContext;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = NativeConstants.DumpHeader64Size)]
    public unsafe struct KernelDumpHeader64
    {
        public uint Signature;
        public uint ValidDump;
        public uint MajorVersion;
        public uint MinorVersion;
        public ulong DirectoryTableBase;
        public ulong PfnDataBase;
        public ulong PsLoadedModuleList;
        public ulong PsActiveProcessHead;
        public uint MachineImageType;
        public uint NumberProcessors;
        public uint BugCheckCode;
        public ulong BugCheckParameter1;
        public ulong BugCheckParameter2;
        public ulong BugCheckParameter3;
        public ulong BugCheckParameter4;

        public fixed byte VersionUser[32];

        public ulong KdDebuggerDataBlock;

        public fixed byte PhysicalMemoryBlockBuffer[NativeConstants.DmpPhysicalMemoryBlockSize64];

        public fixed byte ContextRecord[NativeConstants.DmpContextRecordSize64];

        public ExceptionRecord64 Exception;
        public uint DumpType;
        public long RequiredDumpSpace;
        public long SystemTime;

        public fixed byte Comment[NativeConstants.DmpHeaderCommentSize];

        public long SystemUpTime;
        public uint MiniDumpFields;
        public uint SecondaryDataState;
        public uint ProductType;
        public uint SuiteMask;
        public uint WriterStatus;
        public byte Unused1;
        public byte KdSecondaryVersion;

        public fixed byte Unused[2];

        public fixed byte _reserved0[NativeConstants.DmpReserved0Size64];
    }

    [StructLayout(LayoutKind.Explicit, Size = NativeConstants.FullDumpHeader64Size)]
    public struct FullDumpHeader64
    {
        [FieldOffset(0)] public KernelDumpHeader64 kernelHeader;
        [FieldOffset(0)] public UsermodeCrashdumpHeader64 userHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MiniDump64
    {
        public MinidumpHeader miniHeader;
    }

    [StructLayout(LayoutKind.Explicit, Size = NativeConstants.DumpHeaderUnionSize)]
    public struct DumpHeaderUnion
    {
        [FieldOffset(0)] public FullDumpHeader64 FULL_DUMP64;
        [FieldOffset(0)] public MiniDump64 MINI_DUMP64;
    }

    public enum DumpType : uint
    {
        Unknown = 0,
        miniDump = 1,
        fullDump = 2,

        kernelDump = 11,
        summaryDump = 12,
        onlyHeader = 13,
        triageDump = 14,
    };

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DumpFileInfo
    {
        [FieldOffset(0)] public UIntPtr path; // char*
        [FieldOffset(8)] public uint fileAttributes;
        [FieldOffset(16)] public ulong fileSize;
        [FieldOffset(24)] public int isKernelDump;
        [FieldOffset(28)] public DumpType DumpMode;
        [FieldOffset(32)] public FileTimeNative creationTime;
        [FieldOffset(40)] public FileTimeNative lastWriteTime;
        [FieldOffset(48)] public DumpHeaderUnion* HEADER;
        [FieldOffset(56)] public MappedFileBuffer MappedFile;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DumpCallstackFrame
    {
        public uint threadId;
        public uint frameIndex;
        public ulong instructionPointer;
        public ulong stackPointer;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DumpModuleInfo
    {
        public ulong baseOfImage;
        public uint sizeOfImage;
        public uint checksum;
        public uint timeDateStamp;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeConstants.MaxPath)]
        public string moduleName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DumpThreadInfo
    {
        public uint threadId;
        public uint suspendCount;
        public uint priorityClass;
        public uint priority;
        public ulong teb;
        public ulong stackStart;
        public uint stackDataSize;
        public uint stackRva;
        public uint contextDataSize;
        public uint contextRva;
    }
}