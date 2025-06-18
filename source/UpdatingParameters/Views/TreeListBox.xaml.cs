using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UpdatingParameters.Views;

public partial class TreeListBox : UserControl
{
    public TreeListBox()
    {
        InitializeComponent();
    }

    // Источник данных
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(TreeListBox),
            new PropertyMetadata(null));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    // Шаблон для родительских элементов
    public static readonly DependencyProperty ParentItemTemplateProperty =
        DependencyProperty.Register("ParentItemTemplate", typeof(DataTemplate), typeof(TreeListBox),
            new PropertyMetadata(null));

    public DataTemplate ParentItemTemplate
    {
        get { return (DataTemplate)GetValue(ParentItemTemplateProperty); }
        set { SetValue(ParentItemTemplateProperty, value); }
    }

    // Шаблон для дочерних элементов
    public static readonly DependencyProperty ChildItemTemplateProperty =
        DependencyProperty.Register("ChildItemTemplate", typeof(DataTemplate), typeof(TreeListBox),
            new PropertyMetadata(null));

    public DataTemplate ChildItemTemplate
    {
        get { return (DataTemplate)GetValue(ChildItemTemplateProperty); }
        set { SetValue(ChildItemTemplateProperty, value); }
    }

    // Отступ для дочерних элементов
    public static readonly DependencyProperty ChildIndentProperty =
        DependencyProperty.Register("ChildIndent", typeof(Thickness), typeof(TreeListBox),
            new PropertyMetadata(new Thickness(40, 0, 0, 0)));

    public Thickness ChildIndent
    {
        get { return (Thickness)GetValue(ChildIndentProperty); }
        set { SetValue(ChildIndentProperty, value); }
    }

    // Показывать ли кнопки разворачивания
    public static readonly DependencyProperty ShowExpanderProperty =
        DependencyProperty.Register("ShowExpander", typeof(bool), typeof(TreeListBox),
            new PropertyMetadata(true));

    public bool ShowExpander
    {
        get { return (bool)GetValue(ShowExpanderProperty); }
        set { SetValue(ShowExpanderProperty, value); }
    }

    // Стиль для кнопки разворачивания
    public static readonly DependencyProperty ExpanderStyleProperty =
        DependencyProperty.Register("ExpanderStyle", typeof(Style), typeof(TreeListBox),
            new PropertyMetadata(null));

    public Style ExpanderStyle
    {
        get { return (Style)GetValue(ExpanderStyleProperty); }
        set { SetValue(ExpanderStyleProperty, value); }
    }

    // Стиль для границ элементов
    public static readonly DependencyProperty ItemBorderStyleProperty =
        DependencyProperty.Register("ItemBorderStyle", typeof(Style), typeof(TreeListBox),
            new PropertyMetadata(null));

    public Style ItemBorderStyle
    {
        get { return (Style)GetValue(ItemBorderStyleProperty); }
        set { SetValue(ItemBorderStyleProperty, value); }
    }

    // Выбранный элемент
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register("SelectedItem", typeof(object), typeof(TreeListBox),
            new PropertyMetadata(null, OnSelectedItemChanged));

    public object SelectedItem
    {
        get { return GetValue(SelectedItemProperty); }
        set { SetValue(SelectedItemProperty, value); }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreeListBox treeListBox)
        {
            treeListBox.MainListBox.SelectedItem = e.NewValue;
        }
    }

    // Событие изменения выбранного элемента
    public static readonly RoutedEvent SelectionChangedEvent =
        EventManager.RegisterRoutedEvent("SelectionChanged", RoutingStrategy.Bubble,
            typeof(SelectionChangedEventHandler), typeof(TreeListBox));

    public event SelectionChangedEventHandler SelectionChanged
    {
        add { AddHandler(SelectionChangedEvent, value); }
        remove { RemoveHandler(SelectionChangedEvent, value); }
    }

    private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItem = MainListBox.SelectedItem;
        RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, e.RemovedItems, e.AddedItems));
    }

    // Переопределяем свойства для передачи в ListBox
    public new Brush Background
    {
        get { return (Brush)GetValue(BackgroundProperty); }
        set { SetValue(BackgroundProperty, value); }
    }

    public new Brush BorderBrush
    {
        get { return (Brush)GetValue(BorderBrushProperty); }
        set { SetValue(BorderBrushProperty, value); }
    }

    public new Thickness BorderThickness
    {
        get { return (Thickness)GetValue(BorderThicknessProperty); }
        set { SetValue(BorderThicknessProperty, value); }
    }
}