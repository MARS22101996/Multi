using System;
using HashCalculator.Infrastructure.Interfaces;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace HashCalculator.Infrastructure
{
    public class FileDialogConfigurer : IFileDialogConfigurer
    {
        public string OpenFileDialog()
        {
            var path = string.Empty;
            var openFileDialog = new CommonOpenFileDialog();
            ConfigureFileDialog(openFileDialog);

            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                path = openFileDialog.FileName;
            }

            return path;
        }

        public void ConfigureFileDialog(CommonOpenFileDialog openFileDialog)
        {
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog.IsFolderPicker = true;
        }
    }
}
