using System.Windows;
using System.Windows.Controls;

namespace NoNameApi.Views;

public partial class CustomDialogWindow
{
    
    public string Instruction { get; set; }
    public int Result { get; private set; } = 0; // 0 = Cancel

    public CustomDialogWindow(string title, string instruction, params (string text, int id)[] commands)
    {
        InitializeComponent();

        Title = title;
        Instruction = instruction;
        DataContext = this;
        LoadWindowTemplate();
        foreach (var command in commands)
        {
            var button = new Button
            {
                Content = command.text,
                Tag = command.id,
                Margin = new Thickness(0, 0, 0, 10),
                Style = FindResource("CommonButtonStyle2") as Style
            };

            button.Click += CommandButton_Click;
            CommandsPanel.Children.Add(button);
          
        }
        
    }

    private void CommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            Result = (int)button.Tag;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = 0; // Cancel
        DialogResult = false;
        Close();
    }

    public static int ShowDialog(string title, string instruction, params (string text, int id)[] commands)
    {
        var dialog = new CustomDialogWindow(title, instruction, commands);
        return dialog.ShowDialog() == true ? dialog.Result : 0;
    }
}