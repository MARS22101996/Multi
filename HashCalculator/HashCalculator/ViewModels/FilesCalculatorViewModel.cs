﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
using HashCalculator.Infrastructure.Interfaces;

namespace HashCalculator.ViewModels
{
    public class FilesCalculatorViewModel : INotifyPropertyChanged
    {
        private string[] _filePaths;

        private ICommand _calculateCommand;

        private ICommand _cancelCommand;

        private List<FileInformation> _filesInfo;

        private readonly ICalculatorService _calculatorService;

        private readonly IFileDialogConfigurer _fileDialogConfigurer;

        private readonly object _lockObject = new object();

        public FilesCalculatorViewModel()
        {
            DiSetup.Initialize();

            _fileDialogConfigurer = DiSetup.Container.Resolve<IFileDialogConfigurer>();

            FilesInfo = new List<FileInformation>();

            _calculatorService = DiSetup.Container.Resolve<ICalculatorService>();
        }

        public List<FileInformation> FilesInfo
        {
            get { return _filesInfo; }
            set
            {
                _filesInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilesInfo)));
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

        private bool _chooseButtonIsEnabled = true;

        public bool ChooseButtonIsEnabled
        {
            get { return _chooseButtonIsEnabled; }
            set
            {
                if (value == _chooseButtonIsEnabled)
                    return;

                _chooseButtonIsEnabled = value;
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
            var path = _fileDialogConfigurer.OpenFileDialog();

            if (!string.IsNullOrEmpty(path))
            {
              Task.Run(() => ConfigureFileInfoAsync(path));
            }
        }));

        public ICommand CancelCommand => _cancelCommand ?? (_cancelCommand = new Command(parameter =>
        {
            _calculatorService.Cancel();

            EnableDisableChooseButton(true);
        }));

        private void ConfigureFileInfoAsync(string path)
        {
            ConfigureStartData();

            _calculatorService.RestoreToken();

            EnableDisableChooseButton(false);

            _filePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();

            ProgressMax = _filePaths.Length;

            CollectData(_calculatorService.CancelToken.Token);
            InputOfResultsIntoTheControls(_calculatorService.CancelToken.Token);
            _calculatorService.RecordResultsInAnXmlFile(_calculatorService.CancelToken.Token, ProgressMax);
        }

        private void ConfigureStartData()
        {
            FilesInfo.Clear();

            _calculatorService.ResetCollection();

            Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                ProgressValue = 0));
        }

        private void EnableDisableChooseButton(bool isEnabled)
        {
            Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                ChooseButtonIsEnabled = isEnabled));
        }


        private void InputOfResultsIntoTheControls(CancellationToken cancellationToken)
        {
            var task = Task.Run(async () =>
            {
                while (true)
                {
                    if (FilesInfo.Count == ProgressMax)
                    {
                        EnableDisableChooseButton(true);

                        break;
                    }

                    await Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                    {
                        FilesInfo = _calculatorService.Files.ToList();

                    }), cancellationToken);

                    await Task.Run(() => Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressValue = FilesInfo.Count;

                    }), cancellationToken);

                    await Task.Delay(100, cancellationToken);

                }
            }, cancellationToken);

            _calculatorService.HandleExceptionsIfExists(task);
        }

        private void CollectData(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                foreach (var filePath in _filePaths)
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var info = _calculatorService.GetFileInfo(stream, filePath);

                        Task.Run(() =>
                        {
                            _calculatorService.AddFile(info);

                        }, cancellationToken);
                    }
                }

            }, cancellationToken);

            _calculatorService.HandleExceptionsIfExists(task);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}