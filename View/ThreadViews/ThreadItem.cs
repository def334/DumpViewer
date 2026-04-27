using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DumpViewer.View.ThreadViews
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MiniDumpLocationDescriptor
    {
        public uint DataSize;
        public uint Rva;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MiniDumpMemoryDescriptor
    {
        public ulong StartOfMemoryRange;
        public MiniDumpLocationDescriptor Memory;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MiniDumpThreadEx
    {
        public uint ThreadId;
        public uint SuspendCount;
        public uint PriorityClass;
        public int Priority;
        public ulong Teb;
        public MiniDumpMemoryDescriptor Stack;
        public MiniDumpLocationDescriptor ThreadContext;
    }

    public sealed class ThreadItem
    {
        public string ThreadId { get; init; } = string.Empty;
        public string SuspendCount { get; init; } = string.Empty;
        public string PriorityClass { get; init; } = string.Empty;
        public string Priority { get; init; } = string.Empty;
        public string Teb { get; init; } = string.Empty;
        public string StackStart { get; init; } = string.Empty;
        public string StackDataSize { get; init; } = string.Empty;
        public string StackRva { get; init; } = string.Empty;
        public string ContextDataSize { get; init; } = string.Empty;
        public string ContextRva { get; init; } = string.Empty;

        public ThreadItem(in MiniDumpThreadEx thread)
        {
            ThreadId = thread.ThreadId.ToString();
            SuspendCount = thread.SuspendCount.ToString();
            PriorityClass = thread.PriorityClass.ToString();
            Priority = thread.Priority.ToString();
            Teb = $"0x{thread.Teb:X16}";
            StackStart = $"0x{thread.Stack.StartOfMemoryRange:X16}";
            StackDataSize = $"{thread.Stack.Memory.DataSize} bytes";
            StackRva = $"0x{thread.Stack.Memory.Rva:X8}";
            ContextDataSize = $"{thread.ThreadContext.DataSize} bytes";
            ContextRva = $"0x{thread.ThreadContext.Rva:X8}";
        }
    }
}
