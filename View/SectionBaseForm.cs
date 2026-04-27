using DumpViewer.Static;
using System.ComponentModel;
using System.Data;

namespace DumpViewer.View
{
    public abstract partial class SectionBaseForm : INotifyPropertyChanged
    {
        protected bool isloaded = false;

        public abstract void Update();

        protected SectionBaseForm()
        {
            DumpControl.FileClosed += HandleFileClosed;
        }

        private void HandleFileClosed(object? sender, EventArgs e)
        {
            isloaded = false;
            OnFileClosed(sender, e);
        }

        protected virtual void OnFileClosed(object? sender, EventArgs e)
        {

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (DumpControl.IsDumpFileOpen == false)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
