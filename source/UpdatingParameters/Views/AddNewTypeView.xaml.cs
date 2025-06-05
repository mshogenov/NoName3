using System.Windows;
using UpdatingParameters.ViewModels;

namespace UpdatingParameters.Views;

public partial class AddNewTypeView : Window
{
    public AddNewTypeView(AddNewTypeVM viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}