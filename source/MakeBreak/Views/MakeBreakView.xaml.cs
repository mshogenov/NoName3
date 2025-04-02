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
    }
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}