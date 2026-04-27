using DumpViewer.Static;
using DumpViewer.View;
using System;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace DumpViewer.View.ExceptionViews
{

    internal sealed class ExceptionViewModel : SectionBaseForm
    {
        private string _exceptionCode = "N/A";
        public string ExceptionCode
        {
            get => _exceptionCode;
            set
            {
                if (_exceptionCode == value)
                    return;

                _exceptionCode = value;
                OnPropertyChanged(nameof(ExceptionCode));
            }
        }

        private string _occurredAt = "N/A";
        public string OccurredAt
        {
            get => _occurredAt;
            set
            {
                if (_occurredAt == value)
                    return;

                _occurredAt = value;
                OnPropertyChanged(nameof(OccurredAt));
            }
        }

        private string _faultModule = "N/A";
        public string FaultModule
        {
            get => _faultModule;
            set
            {
                if (_faultModule == value)
                    return;

                _faultModule = value;
                OnPropertyChanged(nameof(FaultModule));
            }
        }

        private string _faultAddress = "N/A";
        public string FaultAddress
        {
            get => _faultAddress;
            set
            {
                if (_faultAddress == value)
                    return;

                _faultAddress = value;
                OnPropertyChanged(nameof(FaultAddress));
            }
        }

        private string _threadId = "N/A";
        public string ThreadId
        {
            get => _threadId;
            set
            {
                if (_threadId == value)
                    return;

                _threadId = value;
                OnPropertyChanged(nameof(ThreadId));
            }
        }

        private ObservableCollection<stackItem> _stackList = new ObservableCollection<stackItem>();
        public ObservableCollection<stackItem> StackList
        {
            get => _stackList;
            set
            {
                if (_stackList == value)
                    return;
                _stackList = value;
                OnPropertyChanged(nameof(StackList));
            }
        }

        public ExceptionViewModel()
        {

        }

        private bool GetStackList()
        {
            nint dumpInfo = DumpControl.CachePtr;

            if (FileSystem.GetMappedThreads(dumpInfo, out nint threadListPtr, out nint threadCount))
            {
                if (threadListPtr == nint.Zero || threadCount == 0)
                    return false;

                for (nint i = 0; i < threadCount; i++)
                {
                    DumpControl.TryReadStructure<UInt64>((nuint)(threadListPtr + i * 0x8), out UInt64? value);

                    StackList.Add(new stackItem
                    {
                        Addr = $"0x{threadListPtr + i * 0x8:X16}",
                        Value = $"0x{value:X16}"
                    });
                }
            }

            return true;
        }

        private void SetPropertyFromExceptionRecord(ExceptionRecord64 exception)
        {
            ExceptionCode = $"0x{exception.ExceptionCode:X8}";
            FaultAddress = $"0x{exception.ExceptionAddress:X16}";
        }

        private void SetExceptionRecord()
        {
            MINIDUMP_EXCEPTION_STREAM? stream = null;
            ExceptionRecord64? record64 = null;

            if (DumpControl.FileCache.isKernelDump == 0)
            {
                stream = DumpControl.GetExceptionStream();
                if (stream == null)
                    return;

                ThreadId = $"0x{stream.Value.ThreadId:X8}";
                record64 = stream.Value.ExceptionRecord;
            }
            else
            {
                FaultModule = DumpControl.GetBugCheck() ?? "N/A";
                record64 = DumpControl.GetException();
                if (record64 == null)
                    return;
            }

            SetPropertyFromExceptionRecord(record64.Value);
            GetStackList();
        }

        public override void Update()
        {
            if (!DumpControl.IsDumpFileOpen || isloaded)
                return;

            SetExceptionRecord();
            isloaded = true;

            return;
        }

        protected override void OnFileClosed(object? sender, EventArgs e)
        {
            Clear();
        }

        private void Clear()
        {
            ExceptionCode = "N/A";
            OccurredAt = "N/A";
            FaultModule = "N/A";
            FaultAddress = "N/A";
            ThreadId = "N/A";

            _stackList.Clear();
        }
    }
}
