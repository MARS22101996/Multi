using Microsoft.WindowsAPICodePack.Dialogs;

namespace HashCalculator.Infrastructure.Interfaces
{
    public interface IFileDialogConfigurer
    {
        string OpenFileDialog();

        void ConfigureFileDialog(CommonOpenFileDialog openFileDialog);
    }
}
