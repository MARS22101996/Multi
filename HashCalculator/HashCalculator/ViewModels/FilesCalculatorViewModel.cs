using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Autofac;
using HashCalculator.BLL.Interfaces;
using HashCalculator.BLL.Models;
using HashCalculator.Commands;
using HashCalculator.Infrastructure;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace HashCalculator.ViewModels
{
    public class FilesCalculatorViewModel : INotifyPropertyChanged
    {
        private string[] _filePaths;

        private ICommand _calculateCommand;

        private ICommand _cancelCommand;

        private List<FileInformation> _filesInfo;

        private ICalculatorService _calculatorService;

        public List<FileInformation> FilesInfo
        {
            get { return _filesInfo; }
            set
            {
                _filesInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilesInfo"));
            }
        }

        private int _progressValue;

        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                if (value == _progressValue)
                    return;

                _progressValue = value;
                OnPropertyChanged();
            }
        }

        private int _progressMax = 100;

        public int ProgressMax
        {
            get { return _progressMax; }
            set
            {
                if (value == _progressMax)
                    return;

                _progressMax = value;
                OnPropertyChanged();
            }
        }

        public ICommand CalculateCommand => _calculateCommand ?? (_calculateCommand = new Command(parameter =>
        {		
            var path = OpenFileDialog();

            if (!string.IsNullOrEmpty(path))
            {
                Task.Run(() => ConfigureFileInfo(path));
            }
        }));

        public ICommand CancelCommand => _cancelCommand ?? (_cancelCommand = new Command(parameter => { _calculatorService.Cancel(); }));


        public FilesCalculatorViewModel()
        {
            DiSetup.Initialize();

            FilesInfo = new List<FileInformation>();

            _calculatorService = DiSetup.Container.Resolve<ICalculatorService>(); ;
        }

        private void ConfigureFileInfo(string path)
        {
            ConfigureStartData();
            _calculatorService.ClearXml();
            _calculatorService.RestoreToken();

            _filePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();

            ProgressMax = _filePaths.Length;

            foreach (var filePath in _filePaths)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var info = _calculatorService.GetFileInfo(stream, filePath);

                        InputOfResultsIntoTheControl(info, _calculatorService.CancelToken.Token);
                        _calculatorService.RecordResultsInAnXmlFile(_calculatorService.CancelToken.Token);
                        WriteToTheProgressBar(_calculatorService.CancelToken.Token);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"An error ocurred while executing the data reading from file: {e.Message}", e);
                }

            }
        }

        private void ConfigureStartData()
        {
            FilesInfo.Clear();

            _calculatorService.ResetCollection();

            ProgressValue = 0;
        }

        private string OpenFileDialog()
        {
            var openFileDialog = new CommonOpenFileDialog();
            ConfigureFileDialog(openFileDialog);
            var path = openFileDialog.FileName;

            return path;
        }

        private void ConfigureFileDialog(CommonOpenFileDialog openFileDialog)
        {
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog.IsFolderPicker = true;

            openFileDialog.ShowDialog();
        }

        private void InputOfResultsIntoTheControl(FileInformation file, CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                _calculatorService.AddFile(file);
                FilesInfo = _calculatorService.Files.ToList();

            }, cancellationToken);

            _calculatorService.HandleExceptionsIfExists(task);
        }

        private void WriteToTheProgressBar(CancellationToken cancellationToken)
        {
            var task = Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                ProgressValue++), cancellationToken);

            _calculatorService.HandleExceptionsIfExists(task);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}