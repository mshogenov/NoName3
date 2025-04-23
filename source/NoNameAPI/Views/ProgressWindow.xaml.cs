using System.Windows;
using System.Windows.Threading;
using Autodesk.Revit.UI;

namespace NoNameApi.Views;

public partial class ProgressWindow 
{
    private int _totalSteps;
    private bool _isCanceled;

    public ProgressWindow(int totalSteps)
    {
        InitializeComponent();
        LoadWindowTemplate();
        _isCanceled = false;
        _totalSteps = totalSteps;
        ProgressBar.Maximum = totalSteps;
        ProgressBar.Minimum = 0;
        ProgressBar.Value = 0;
        ProgressText.Text = "0%";
    }
    public void UpdateProgress(int currentStep)
    {
        ProgressBar.Value = currentStep;
        int percentage = (int)((double)currentStep / _totalSteps * 100);
        ProgressText.Text = $"{percentage}%";
        DoEvents();
        if (currentStep >= _totalSteps)
        {
            Close();
        }
    }

    private void DoEvents()
    {
        DispatcherFrame frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            new DispatcherOperationCallback(ExitFrame), frame);
        Dispatcher.PushFrame(frame);
    }

    private object ExitFrame(object frame)
    {
        ((DispatcherFrame)frame).Continue = false;
        return null;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _isCanceled = true;
        Close();
    }

    public bool IsCancelling
    {
        get { return _isCanceled; }
    }

    public void UpdateCurrentTask(string taskName)
    {
        CurrentTaskText.Text = taskName;
        DoEvents();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}