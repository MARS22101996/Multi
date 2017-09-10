using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HashCalculator.BLL.Models;

namespace HashCalculator.BLL.Interfaces
{
    public interface ICalculatorService
    {
        ObservableCollection<FileInformation> Files { get; }

        CancellationTokenSource CancelToken { get; }

        void RestoreToken();

        FileInformation GetFileInfo(Stream stream, string filePath);

        Task RecordResultsInAnXmlFile(CancellationToken cancellationToken);

        void HandleExceptionsIfExists(Task task);

        void Cancel();

        void AddFile(FileInformation file);

        void ResetCollection();
    }
}