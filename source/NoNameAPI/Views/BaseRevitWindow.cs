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

        if (msg != WM_NCHITTEST) return IntPtr.Zero;
        Point ptScreen = new Point(
            (int)(lParam) & 0xFFFF,
            (int)(lParam) >> 16
        );

        Point ptClient = PointFromScreen(ptScreen);

        // Проверяем, находится ли курсор в области изменения размера
        if (ptClient.X <= borderWidth)
        {
            if (ptClient.Y <= borderWidth)
            {
                handled = true;
                return (IntPtr)HTTOPLEFT;
            }

            if (ptClient.Y >= ActualHeight - borderWidth)
            {
                handled = true;
                return (IntPtr)HTBOTTOMLEFT;
            }

            handled = true;
            return (IntPtr)HTLEFT;
        }

        if (ptClient.X >= ActualWidth - borderWidth)
        {
            if (ptClient.Y <= borderWidth)
            {
                handled = true;
                return (IntPtr)HTTOPRIGHT;
            }

            if (ptClient.Y >= ActualHeight - borderWidth)
            {
                handled = true;
                return (IntPtr)HTBOTTOMRIGHT;
            }

            handled = true;
            return (IntPtr)HTRIGHT;
        }

        if (ptClient.Y <= borderWidth)
        {
            handled = true;
            return (IntPtr)HTTOP;
        }

        if (!(ptClient.Y >= ActualHeight - borderWidth)) return IntPtr.Zero;
        handled = true;
        return (IntPtr)HTBOTTOM;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        // Привязываем обработчики к кнопкам управления окном
        if (GetTemplateChild("MinimizeButton") is Button minimizeButton)
        {
            minimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        }

        if (GetTemplateChild("MaximizeButton") is Button maximizeButton)
        {
            maximizeButton.Click += (_, _) =>
            {
                WindowState = (WindowState == WindowState.Maximized)
                    ? WindowState.Normal
                    : WindowState.Maximized;

                // Обновляем иконку кнопки
                maximizeButton.Content = (WindowState == WindowState.Maximized) ? "\uE923" : "\uE922";
            };
        }

        if (GetTemplateChild("CloseButton") is Button closeButton)
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