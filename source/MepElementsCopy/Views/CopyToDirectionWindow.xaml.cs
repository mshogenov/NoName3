using System.Windows;
using System.Windows.Controls;

namespace MepElementsCopy.Views;

public partial class CopyToDirectionWindow : UserControl
{
    public CopyToDirectionWindow()
    {
        InitializeComponent();
    }

    private void UIElement_OnLostFocus(object sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (!string.IsNullOrWhiteSpace(textBox?.Text)) return;
        if (textBox != null) textBox.Text = "0";
    }
}