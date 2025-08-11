using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NoNameApi.Views.Controls;

namespace UpdatingParameters.Views;

public partial class ParameterSelectorPopup : UserControl
{
    private ICollectionView _collectionViewInstanceParameters;
    private ICollectionView _collectionViewTypeParameters;
    private string _currentSearchText = string.Empty;

    public ParameterSelectorPopup()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #region Dependency Properties

    public static readonly DependencyProperty ItemsSourceInstanceParametersProperty = DependencyProperty.Register(
        nameof(ItemsSourceInstanceParameters), typeof(IEnumerable), typeof(ParameterSelectorPopup),
        new PropertyMetadata(null, OnItemsSourceInstanceParametersChanged));

    public IEnumerable ItemsSourceInstanceParameters
    {
        get => (IEnumerable)GetValue(ItemsSourceInstanceParametersProperty);
        set => SetValue(ItemsSourceInstanceParametersProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceTypeParametersProperty = DependencyProperty.Register(
        nameof(ItemsSourceTypeParameters), typeof(IEnumerable), typeof(ParameterSelectorPopup),
        new PropertyMetadata(null, OnItemsSourceTypeParametersChanged));

    public IEnumerable ItemsSourceTypeParameters
    {
        get => (IEnumerable)GetValue(ItemsSourceTypeParametersProperty);
        set => SetValue(ItemsSourceTypeParametersProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(ParameterSelectorPopup),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedParameterChanged));

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(ParameterSelectorPopup),
        new PropertyMetadata(null, OnItemTemplateChanged));

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
        nameof(DisplayMemberPath), typeof(string), typeof(ParameterSelectorPopup),
        new PropertyMetadata(null, OnDisplayMemberPathChanged));

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public static readonly DependencyProperty ClosePopupCommandProperty =
        DependencyProperty.Register(nameof(ClosePopupCommand), typeof(ICommand), typeof(ParameterSelectorPopup),
            new PropertyMetadata(default(ICommand)));

    public ICommand ClosePopupCommand
    {
        get => (ICommand)GetValue(ClosePopupCommandProperty);
        set => SetValue(ClosePopupCommandProperty, value);
    }

    public static readonly DependencyProperty CurrentPopupTargetProperty =
        DependencyProperty.Register(nameof(CurrentPopupTarget), typeof(UIElement), typeof(ParameterSelectorPopup),
            new PropertyMetadata(default(UIElement)));

    public UIElement CurrentPopupTarget
    {
        get => (UIElement)GetValue(CurrentPopupTargetProperty);
        set => SetValue(CurrentPopupTargetProperty, value);
    }

