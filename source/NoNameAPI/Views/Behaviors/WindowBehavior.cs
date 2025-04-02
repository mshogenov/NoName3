using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NoNameApi.Views.Behaviors;

public class WindowBehavior : Behavior<Window>
{
    // Заголовок
    public string HeaderText
    {
        get { return (string)GetValue(HeaderTextProperty); }
        set { SetValue(HeaderTextProperty, value); }
    }

    public static readonly DependencyProperty HeaderTextProperty =
        DependencyProperty.Register("HeaderText", typeof(string), typeof(WindowBehavior),
            new PropertyMetadata(string.Empty));

    // Высота заголовка
    public double HeaderHeight
    {
        get { return (double)GetValue(HeaderHeightProperty); }
        set { SetValue(HeaderHeightProperty, value); }
    }

    public static readonly DependencyProperty HeaderHeightProperty =
        DependencyProperty.Register("HeaderHeight", typeof(double), typeof(WindowBehavior),
            new PropertyMetadata(32.0));

    // Цвет заголовка
    public Brush HeaderBackground
    {
        get { return (Brush)GetValue(HeaderBackgroundProperty); }
        set { SetValue(HeaderBackgroundProperty, value); }
    }

    public static readonly DependencyProperty HeaderBackgroundProperty =
        DependencyProperty.Register("HeaderBackground", typeof(Brush), typeof(WindowBehavior),
            new PropertyMetadata(Brushes.LightGray));

    // Контрол заголовка
    private Grid _headerGrid;
    private ContentControl _windowContent;

    protected override void OnAttached()
    {
        base.OnAttached();

        // Получаем окно
        Window window = AssociatedObject;

        // Настраиваем окно
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = false;
        window.Background = Brushes.White;

        // Сохраняем оригинальный контент
        UIElement originalContent = window.Content as UIElement;

        // Создаем Grid как новый корень
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

        // Заголовок
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

        // Кнопка максимизации/восстановления
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

            // Обновляем иконку
            maximizeButton.Content = window.WindowState == WindowState.Maximized ? "❐" : "□";
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

        // Добавляем перетаскивание
        _headerGrid.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                window.DragMove();
        };

        // Область для контента
        _windowContent = new ContentControl();
        _windowContent.Content = originalContent;
        _windowContent.SetValue(Grid.RowProperty, 1);

        // Собираем все вместе
        rootGrid.Children.Add(_headerGrid);
        rootGrid.Children.Add(_windowContent);

        // Устанавливаем в окно
        window.Content = rootGrid;

        // Регистрируем обработчики
        window.Loaded += Window_Loaded;
        window.StateChanged += Window_StateChanged;
    }

    protected override void OnDetaching()
    {
        // Удаляем обработчики
        AssociatedObject.Loaded -= Window_Loaded;
        AssociatedObject.StateChanged -= Window_StateChanged;

        base.OnDetaching();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Обновляем заголовок при загрузке окна
        UpdateHeader();
    }

    private void Window_StateChanged(object sender, System.EventArgs e)
    {
        // Обновляем вид кнопок при изменении состояния окна
        UpdateHeader();
    }

    private void UpdateHeader()
    {
        // Обновляем текст заголовка
        TextBlock titleBlock = _headerGrid.Children[0] as TextBlock;
        if (titleBlock != null)
        {
            titleBlock.Text = string.IsNullOrEmpty(HeaderText) ? AssociatedObject.Title : HeaderText;
        }

        // Обновляем кнопку максимизации
        Button maximizeButton = _headerGrid.Children[2] as Button;
        if (maximizeButton != null)
        {
            maximizeButton.Content = AssociatedObject.WindowState == WindowState.Maximized ? "❐" : "□";
        }
    }
}