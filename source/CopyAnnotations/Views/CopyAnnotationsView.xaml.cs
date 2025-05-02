using System.Windows;
using CopyAnnotations.ViewModels;
using NoNameApi.Views;

namespace CopyAnnotations.Views;

public sealed partial class CopyAnnotationsView : BaseRevitWindow
{
    public CopyAnnotationsView(CopyAnnotationsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        LoadWindowTemplate();
    }

    private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}