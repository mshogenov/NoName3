using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace NoNameApi.Views;

public class BaseRevitWindow : Window
{
    protected BaseRevitWindow()
    {
        try
        {
            // Базовые настройки
            WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
           ResizeMode = ResizeMode.CanResizeWithGrip;
           
            // Загружаем шаблон
            LoadWindowTemplate();

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

    private void LoadWindowTemplate()
    {
        // Загружаем ResourceDictionary с шаблоном
        ResourceDictionary resourceDict = new ResourceDictionary();
        resourceDict.Source =
            new Uri("pack://application:,,,/NoNameAPI;component/Views/Resources/Themes/LightTheme.xaml",
                UriKind.Absolute);

        // Добавляем словарь ресурсов
        Resources.MergedDictionaries.Add(resourceDict);

        // Применяем шаблон
        Template = Resources["CustomWindowTemplate"] as ControlTemplate;
    }

    private void BaseRevitWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Удаляем обработчик Loaded, чтобы не вызывался повторно
        Loaded -= BaseRevitWindow_Loaded;

        // Находим заголовок и добавляем обработчик для перетаскивания
        if (Template.FindName("PART_HeaderGrid", this) is Grid headerGrid)
        {
            headerGrid.MouseLeftButtonDown += (s, args) => {
                if (args.ButtonState == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };
        }
    }
    private void BaseRevitWindow_SourceInitialized(object sender, EventArgs e)
    {
        // Добавляем обработчик сообщений Windows для изменения размера
        IntPtr handle = (new WindowInteropHelper(this)).Handle;
        HwndSource source = HwndSource.FromHwnd(handle);
        source.AddHook(WndProc);
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
        int borderWidth = 8;

        if (msg == WM_NCHITTEST)
        {
            Point ptScreen = new Point(
                (double)((int)(lParam) & 0xFFFF),
                (double)((int)(lParam) >> 16)
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
                else if (ptClient.Y >= ActualHeight - borderWidth)
                {
                    handled = true;
                    return (IntPtr)HTBOTTOMLEFT;
                }
                handled = true;
                return (IntPtr)HTLEFT;
            }
            else if (ptClient.X >= ActualWidth - borderWidth)
            {
                if (ptClient.Y <= borderWidth)
                {
                    handled = true;
                    return (IntPtr)HTTOPRIGHT;
                }
                else if (ptClient.Y >= ActualHeight - borderWidth)
                {
                    handled = true;
                    return (IntPtr)HTBOTTOMRIGHT;
                }
                handled = true;
                return (IntPtr)HTRIGHT;
            }
            else if (ptClient.Y <= borderWidth)
            {
                handled = true;
                return (IntPtr)HTTOP;
            }
            else if (ptClient.Y >= ActualHeight - borderWidth)
            {
                handled = true;
                return (IntPtr)HTBOTTOM;
            }
        }

        return IntPtr.Zero;
    }
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Привязываем обработчики к кнопкам управления окном
        if (GetTemplateChild("MinimizeButton") is Button minimizeButton)
        {
            minimizeButton.Click += (s, e) => WindowState = WindowState.Minimized;
        }

        if (GetTemplateChild("MaximizeButton") is Button maximizeButton)
        {
            maximizeButton.Click += (s, e) => 
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
            closeButton.Click += (s, e) => Close();
        }

        // Добавляем перетаскивание окна через заголовок
        if (GetTemplateChild("PART_HeaderGrid") is Grid headerGrid)
        {
            headerGrid.MouseLeftButtonDown += (s, e) => 
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };
        }
    }
}