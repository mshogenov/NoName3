using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using UpdatingParameters.Models;
using UpdatingParameters.ViewModels;
using Point = System.Drawing.Point;

namespace UpdatingParameters.Views;

public partial class FilteringCriteriaControl : UserControl
{
    public static readonly DependencyProperty RootGroupProperty = DependencyProperty.Register(nameof(RootGroup),
        typeof(FilterGroup), typeof(FilteringCriteriaControl), new PropertyMetadata(default(FilterGroup)));

    public string SelectedCategoriesStatus { get; } = "Выберите категорию";

    public FilteringCriteriaControl()
    {
        InitializeComponent();
    }

    public FilterGroup RootGroup
    {
        get => (FilterGroup)GetValue(RootGroupProperty);
        set => SetValue(RootGroupProperty, value);
    }


    private void ButtonSelectedCategories_OnClick(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) return;

        Popup.Placement = PlacementMode.Custom;
        Popup.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
        [
            new CustomPopupPlacement(
                new System.Windows.Point(0, (int)(targetSize.Height + 5)),
                PopupPrimaryAxis.Horizontal
            )
        ];

        Popup.PlacementTarget = button;
        Popup.IsOpen = true;
    }
}