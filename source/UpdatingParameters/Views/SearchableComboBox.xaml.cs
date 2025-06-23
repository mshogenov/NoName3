using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace UpdatingParameters.Views;

public partial class SearchableComboBox : UserControl
{
    // Внутренняя коллекция для отфильтрованных элементов
    private readonly ObservableCollection<object> _filteredItems = new ObservableCollection<object>();
    private bool _isInternalUpdate = false; // Флаг для предотвращения рекурсивных обновлений

    public SearchableComboBox()
    {
        InitializeComponent();
        ItemsListBox.ItemsSource = _filteredItems;
    }

    #region Dependency Properties (API для MVVM)

    // 1. ItemsSource: Полная коллекция всех элементов для поиска
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    // 2. SelectedItem: Выбранный элемент (с поддержкой TwoWay биндинга)
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemChanged));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    // 3. DisplayMemberPath: Свойство объекта, которое нужно отображать (как в обычном ComboBox)
    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(SearchableComboBox),
            new PropertyMetadata(string.Empty));

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    #endregion

    #region PropertyChanged Callbacks

    // Вызывается, когда ViewModel меняет ItemsSource
    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableComboBox)d;
        control.UpdateFilter();
    }

    // Вызывается, когда ViewModel меняет SelectedItem
    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableComboBox)d;
        if (control._isInternalUpdate) return;

        control.UpdateTextFromSelectedItem();
    }

    #endregion

    #region Event Handlers (Внутренняя логика)

    // При изменении текста в TextBox - фильтруем список
    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInternalUpdate) return;

        UpdateFilter();
        ItemsPopup.IsOpen = true;
    }

    // При выборе элемента в ListBox - обновляем SelectedItem и закрываем Popup
    private void ItemsListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ItemsListBox.SelectedItem == null) return;

        _isInternalUpdate = true;
        SelectedItem = ItemsListBox.SelectedItem; // Это вызовет OnSelectedItemChanged
        _isInternalUpdate = false;

        UpdateTextFromSelectedItem();
        ItemsPopup.IsOpen = false;
    }

    #endregion

    #region Private Methods

    // Основная логика фильтрации
    private void UpdateFilter()
    {
        _filteredItems.Clear();
        if (ItemsSource == null) return;

        string searchText = SearchTextBox.Text.ToLower();

        foreach (var item in ItemsSource)
        {
            string displayValue = GetDisplayValue(item)?.ToString().ToLower() ?? "";
            if (string.IsNullOrEmpty(searchText) || displayValue.Contains(searchText))
            {
                _filteredItems.Add(item);
            }
        }
    }

    // Обновляет текст в TextBox на основе выбранного элемента
    private void UpdateTextFromSelectedItem()
    {
        _isInternalUpdate = true;
        SearchTextBox.Text = SelectedItem != null ? GetDisplayValue(SelectedItem)?.ToString() : string.Empty;
        _isInternalUpdate = false;
    }

    // Получает значение для отображения из объекта с помощью DisplayMemberPath
    private object GetDisplayValue(object item)
    {
        if (item == null || string.IsNullOrEmpty(DisplayMemberPath))
        {
            return item;
        }

        // Используем рефлексию для получения значения свойства
        var property = item.GetType().GetProperty(DisplayMemberPath);
        return property?.GetValue(item, null);
    }

    #endregion
}