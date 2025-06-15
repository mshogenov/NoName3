using System.Windows;
using System.Windows.Controls;
using UpdatingParameters.Models;
using UpdatingParameters.ViewModels;

namespace UpdatingParameters.Views;

public partial class FilteringCriteriaControl : UserControl
{
    public static readonly DependencyProperty RootGroupProperty = DependencyProperty.Register(nameof(RootGroup),
        typeof(FilterGroup), typeof(FilteringCriteriaControl), new PropertyMetadata(default(FilterGroup)));

    public FilteringCriteriaControl()
    {
        InitializeComponent();
    }

    public FilterGroup RootGroup
    {
        get => (FilterGroup)GetValue(RootGroupProperty);
        set => SetValue(RootGroupProperty, value);
    }
}