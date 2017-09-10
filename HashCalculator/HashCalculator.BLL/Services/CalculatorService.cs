using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HashCalculator.BLL.Interfaces;
using HashCalculator.BLL.Models;

namespace HashCalculator.BLL.Services
{
    public class CalculatorService : ICalculatorService
    {
        private ConcurrentQueue<FileInformation> _filesCollection;

        private CancellationTokenSource _cancellationTokenSource;

        private readonly object _lockObject = new object();

        private const string XmlFileName = "FilesInfo.xml";

        public ConcurrentQueue<FileInformation> Files => _filesCollection;

        public CancellationTokenSource CancelToken => _cancellationTokenSource;

        public CalculatorService()
        {
            _filesCollection = new ConcurrentQueue<FileInformation>();

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void RestoreToken()
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }
        }

        public FileInformation GetFileInfo(Stream stream, string filePath)
        {
            var info = new FileInformation();

            try
            {
                using (var hashAlgorithm = MD5.Create())
                {
                    info.Path = Path.GetDirectoryName(filePath);
                    info.Hash = Encoding.Default.GetString(hashAlgorithm.ComputeHash(stream));
                    info.Length = stream.Length;
                    info.Name = Path.GetFileName(filePath);
                    stream.Close();

                    hashAlgorithm.Clear();
                }
            }
            catch (Exception e)
            {
                throw new Exception($"An error ocurred while executing the generating of hash: {e.Message}", e);
            }

            return info;
        }


        public Task RecordResultsInAnXmlFile(CancellationToken cancellationToken, int maxValue)
        {
            var writer = new XmlSerializer(typeof(List<FileInformation>));
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = Path.Combine(folder, XmlFileName);
            List<FileInformation> infos;

            var task = Task.Run(async () =>
            {
                using (var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                {
                    while (true)
                    {
                        infos = _filesCollection.ToList();

                        writer.Serialize(file, infos);

                        await Task.Delay(100, cancellationToken);

                        if (_filesCollection.Count == maxValue)
                        {
                            break;
                        }
                    }
                }
            }, cancellationToken);

            HandleExceptionsIfExists(task);

            return task;
        }

        public void HandleExceptionsIfExists(Task task)
        {
            task.ContinueWith(t => t.Exception?.Flatten(), TaskContinuationOptions.OnlyOnFaulted);
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public void AddFile(FileInformation file)
        {
            _filesCollection.Enqueue(file);
        }

        public void ResetCollection()
        {
            _filesCollection = new ConcurrentQueue<FileInformation>();
        }
    }
}