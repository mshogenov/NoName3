using System.Windows.Controls;
using System.Windows.Input;
using Autodesk.Revit.UI;
using NoNameApi.Views;
using PositionNumbering.ViewModels;
using Grid = Autodesk.Revit.DB.Grid;

namespace PositionNumbering.Views;

public partial class PositionNumberingWindow
{
    public PositionNumberingWindow(NumberingViewModel viewModel)

    {
        InitializeComponent();
        DataContext = viewModel;
       LoadWindowTemplate();
    }

    private void UIElement_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }
}