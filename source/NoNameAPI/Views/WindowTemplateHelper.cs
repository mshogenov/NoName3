using System.Windows;
using System.Windows.Input;

namespace NoNameApi.Views;

public static class WindowTemplateHelper
{
    public static void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && 
            element.TemplatedParent is Window window && 
            e.ClickCount == 1)
        {
            window.DragMove();
        }
        else if (sender is FrameworkElement element2 && 
                 element2.TemplatedParent is Window window2 && 
                 e.ClickCount == 2)
        {
            window2.WindowState = window2.WindowState == WindowState.Maximized ? 
                WindowState.Normal : WindowState.Maximized;
        }
    }

    public static void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && 
            element.TemplatedParent is Window window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    public static void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && 
            element.TemplatedParent is Window window)
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? 
                WindowState.Normal : WindowState.Maximized;
        }
    }

    public static void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && 
            element.TemplatedParent is Window window)
        {
            window.Close();
        }
    }
}