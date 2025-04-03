using System.Windows;
using UpdatingParameters.Models;

namespace UpdatingParameters.Views.Parameters;

public partial class DuctParameterDialog : Window
{
    private DuctParameters _parameters;
    public DuctParameterDialog(DuctParameters parameters)
    {
        InitializeComponent();
        _parameters = parameters;
        DataContext = _parameters;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        // Проверка валидации
        if (string.IsNullOrWhiteSpace(_parameters.Material) ||
            string.IsNullOrWhiteSpace(_parameters.Shape) ||
            _parameters.Size <= 0 ||
            _parameters.Thickness <= 0)
        {
            MessageBox.Show("Пожалуйста, заполните все обязательные поля корректно", 
                "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}