using System;
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
        private ObservableCollection<FileInformation> _filesCollection;

        private CancellationTokenSource _cancellationTokenSource;

        private readonly object _lockObject = new object();

        private const string XmlFileName = "\\FilesInfo.xml";

        public ObservableCollection<FileInformation> Files => _filesCollection;

        public CancellationTokenSource CancelToken => _cancellationTokenSource;

        public CalculatorService()
        {
            _filesCollection = new ObservableCollection<FileInformation>();

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


        public void RecordResultsInAnXmlFile(CancellationToken cancellationToken)
        {
            var writer = new XmlSerializer(typeof(List<FileInformation>));
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = folder + XmlFileName;

            var task = Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            writer.Serialize(file, _filesCollection.ToList());
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"An error ocurred while executing the data writing to the file: {e.Message}", e);
                    }
                }
            }, cancellationToken);

            HandleExceptionsIfExists(task);
        }

        public void HandleExceptionsIfExists(Task task)
        {
            task.ContinueWith(t => t.Exception?.Flatten(), TaskContinuationOptions.OnlyOnFaulted);
        }


        public void ClearXml()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var path = folder + XmlFileName;

            var serializer = new XmlSerializer(typeof(List<FileInformation>));

            try
            {
                using (var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    stream.SetLength(0);
                    serializer.Serialize(stream, new List<FileInformation>());
                }
            }
            catch (Exception e)
            {
                throw new Exception($"An error ocurred while executing the data clearing in the file: {e.Message}", e);
            }
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public void AddFile(FileInformation file)
        {
            _filesCollection.Add(file);
        }

        public void ResetCollection()
        {
            _filesCollection = new ObservableCollection<FileInformation>();
        }
    }
}