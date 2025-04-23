using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NumberingOfRisers.ViewModels;

namespace NumberingOfRisers.Views;

public partial class NumberingOfRisersView 
{
    private readonly NumberingOfRisersViewModel _viewModel;
    public NumberingOfRisersView(NumberingOfRisersViewModel viewModel)
    {
        InitializeComponent();
        LoadWindowTemplate();
        _viewModel = viewModel;
        DataContext = viewModel;
        // Подписываемся на событие закрытия окна
        Closing += SaveSettings;
    }

    private void SaveSettings(object sender, EventArgs e)
    {
        _viewModel.SaveSettings();
    }
   
}