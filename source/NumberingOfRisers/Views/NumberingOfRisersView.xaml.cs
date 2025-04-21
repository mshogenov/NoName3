using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NumberingOfRisers.ViewModels;

namespace NumberingOfRisers.Views;

public partial class NumberingOfRisersView 
{
    public NumberingOfRisersView(NumberingOfRisersViewModel viewModel)
    {
        InitializeComponent();
        LoadWindowTemplate();
        DataContext = viewModel;
    }

    private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Если мы кликнули на кнопку, предотвращаем обработку события TreeViewItem
        if (e.OriginalSource is Button || 
            VisualTreeHelper.GetParent(e.OriginalSource as DependencyObject ?? throw new InvalidOperationException()) is Button)
        {
            e.Handled = true;
        }
    }
}