using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LastAllocation.ViewModels;

namespace LastAllocation.Views;

public sealed partial class LastAllocationView
{
    LastAllocationViewModel  _viewModel;
    public LastAllocationView(LastAllocationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        LoadWindowTemplate();
    }

    private void ButtonCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var item = ((ListBox)sender).SelectedItem;
        if (item != null && DataContext is LastAllocationViewModel viewModel)
        {
            viewModel.ApplySelectionCommand.Execute(item);
        }
    }
}