    public static readonly DependencyProperty IsPopupOpenProperty =
        DependencyProperty.Register(nameof(IsPopupOpen), typeof(bool), typeof(ParameterSelectorPopup),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsPopupOpenChanged));

    public bool IsPopupOpen
    {
        get => (bool)GetValue(IsPopupOpenProperty);
        set => SetValue(IsPopupOpenProperty, value);
    }

    #endregion

    #region Property Changed Handlers

    private static void OnItemsSourceInstanceParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as ParameterSelectorPopup;
        control?.UpdateItemsSourceInstanceParameters();
    }

    private static void OnItemsSourceTypeParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = d as ParameterSelectorPopup;
        control?.UpdateItemsSourceTypeParameters();
    }

    private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ParameterSelectorPopup control)
        {
            var displayPath = e.NewValue as string;
        
            if (control.InstanceParametersListBox != null)
            {
                // Очищаем ItemTemplate перед установкой DisplayMemberPath
                control.InstanceParametersListBox.ItemTemplate = null;
                control.InstanceParametersListBox.DisplayMemberPath = displayPath;
            }
        
            if (control.TypeParametersListBox != null)
            {
                // Очищаем ItemTemplate перед установкой DisplayMemberPath
                control.TypeParametersListBox.ItemTemplate = null;
                control.TypeParametersListBox.DisplayMemberPath = displayPath;
            }
        }
    }

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ParameterSelectorPopup control)
        {
            var template = (DataTemplate)e.NewValue;
        
            if (control.InstanceParametersListBox != null)
            {
                // Очищаем DisplayMemberPath перед установкой ItemTemplate
                control.InstanceParametersListBox.DisplayMemberPath = null;
                control.InstanceParametersListBox.ItemTemplate = template;
            }

            if (control.TypeParametersListBox != null)
            {
                // Очищаем DisplayMemberPath перед установкой ItemTemplate
                control.TypeParametersListBox.DisplayMemberPath = null;
                control.TypeParametersListBox.ItemTemplate = template;
            }
        }
    }

    private static void OnSelectedParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ParameterSelectorPopup)d;
        if (e.NewValue != null && control.IsPopupOpen)
        {
            // Закрываем popup при выборе с небольшой задержкой
            control.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => { control.IsPopupOpen = false; }));
        }
    }

    private static void OnIsPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ParameterSelectorPopup)d;
        if ((bool)e.NewValue)
        {
            // При открытии popup очищаем поиск
            control._currentSearchText = string.Empty;
            control.ApplyFilter();

            // Очищаем SearchBox
            control.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => { control.ClearSearchBox(); }));
        }
    }

    private void ClearSearchBox()
    {
        // Находим SearchBox в визуальном дереве
        var searchBox = FindVisualChild<FrameworkElement>(this, "SearchBox");
        if (searchBox != null)
        {
            try
            {
                // Пытаемся установить свойство Text
                var textProperty = searchBox.GetType().GetProperty("Text");
                if (textProperty != null && textProperty.CanWrite)
                {
                    textProperty.SetValue(searchBox, string.Empty);
                }
            }
            catch
            {
            }
        }
    }

    private T FindVisualChild<T>(DependencyObject parent, string name = null) where T : FrameworkElement
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                if (string.IsNullOrEmpty(name) || typedChild.Name == name)
                    return typedChild;
            }

            var result = FindVisualChild<T>(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    #endregion

    #region Event Handlers

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateItemsSourceInstanceParameters();
        UpdateItemsSourceTypeParameters();
        // Обработка клика вне контрола
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewMouseDown += Window_PreviewMouseDown;
        }

        SetupListBoxes();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewMouseDown -= Window_PreviewMouseDown;
        }
    }

    private void SetupListBoxes()
    {
        if (InstanceParametersListBox != null)
        {
            InstanceParametersListBox.AddHandler(PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(ListBoxItem_Click), true);
            InstanceParametersListBox.SelectionChanged += ListBox_SelectionChanged;
        }

        if (TypeParametersListBox != null)
        {
            TypeParametersListBox.AddHandler(PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(ListBoxItem_Click), true);
            TypeParametersListBox.SelectionChanged += ListBox_SelectionChanged;
        }
    }

    private void SetupSearchBox()
    {
        var searchBox = FindSearchBox();
        if (searchBox != null)
        {
            var textBox = searchBox.FindName("SearchTextBox") as TextBox;
            if (textBox != null)
            {
                textBox.TextChanged += SearchBox_TextChanged;
            }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentSearchText = (sender as TextBox)?.Text;
        ApplyFilter();
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            SelectedItem = listBox.SelectedItem;
        }
    }

    private void ListBoxItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject originalSource)
        {
            var listBoxItem = FindParent<ListBoxItem>(originalSource);
            if (listBoxItem != null)
            {
                SelectedItem = listBoxItem.DataContext;
            }
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!Popup.IsOpen) return;

        // Проверяем, был ли клик внутри нашего контрола или Popup
        var hitTest = VisualTreeHelper.HitTest(this, e.GetPosition(this));
        var popupHitTest =
            Popup.Child != null ? VisualTreeHelper.HitTest(Popup.Child, e.GetPosition(Popup.Child)) : null;

        if (hitTest == null && popupHitTest == null)
        {
            IsPopupOpen = false;
        }
    }

    #endregion

    #region Private Methods

    private void UpdateItemsSourceInstanceParameters()
    {
        if (ItemsSourceInstanceParameters != null && InstanceParametersListBox != null)
        {
            _collectionViewInstanceParameters = CollectionViewSource.GetDefaultView(ItemsSourceInstanceParameters);
            if (_collectionViewInstanceParameters != null)
            {
                _collectionViewInstanceParameters.Filter = FilterPredicate;
            }
        
            InstanceParametersListBox.ItemsSource = _collectionViewInstanceParameters;
        
            // Приоритет: ItemTemplate > DisplayMemberPath
            if (ItemTemplate != null)
            {
                InstanceParametersListBox.DisplayMemberPath = null; // Очищаем DisplayMemberPath
                InstanceParametersListBox.ItemTemplate = ItemTemplate;
            }
            else if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                InstanceParametersListBox.ItemTemplate = null; // Очищаем ItemTemplate
                InstanceParametersListBox.DisplayMemberPath = DisplayMemberPath;
            }
            else
            {
                // Если ничего не задано, очищаем оба свойства
                InstanceParametersListBox.ItemTemplate = null;
                InstanceParametersListBox.DisplayMemberPath = null;
            }
        }
    }

    private void UpdateItemsSourceTypeParameters()
    {
        if (ItemsSourceTypeParameters != null && TypeParametersListBox != null)
        {
            _collectionViewTypeParameters = CollectionViewSource.GetDefaultView(ItemsSourceTypeParameters);
            if (_collectionViewTypeParameters != null)
            {
                _collectionViewTypeParameters.Filter = FilterPredicate;
            }
        
            TypeParametersListBox.ItemsSource = _collectionViewTypeParameters;
        
            // Приоритет: ItemTemplate > DisplayMemberPath
            if (ItemTemplate != null)
            {
                TypeParametersListBox.DisplayMemberPath = null; // Очищаем DisplayMemberPath
                TypeParametersListBox.ItemTemplate = ItemTemplate;
            }
            else if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                TypeParametersListBox.ItemTemplate = null; // Очищаем ItemTemplate
                TypeParametersListBox.DisplayMemberPath = DisplayMemberPath;
            }
            else
            {
                // Если ничего не задано, очищаем оба свойства
                TypeParametersListBox.ItemTemplate = null;
                TypeParametersListBox.DisplayMemberPath = null;
            }
        }
    }

    private void ApplyFilter()
    {
        // Фильтруем параметры экземпляра
        if (_collectionViewInstanceParameters != null)
        {
            // Используем BeginInvoke для избежания проблем с обновлением UI
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _collectionViewInstanceParameters.Filter = FilterPredicate;
                _collectionViewInstanceParameters.Refresh();
            }));
        }

        // Фильтруем параметры типа
        if (_collectionViewTypeParameters != null)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                _collectionViewTypeParameters.Filter = FilterPredicate;
                _collectionViewTypeParameters.Refresh();
            }));
        }
    }

    private bool FilterPredicate(object item)
    {
        if (string.IsNullOrWhiteSpace(_currentSearchText))
            return true;

        string itemText = GetItemText(item);
        if (string.IsNullOrEmpty(itemText))
            return false;

        // Регистронезависимый поиск
        return itemText.IndexOf(_currentSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private FrameworkElement FindSearchBox()
    {
        return FindChild<FrameworkElement>(Popup.Child, "SearchBox");
    }

    private T FindChild<T>(DependencyObject parent, string childName = null) where T : FrameworkElement
    {
        if (parent == null) return null;

        T foundChild = null;
        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild && (string.IsNullOrEmpty(childName) || typedChild.Name == childName))
            {
                foundChild = typedChild;
                break;
            }

            foundChild = FindChild<T>(child, childName);
            if (foundChild != null) break;
        }

        return foundChild;
    }

    private T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);

        if (parentObject == null) return null;

        return parentObject is T parent ? parent : FindParent<T>(parentObject);
    }

    #endregion

    private void SearchBox_OnTextChanged(object sender, RoutedEventArgs e)
    {
        string searchText = string.Empty;

        // Пробуем получить текст через рефлексию
        if (sender != null)
        {
            var textProperty = sender.GetType().GetProperty("Text");
            if (textProperty != null)
            {
                searchText = textProperty.GetValue(sender)?.ToString() ?? string.Empty;
            }
        }

        _currentSearchText = searchText;

        // Применяем фильтр к обоим спискам
        ApplyFilter();
    }


    private string GetItemText(object item)
    {
        if (item == null)
            return string.Empty;

        // Если item это строка, возвращаем её
        if (item is string str)
            return str;

        // Сначала пытаемся получить Definition.Name (для параметров Revit)
        try
        {
            var definitionProperty = item.GetType().GetProperty("Definition");
            if (definitionProperty != null)
            {
                var definition = definitionProperty.GetValue(item);
                if (definition != null)
                {
                    var nameProperty = definition.GetType().GetProperty("Name");
                    if (nameProperty != null)
                    {
                        var name = nameProperty.GetValue(definition)?.ToString();
                        if (!string.IsNullOrEmpty(name))
                            return name;
                    }
                }
            }
        }
        catch
        {
        }

        // Если есть DisplayMemberPath, используем его
        if (!string.IsNullOrEmpty(DisplayMemberPath))
        {
            try
            {
                // Поддержка вложенных свойств (например, "Definition.Name")
                var parts = DisplayMemberPath.Split('.');
                object currentValue = item;

                foreach (var part in parts)
                {
                    if (currentValue == null)
                        break;

                    var property = currentValue.GetType().GetProperty(part);
                    if (property != null)
                    {
                        currentValue = property.GetValue(currentValue);
                    }
                    else
                    {
                        currentValue = null;
                        break;
                    }
                }

                if (currentValue != null)
                    return currentValue.ToString();
            }
            catch
            {
            }
        }

        // Пытаемся найти общие свойства для отображения
        var possibleProperties = new[] { "Name", "Title", "DisplayName", "Text", "Value" };
        foreach (var propName in possibleProperties)
        {
            try
            {
                var property = item.GetType().GetProperty(propName);
                if (property != null)
                {
                    var value = property.GetValue(item);
                    if (value != null)
                        return value.ToString();
                }
            }
            catch
            {
            }
        }

        // Используем ToString() как последний вариант
        return item.ToString();
    }
}