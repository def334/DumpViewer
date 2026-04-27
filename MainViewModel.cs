using DumpViewer.Static;
using DumpViewer.View;
using DumpViewer.View.FileInfoViews;
using DumpViewer.View.ModuleViews;
using DumpViewer.View.ExceptionViews;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DumpViewer.View.ThreadViews;

namespace DumpViewer
{
    internal class MainViewModel : INotifyPropertyChanged
    {
        private readonly OpenFileDialog _openFileDialog;

        private ModuleListUpViewModel ModuleListUpViewModel { get; } = new ModuleListUpViewModel();
        private FileInfocardViewModel FileInfocardViewModel { get; } = new FileInfocardViewModel();
        private ExceptionViewModel ExceptionViewModel { get; } = new ExceptionViewModel();

        private ThreadListViewModel ThreadListViewModel { get; } = new ThreadListViewModel();

        private string _currentFileStatus = "열린 파일 없음";
        private string _fileStatus = "파일 없음";
        private string _fileStatusFormat = "";

        private bool _hasLoadedFile;
        private string? _selectedDumpSection;
        private SectionBaseForm? _currentSectionView;

        public string CurrentFileStatus
        {
            get => _currentFileStatus;
            private set
            {
                if (_currentFileStatus == value) return;
                _currentFileStatus = value;
                OnPropertyChanged(nameof(CurrentFileStatus));
            }
        }

        public string FileStatus
        {
            get => _fileStatus;
            private set
            {
                if (_fileStatus == value) return;
                _fileStatus = value;
                OnPropertyChanged(nameof(FileStatus));
            }
        }

        public string FileStatusFormat
        {
            get => _fileStatusFormat;
            set
            {
                if (_fileStatusFormat == value) return;
                _fileStatusFormat = value;
                OnPropertyChanged(nameof(FileStatusFormat));
            }
        }

        public bool HasLoadedFile
        {
            get => _hasLoadedFile;
            private set
            {
                if (_hasLoadedFile == value) return;
                _hasLoadedFile = value;
                OnPropertyChanged(nameof(HasLoadedFile));
            }
        }

        public ObservableCollection<string> DumpSections { get; } = new()
        {
            "파일 개요",
            "크래시 정보",
            "모듈 목록",
            "스레드 상태",
            "콜스택"
        };

        public string? SelectedDumpSection
        {
            get => _selectedDumpSection;
            set
            {
                if (_selectedDumpSection == value) return;
                _selectedDumpSection = value;
                OnPropertyChanged(nameof(SelectedDumpSection));
                UpdateCurrentSectionView();
            }
        }

        public SectionBaseForm? CurrentSectionView
        {
            get => _currentSectionView;
            private set
            {
                if (_currentSectionView == value) return;
                _currentSectionView = (SectionBaseForm?)value;
                _currentSectionView?.Update();
                OnPropertyChanged(nameof(CurrentSectionView));
            }
        }

        public ICommand OpenFileCommand { get; }

        public MainViewModel()
        {
            _openFileDialog = new OpenFileDialog
            {
                Filter = "Dump files (*.dmp)|*.dmp|All files (*.*)|*.*",
                Multiselect = false,
                Title = "덤프 파일 선택",
            };

            OpenFileCommand = new RelayCommand(_ => OpenDumpFiles_Click());

        }
        private void ClearCurrentFile()
        {
            CurrentFileStatus = "열린 파일 없음";
            FileStatus = "파일 없음";
            FileStatusFormat = "";
            HasLoadedFile = false;
            SelectedDumpSection = null;
            CurrentSectionView = null;
        }

        private void OpenDumpFiles_Click()
        {
            _openFileDialog.FileName = string.Empty;

            if (_openFileDialog.ShowDialog() != true) return;

            string filePath = _openFileDialog.FileName;

            if (HasLoadedFile)
                ClearCurrentFile();

            if (!FileSystem.OpenDump(filePath))
            {
                MessageBox.Show("덤프 파일을 여는 데 실패했습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SettingCurrentFileStatus(filePath);

            SelectedDumpSection = DumpSections[0];
        }

        private bool SettingCurrentFileStatus(string filePath)
        {
            try
            {
                List<string>? fileHeader = DumpControl.GetFileStatus();
                if (fileHeader == null)
                    throw new Exception("덤프 파일의 헤더를 읽는 데 실패했습니다.");
                CurrentFileStatus = fileHeader[0];
                FileStatusFormat = $"{fileHeader[1]} | {fileHeader[2]} | {fileHeader[3]}";
                FileStatus = "준비";
                HasLoadedFile = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"덤프 파일을 처리하는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                ClearCurrentFile();
                return false;
            }
        }

        private void UpdateCurrentSectionView()
        {
            CurrentSectionView = SelectedDumpSection switch
            {
                "파일 개요" => FileInfocardViewModel,
                "크래시 정보" => ExceptionViewModel,
                "모듈 목록" => ModuleListUpViewModel,
                "스레드 상태" => ThreadListViewModel,
                _ => null
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
