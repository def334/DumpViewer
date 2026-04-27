using DumpViewer.View.ModuleViews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DumpViewer.Static
{
    public static class Format
    {
        unsafe public static string GetDumpPath(char* pathptr)
        {
            string result = "";

            unsafe
            {
                char* path = (char*)pathptr;
                while (*path != '\0')
                {
                    result += *path;
                    path++;
                }
            }

            return result;
        }

        public static string GetDumpTypeString(DumpType type)
        {
            return type switch
            {
                DumpType.miniDump => " Mini Dump",
                DumpType.fullDump => " Full Dump",
                DumpType.kernelDump => " Full Dump(Kernel Mode)",
                DumpType.summaryDump => " Summary Dump(Kernel Mode)",
                DumpType.onlyHeader => " Only Header Dump(Kernel Mode)",
                DumpType.triageDump => " Triage Dump(Kernel Mode)",
                _ => " Unknown Dump Type",
            };
        }

        public static string GetFileAttributesString(uint attributes)
        {
            if (attributes == 0) return "None";
            var attrList = new List<string>();
            if ((attributes & 0x1) != 0) attrList.Add("ReadOnly");
            if ((attributes & 0x2) != 0) attrList.Add("Hidden");
            if ((attributes & 0x4) != 0) attrList.Add("System");
            if ((attributes & 0x10) != 0) attrList.Add("Directory");
            if ((attributes & 0x20) != 0) attrList.Add("Archive");
            if ((attributes & 0x40) != 0) attrList.Add("Device");
            if ((attributes & 0x80) != 0) attrList.Add("Normal");
            if ((attributes & 0x100) != 0) attrList.Add("Temporary");
            if ((attributes & 0x200) != 0) attrList.Add("SparseFile");
            if ((attributes & 0x400) != 0) attrList.Add("ReparsePoint");
            if ((attributes & 0x800) != 0) attrList.Add("Compressed");
            if ((attributes & 0x1000) != 0) attrList.Add("Offline");
            if ((attributes & 0x2000) != 0) attrList.Add("NotContentIndexed");
            if ((attributes & 0x4000) != 0) attrList.Add("Encrypted");
            return string.Join(", ", attrList);
        }

        public static string GetModuleVersion(nuint offset)
        {
            unsafe
            {
                UInt32* versionInfo = (UInt32*)offset;
                return $"{versionInfo[2] >> 16}.{versionInfo[2] & 0xFFFF}.{versionInfo[3] >> 16}.{versionInfo[3] & 0xFFFF}";
            }
        }

        public static DateTime FileTimeToDateTime(FileTimeNative fileTime)
        {
            long high = ((long)fileTime.dwHighDateTime) << 32;
            long low = fileTime.dwLowDateTime;
            long fileTimeLong = high | low;
            return DateTime.FromFileTimeUtc(fileTimeLong);
        }
    }
}
