using DumpViewer.Static;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace DumpViewer.View.ModuleViews
{
    internal class ModuleListUpViewModel : SectionBaseForm
    {
        private static readonly UInt32 miniDumpString = 4;

        private static nint MappedBaseAddress => DumpControl.CachePtr;

        private ObservableCollection<ModuleItem> moduleItems = new ObservableCollection<ModuleItem>();
        public ObservableCollection<ModuleItem> Modules
        {
            get
            {
                return moduleItems;
            }

            set
            {
                if (moduleItems == value)
                    return;
                moduleItems = value;
                OnPropertyChanged(nameof(Modules));
            }
        }

        public void Clear()
        {
            Modules.Clear();
        }

        private bool GetMiniDumpModuleItems()
        {

            if(!DumpControl.GetModuleLists(ref moduleItems))
                return false;

            return true;
        }

        override public void Update()
        {
            if(isloaded)
                return;

            switch (DumpControl.FileCache.DumpMode)
            {
                case DumpType.Unknown:
                     break;
                case DumpType.miniDump:
                case DumpType.fullDump:
                    isloaded = GetMiniDumpModuleItems();
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
