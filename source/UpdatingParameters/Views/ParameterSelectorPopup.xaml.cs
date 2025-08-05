using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UpdatingParameters.Views;

public partial class ParameterSelectorPopup : UserControl
{
    public static readonly DependencyProperty ClosePopupCommandProperty =
        DependencyProperty.Register(nameof(ClosePopupCommand), typeof(ICommand), typeof(ParameterSelectorPopup),
            new PropertyMetadata(default(ICommand)));

    public static readonly DependencyProperty TypeParametersProperty =
        DependencyProperty.Register(nameof(TypeParameters), typeof(IEnumerable), typeof(ParameterSelectorPopup),
            new PropertyMetadata(default(IEnumerable)));

    public static readonly DependencyProperty SelectedParameterProperty =
        DependencyProperty.Register(nameof(SelectedParameter), typeof(object), typeof(ParameterSelectorPopup),
            new PropertyMetadata(null, OnSelectedParameterChanged));

    public static readonly DependencyProperty InstanceParametersProperty =
        DependencyProperty.Register(nameof(InstanceParameters), typeof(IEnumerable), typeof(ParameterSelectorPopup),
            new PropertyMetadata(default(IEnumerable)));

    public static readonly DependencyProperty CurrentPopupTargetProperty =
        DependencyProperty.Register(nameof(CurrentPopupTarget), typeof(UIElement), typeof(ParameterSelectorPopup),
            new PropertyMetadata(default(UIElement)));

    public static readonly DependencyProperty IsPopupOpenProperty =
        DependencyProperty.Register(nameof(IsPopupOpen), typeof(bool), typeof(ParameterSelectorPopup),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ParameterSelectorPopup()
    {
        InitializeComponent();
    }
    private static void OnSelectedParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ParameterSelectorPopup)d;
        if (e.NewValue != null)
        {
            // Закрываем popup при выборе
            control.IsPopupOpen = false;
        }
    }

    public bool IsPopupOpen
    {
        get => (bool)GetValue(IsPopupOpenProperty);
        set => SetValue(IsPopupOpenProperty, value);
    }

    public UIElement CurrentPopupTarget
    {
        get => (UIElement)GetValue(CurrentPopupTargetProperty);
        set => SetValue(CurrentPopupTargetProperty, value);
    }

    public IEnumerable InstanceParameters
    {
        get => (IEnumerable)GetValue(InstanceParametersProperty);
        set => SetValue(InstanceParametersProperty, value);
    }

    public object SelectedParameter
    {
        get => (object)GetValue(SelectedParameterProperty);
        set => SetValue(SelectedParameterProperty, value);
    }

    public IEnumerable TypeParameters
    {
        get => (IEnumerable)GetValue(TypeParametersProperty);
        set => SetValue(TypeParametersProperty, value);
    }

    public ICommand ClosePopupCommand
    {
        get => (ICommand)GetValue(ClosePopupCommandProperty);
        set => SetValue(ClosePopupCommandProperty, value);
    }
}