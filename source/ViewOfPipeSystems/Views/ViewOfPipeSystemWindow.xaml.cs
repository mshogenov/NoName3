using System.Windows;
using ViewOfPipeSystems.ViewModels;

namespace ViewOfPipeSystems.Views;

public partial class ViewOfPipeSystemWindow 
{
    public ViewOfPipeSystemWindow(ViewOfPipeSystemsVM viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        LoadWindowTemplate();
    }
}