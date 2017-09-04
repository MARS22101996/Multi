using System.Windows;
using HashCalculator.ViewModels;

namespace HashCalculator
{
    public partial class MainWindow : Window
    {
	    public MainWindow()
        {
	        InitializeComponent();

            var model = new FilesCalculatorViewModel();

            DataContext = model;
        }
    }
}
