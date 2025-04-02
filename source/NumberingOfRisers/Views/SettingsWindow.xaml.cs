using NumberingOfRisers.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace NumberingOfRisers.Views
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class SettingsWindow : UserControl
    {
        public SettingsWindow()
        {

            InitializeComponent();
           
          
        }

        // Событие для закрытия меню
        public event EventHandler CloseRequested;

        // Обработчик кнопки "Закрыть"
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Вызываем событие
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
