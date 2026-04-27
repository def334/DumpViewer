using DumpViewer.Static;
using DumpViewer.View;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace DumpViewer.View.ThreadViews
{
    internal class ThreadListViewModel : SectionBaseForm
    {
        private ObservableCollection<ThreadItem> threadItems = new ObservableCollection<ThreadItem>();

        public ObservableCollection<ThreadItem> Threads
        {
            get => threadItems;
            set
            {
                if (threadItems == value)
                    return;

                threadItems = value;
                OnPropertyChanged(nameof(Threads));
            }
        }

        public void Clear()
        {
            Threads.Clear();
        }

        private bool GetMiniDumpThreadItems()
        {
            if (DumpControl.IsDumpFileOpen == false)
                return false;

            IntPtr dumpInfo = DumpControl.CachePtr;
            if (dumpInfo == IntPtr.Zero)
                return false;

            bool result = FileSystem.GetThreads(dumpInfo, out IntPtr threadListPtr, out IntPtr threadCountPtr);
            if (!result)
                return false;

            if (threadListPtr == IntPtr.Zero || threadCountPtr == IntPtr.Zero)
                return true;

            uint threadCount = unchecked((uint)threadCountPtr.ToInt64());
            int threadSize = Marshal.SizeOf<MiniDumpThreadEx>();

            for (uint i = 0; i < threadCount; i++)
            {
                IntPtr currentPtr = IntPtr.Add(threadListPtr, checked((int)(i * (uint)threadSize)));

                try
                {
                    MiniDumpThreadEx thread = Marshal.PtrToStructure<MiniDumpThreadEx>(currentPtr);
                    thread.Priority = (UInt32)thread.Priority == (UInt32)0xFFFFFFF1 ? -1 : thread.Priority;
                    Threads.Add(new ThreadItem(thread));
                }
                catch (Exception ex)
                {
                    Clear();
                    MessageBox.Show($"Failed to read thread information: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return true;
        }

        public override void Update()
        {
            if (isloaded)
                return;

            switch (DumpControl.FileCache.DumpMode)
            {
                case DumpType.Unknown:
                    break;
                case DumpType.miniDump:
                case DumpType.fullDump:
                    isloaded = GetMiniDumpThreadItems();
                    break;
                default:
                    break;
            }
        }

        protected override void OnFileClosed(object? sender, EventArgs e)
        {
            Clear();
        }
    }
}
