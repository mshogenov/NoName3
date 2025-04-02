using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoNameApi.Views.Behaviors;

public class CustomWindowBehavior
{
    // Свойства поведения
    public double HeaderHeight { get; set; } = 32.0;
    public Brush HeaderBackground { get; set; } = Brushes.LightGray;
    public string HeaderText { get; set; } = string.Empty;

    // Элементы управления
    private Grid _headerGrid;
    private ContentControl _windowContent;
    private Window _window;

    // Метод прикрепления поведения
    public void Attach(Window window)
    {
        try
        {
            // Сохраняем ссылку на окно
            _window = window;

            // Настраиваем базовые параметры окна
            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.CanResizeWithGrip;
            window.Background = Brushes.White;
            // Сохраняем оригинальный контент
            UIElement originalContent = window.Content as UIElement;
            // Создаем главную сетку
            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(HeaderHeight) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            // Создаем заголовок
            _headerGrid = new Grid();
            _headerGrid.Background = HeaderBackground;
            _headerGrid.SetValue(Grid.RowProperty, 0);

            // Добавляем колонки для кнопок
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            // Текст заголовка
            TextBlock titleText = new TextBlock();
            titleText.Text = string.IsNullOrEmpty(HeaderText) ? window.Title : HeaderText;
            titleText.Margin = new Thickness(10, 0, 0, 0);
            titleText.VerticalAlignment = VerticalAlignment.Center;
            titleText.SetValue(Grid.ColumnProperty, 0);
            _headerGrid.Children.Add(titleText);
            // Кнопка минимизации
            Button minimizeButton = new Button();
            minimizeButton.Content = "_";
            minimizeButton.Width = 46;
            minimizeButton.Height = HeaderHeight;
            minimizeButton.SetValue(Grid.ColumnProperty, 1);
            minimizeButton.Click += (s, e) => window.WindowState = WindowState.Minimized;
            _headerGrid.Children.Add(minimizeButton);
            // Кнопка максимизации
            Button maximizeButton = new Button();
            maximizeButton.Content = "□";
            maximizeButton.Width = 46;
            maximizeButton.Height = HeaderHeight;
            maximizeButton.SetValue(Grid.ColumnProperty, 2);
            maximizeButton.Click += (s, e) =>
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            };
            _headerGrid.Children.Add(maximizeButton);
            // Кнопка закрытия
            Button closeButton = new Button();
            closeButton.Content = "✕";
            closeButton.Width = 46;
            closeButton.Height = HeaderHeight;
            closeButton.SetValue(Grid.ColumnProperty, 3);
            closeButton.Click += (s, e) => window.Close();
            _headerGrid.Children.Add(closeButton);
            // Область для контента
            _windowContent = new ContentControl();
            _windowContent.SetValue(Grid.RowProperty, 1);
            _windowContent.Content = originalContent;
            // Добавляем в корневую сетку
            rootGrid.Children.Add(_headerGrid);
            rootGrid.Children.Add(_windowContent);

            // Устанавливаем корневую сетку как содержимое окна
            window.Content = rootGrid;

            // Добавляем перетаскивание
            _headerGrid.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    window.DragMove();
                }
            };

            // Обновление заголовка при изменении Title
            DependencyPropertyDescriptor
                .FromProperty(Window.TitleProperty, typeof(Window))
                .AddValueChanged(window, (s, e) => UpdateTitle());
        }
        catch (Exception ex)
        {
            // Восстанавливаем стандартный стиль окна в случае ошибки
            if (_window != null)
            {
                _window.WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }
    }

    private void UpdateTitle()
    {
        if (_headerGrid != null && _headerGrid.Children.Count > 0 && _headerGrid.Children[0] is TextBlock titleBlock)
        {
            titleBlock.Text = string.IsNullOrEmpty(HeaderText) ? _window.Title : HeaderText;
        }
    }
}