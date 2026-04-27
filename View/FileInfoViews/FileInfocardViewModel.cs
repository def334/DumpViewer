using DumpViewer.Static;
using System.Text;

namespace DumpViewer.View.FileInfoViews
{
    class FileInfocardViewModel : SectionBaseForm
    {

        private string _dumpHeader = "";
        public string? DumpHeader
        {
            get { return _dumpHeader; }
            set
            {
                if (_dumpHeader == value || value == null)
                    return;

                _dumpHeader = value;
                OnPropertyChanged(nameof(DumpHeader));
            }
        }

        private string _exception = "";
        public string? Exception
        {
            get { return _exception; }
            set
            {
                if (_exception == value || value == null)
                    return;

                _exception = value;
                OnPropertyChanged(nameof(Exception));
            }
        }

        public override void Update()
        {
            if(isloaded)
                return;

            StringBuilder[]? sb = DumpControl.GetDumpHeader() ?? null;
            if (sb == null || sb.Length == 0)
                return;

            DumpHeader = sb[0].ToString();
            Exception = sb[1].ToString();
            isloaded = true;
        }

        protected override void OnFileClosed(object? sender, EventArgs e)
        {
            DumpHeader = string.Empty;
            Exception = string.Empty;
        }
    }
}
