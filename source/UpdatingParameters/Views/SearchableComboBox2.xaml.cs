using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace UpdatingParameters.Views;

public partial class SearchableComboBox2 : UserControl
{
    public SearchableComboBox2()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        PART_Combo.DropDownOpened += Combo_DropDownOpened;
        PART_Combo.DropDownClosed += Combo_DropDownClosed;
    }
       // Dependency properties
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SearchableComboBox),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(SearchableComboBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(SearchableComboBox),
            new PropertyMetadata(null));

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(SearchableComboBox),
            new PropertyMetadata(null));

    // По какому свойству фильтровать (если не задано — берётся DisplayMemberPath, иначе ToString())
    public static readonly DependencyProperty SearchMemberPathProperty =
        DependencyProperty.Register(nameof(SearchMemberPath), typeof(string), typeof(SearchableComboBox),
            new PropertyMetadata(null));

    // Минимальная длина строки поиска, с которой начинается фильтрация
    public static readonly DependencyProperty MinimumSearchLengthProperty =
        DependencyProperty.Register(nameof(MinimumSearchLength), typeof(int), typeof(SearchableComboBox),
            new PropertyMetadata(0));

    public static readonly DependencyProperty IgnoreCaseProperty =
        DependencyProperty.Register(nameof(IgnoreCase), typeof(bool), typeof(SearchableComboBox),
            new PropertyMetadata(true));

    // Очищать ли поисковую строку и фильтр при закрытии списка
    public static readonly DependencyProperty ClearSearchOnCloseProperty =
        DependencyProperty.Register(nameof(ClearSearchOnClose), typeof(bool), typeof(SearchableComboBox),
            new PropertyMetadata(true));

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string SearchMemberPath
    {
        get => (string)GetValue(SearchMemberPathProperty);
        set => SetValue(SearchMemberPathProperty, value);
    }

    public int MinimumSearchLength
    {
        get => (int)GetValue(MinimumSearchLengthProperty);
        set => SetValue(MinimumSearchLengthProperty, value);
    }

    public bool IgnoreCase
    {
        get => (bool)GetValue(IgnoreCaseProperty);
        set => SetValue(IgnoreCaseProperty, value);
    }

    public bool ClearSearchOnClose
    {
        get => (bool)GetValue(ClearSearchOnCloseProperty);
        set => SetValue(ClearSearchOnCloseProperty, value);
    }

    private ICollectionView _view;
    private TextBox _editBox; // внутренний текстбокс ComboBox при IsEditable=true
    private string _searchText = string.Empty;

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (SearchableComboBox)d;
        // ctrl.AttachView();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachView();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachView();
    }

    private void AttachView()
    {
        DetachView();

        if (ItemsSource == null)
            return;

        _view = CollectionViewSource.GetDefaultView(ItemsSource);
        if (_view != null)
        {
            _view.Filter = FilterPredicate;
        }
    }

    private void DetachView()
    {
        if (_view != null)
        {
            _view.Filter = null;
            _view = null;
        }
    }

    private void Combo_DropDownOpened(object sender, EventArgs e)
    {
        // Включаем режим редактирования только на время открытого списка
        PART_Combo.IsEditable = true;
        PART_Combo.IsTextSearchEnabled = false;
        PART_Combo.StaysOpenOnEdit = true;

        PART_Combo.ApplyTemplate();
        _editBox = PART_Combo.Template.FindName("PART_EditableTextBox", PART_Combo) as TextBox;
        if (_editBox != null)
        {
            _editBox.TextChanged -= EditBox_TextChanged;
            _editBox.Text = _searchText; // восстановить предыдущую строку, если была
            _editBox.TextChanged += EditBox_TextChanged;

            _editBox.Focus();
            _editBox.SelectAll();
        }

        // Раскрыли — покажем все (если строка пустая)
        RefreshView();
    }

    private void Combo_DropDownClosed(object sender, EventArgs e)
    {
        if (ClearSearchOnClose)
        {
            _searchText = string.Empty;
            RefreshView();
        }

        if (_editBox != null)
        {
            _editBox.TextChanged -= EditBox_TextChanged;
        }

        // Выключаем IsEditable, чтобы в закрытом состоянии отображался SelectedItem, а не текст поиска
        PART_Combo.IsEditable = false;
    }

    private void EditBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = _editBox?.Text ?? string.Empty;
        RefreshView();

        // Чтобы список не закрывался при вводе
        if (!PART_Combo.IsDropDownOpen)
            PART_Combo.IsDropDownOpen = true;
    }

    private void RefreshView()
    {
        _view?.Refresh();
    }

    private bool FilterPredicate(object obj)
    {
        if (obj == null) return false;

        var text = _searchText ?? string.Empty;
        if (text.Length < MinimumSearchLength)
            return true; // не фильтруем до нужной длины

        var candidate = GetItemText(obj);
        if (candidate == null) return false;

        var cmp = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return candidate.IndexOf(text, cmp) >= 0; // содержит
    }

    private string GetItemText(object item)
    {
        string path = !string.IsNullOrWhiteSpace(SearchMemberPath)
            ? SearchMemberPath
            : (!string.IsNullOrWhiteSpace(DisplayMemberPath) ? DisplayMemberPath : null);

        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                var pi = item.GetType().GetProperty(path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                var val = pi?.GetValue(item, null);
                return val?.ToString() ?? string.Empty;
            }
            catch { /* ignore */ }
        }

        return item?.ToString() ?? string.Empty;
    }  

}