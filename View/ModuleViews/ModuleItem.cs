using DumpViewer.Static;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DumpViewer.View.ModuleViews
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public unsafe struct MiniDumpModule
    {
        public UInt64 BaseOfImage;
        public UInt32 SizeOfImage;
        public UInt32 CheckSum;
        public UInt32 TimeDateStamp;
        public UInt32 ModuleNameRva;
        public fixed UInt32 VSFILEINFO[13];
        public UInt64 CvRecord;
        public UInt64 MiscRecord;
        public UInt64 Reserved0;
        public UInt64 Reserved1;
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
    public unsafe struct MiniDumpModuleList
    {
        public uint NumberOfModules;
        public MiniDumpModule* Modules;
    };

    public class ModuleItem
    {
        public string ModuleName { get; init; } = string.Empty;
        public string ModuleSize { get; init; } = string.Empty;
        public string ModuleVersion { get; init; } = string.Empty;
        public string ModulePath { get; init; } = string.Empty;

        public string ModuleBaseAddress { get; init; } = string.Empty;

        public ModuleItem(string[] value)
        {
            if (value.Length != 4)
                throw new ArgumentException("Invalid module item data.");

            ModulePath = value[0];
            ModuleName = Path.GetFileName(ModulePath);
            ModuleSize = value[1];
            ModuleVersion = value[2];
            ModuleBaseAddress = value[3];
        }
    }
}
