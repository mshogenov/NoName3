using System.Windows;
using System.Windows.Input;
using MakeBreak.ViewModels;

namespace MakeBreak.Views;

public sealed partial class MakeBreakView
{
    public MakeBreakView(MakeBreakViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        LoadWindowTemplate();
    }
   

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}