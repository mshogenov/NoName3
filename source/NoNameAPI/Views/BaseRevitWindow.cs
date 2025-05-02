using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Autodesk.Revit.UI;
using NoNameApi.Views.Services;

namespace NoNameApi.Views;

public class BaseRevitWindow : Window
{
    // Свойства для управления видимостью кнопок
    public bool ShowMinimizeButton
    {
        get => (bool)GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public static readonly DependencyProperty ShowMinimizeButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMinimizeButton),
            typeof(bool),
            typeof(BaseRevitWindow),
            new PropertyMetadata(false, OnShowMinimizeButtonChanged));

    private static void OnShowMinimizeButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BaseRevitWindow window && window.GetTemplateChild("MinimizeButton") is Button button)
        {
            button.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public bool ShowMaximizeButton
    {
        get => (bool)GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public static readonly DependencyProperty ShowMaximizeButtonProperty =
        DependencyProperty.Register(
            nameof(ShowMaximizeButton),
            typeof(bool),
            typeof(BaseRevitWindow),
            new PropertyMetadata(false, OnShowMaximizeButtonChanged));

    private static void OnShowMaximizeButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BaseRevitWindow window && window.GetTemplateChild("MaximizeButton") is Button button)
        {
            button.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    protected BaseRevitWindow()
    {
        try
        {
            // Базовые настройки
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            // Регистрируем обработчик Loaded
            Loaded += BaseRevitWindow_Loaded;

            SourceInitialized += BaseRevitWindow_SourceInitialized;
        }
        catch (Exception ex)
        {
            // Установим стандартные настройки для восстановления
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
    }

    public static bool IsDarkTheme()
    {
        try
        {
            // Получаем текущую тему
            UITheme currentTheme = UIThemeManager.CurrentCanvasTheme;

            // Проверяем, является ли тема темной
            return currentTheme == UITheme.Dark;
        }
        catch
        {
            return false;
        }
    }
    private bool _stylesLoaded;

    protected void LoadWindowTemplate()
    {
        if (_stylesLoaded) return;
      // Обновляем текущую тему в менеджере
        RevitThemeManager.UpdateCurrentTheme();

        // Получаем словарь ресурсов текущей темы
        ResourceDictionary themeDictionary = RevitThemeManager.GetCurrentThemeDictionary();
        Resources.MergedDictionaries.Add(themeDictionary);

        // Затем добавляем стили
        ResourceDictionary stylesDictionary = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/NoNameAPI;component/Views/Resources/Themes/Styles.xaml",
                UriKind.Absolute)
        };
        Resources.MergedDictionaries.Add(stylesDictionary);

        // Применяем шаблон окна если он определен
        if (Resources.Contains("CustomWindowTemplate"))
        {
            Template = Resources["CustomWindowTemplate"] as ControlTemplate;
        }
        ShowMinimizeButton = false;
        ShowMaximizeButton = false;
        _stylesLoaded = true;
    }

    private void BaseRevitWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Удаляем обработчик Loaded, чтобы не вызывался повторно
        Loaded -= BaseRevitWindow_Loaded;

        // Находим заголовок и добавляем обработчик для перетаскивания
        if (Template.FindName("PART_HeaderGrid", this) is Grid headerGrid)
        {
            headerGrid.MouseLeftButtonDown += (_, args) =>
            {
                if (args.ButtonState == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };
        }
    }

    private void BaseRevitWindow_SourceInitialized(object sender, EventArgs e)
    {
        // Добавляем обработчик сообщений Windows для изменения размера
        IntPtr handle = (new WindowInteropHelper(this)).Handle;
        HwndSource source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

// Методы для включения/отключения кнопок
    public void EnableMinimizeButton()
    {
        ShowMinimizeButton = true;
    }

    public void DisableMinimizeButton()
    {
        ShowMinimizeButton = false;
    }

    public void EnableMaximizeButton()
    {
        ShowMaximizeButton = true;
    }

    public void DisableMaximizeButton()
    {
        ShowMaximizeButton = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;

        // Ширина невидимой рамки для изменения размера
        const int borderWidth = 8;
        // Смещение области изменения размера (отрицательное значение смещает наружу)
        const int offsetBorder = -4;

        if (msg != WM_NCHITTEST) return IntPtr.Zero;
        Point ptScreen = new Point(
            (int)(lParam) & 0xFFFF,
            (int)(lParam) >> 16
        );

        Point ptClient = PointFromScreen(ptScreen);

        // Проверяем, находится ли курсор в области изменения размера
        // Используем смещение, чтобы сдвинуть область ближе к границе
        if (ptClient.X <= borderWidth + offsetBorder)
        {
            if (ptClient.Y <= borderWidth + offsetBorder)
            {
                handled = true;
                return (IntPtr)HTTOPLEFT;
            }

            if (ptClient.Y >= ActualHeight - (borderWidth + offsetBorder))
            {
                handled = true;
                return (IntPtr)HTBOTTOMLEFT;
            }

            handled = true;
            return (IntPtr)HTLEFT;
        }

        if (ptClient.X >= ActualWidth - (borderWidth + offsetBorder))
        {
            if (ptClient.Y <= borderWidth + offsetBorder)
            {
                handled = true;
                return (IntPtr)HTTOPRIGHT;
            }

            if (ptClient.Y >= ActualHeight - (borderWidth + offsetBorder))
            {
                handled = true;
                return (IntPtr)HTBOTTOMRIGHT;
            }

            handled = true;
            return (IntPtr)HTRIGHT;
        }

        if (ptClient.Y <= borderWidth + offsetBorder)
        {
            handled = true;
            return (IntPtr)HTTOP;
        }

        if (!(ptClient.Y >= ActualHeight - (borderWidth + offsetBorder))) return IntPtr.Zero;
        handled = true;
        return (IntPtr)HTBOTTOM;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Находим кнопки по их именам в шаблоне
        Button minimizeButton = GetTemplateChild("MinimizeButton") as Button;
        Button maximizeButton = GetTemplateChild("MaximizeButton") as Button;
        Button closeButton = GetTemplateChild("CloseButton") as Button;

        // Устанавливаем видимость кнопок в соответствии с флагами
        if (minimizeButton != null)
        {
            minimizeButton.Visibility = ShowMinimizeButton ? Visibility.Visible : Visibility.Collapsed;
            minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        }

        if (maximizeButton != null)
        {
            maximizeButton.Visibility = ShowMaximizeButton ? Visibility.Visible : Visibility.Collapsed;
            maximizeButton.Click += (_, _) =>
            {
                WindowState = (WindowState == WindowState.Maximized)
                    ? WindowState.Normal
                    : WindowState.Maximized;

                // Обновляем иконку кнопки
                maximizeButton.Content = (WindowState == WindowState.Maximized) ? "\uE923" : "\uE922";
            };
        }

        if (closeButton != null)
        {
            closeButton.Click += (_, _) => Close();
        }

        // Добавляем перетаскивание окна через заголовок
        if (GetTemplateChild("PART_HeaderGrid") is Grid headerGrid)
        {
            headerGrid.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };
        }
    }
}