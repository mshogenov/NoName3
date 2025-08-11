using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NoNameApi.Views.Controls;

namespace UpdatingParameters.Views
{
    public partial class SearchableComboBox : UserControl
    {
        private ICollectionView _collectionView;
     
        public SearchableComboBox()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable),
                typeof(SearchableComboBox),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object),
                typeof(SearchableComboBox),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemChanged));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate),
                typeof(SearchableComboBox),
                new PropertyMetadata(null, OnItemTemplateChanged));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchableComboBox control && control.ItemsListBox != null)
            {
                control.ItemsListBox.ItemTemplate = e.NewValue as DataTemplate;
            }
        }

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string),
                typeof(SearchableComboBox),
                new PropertyMetadata(string.Empty, OnDisplayMemberPathChanged));

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(nameof(PlaceholderText), typeof(string),
                typeof(SearchableComboBox),
                new PropertyMetadata("Выберите элемент..."));

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        #endregion

        #region Event Handlers

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateItemsSource();
            UpdateDisplayText();
          
            // Обработка клика вне контрола
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseDown += Window_PreviewMouseDown;
            }
            if (ItemsListBox != null)
            {
                ItemsListBox.AddHandler(PreviewMouseLeftButtonDownEvent, 
                    new MouseButtonEventHandler(ListBoxItem_Click), true);
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Popup.IsOpen) return;
            // Проверяем, был ли клик внутри нашего контрола или Popup
            var hitTest = VisualTreeHelper.HitTest(this, e.GetPosition(this));
            var popupHitTest = VisualTreeHelper.HitTest(Popup.Child as Visual, e.GetPosition(Popup.Child));
        
            if (hitTest == null && popupHitTest == null)
            {
                Popup.IsOpen = false;
            }
        }

        private void Popup_Closed(object sender, EventArgs e)
        {
            SearchTextBox.Clear();
            if (_collectionView != null)
            {
                _collectionView.Filter = null;
            }
        }

        private void Popup_Opened(object sender, EventArgs e)
        {
            // Фокус на поле поиска при открытии
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                SearchTextBox.Clear();
                SearchTextBox.Focus();
            }));
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SearchableComboBox;
            control?.UpdateItemsSource();
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SearchableComboBox;
            if (control != null)
            {
                control.UpdateDisplayText();
            }
        }
     
        private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SearchableComboBox;
            if (control != null && control.ItemsListBox != null)
            {
                control.ItemsListBox.DisplayMemberPath = e.NewValue as string;
            }
        }

        private void DisplayTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DisplayTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Popup.IsOpen = !Popup.IsOpen;
            e.Handled = true;
        }

        private void SearchTextBox_TextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            var searchBox = sender as SearchBox;
            if (searchBox != null && ItemsListBox != null)
            {
                FilterItems(searchBox.Text);
            }
         
        }

        private void ItemsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
            if (ItemsListBox.SelectedItem != null)
            {
                var selectedItem = ItemsListBox.SelectedItem;
        
                // Используем SetCurrentValue вместо SetValue
                SetCurrentValue(SelectedItemProperty, selectedItem);
        
                Popup.IsOpen = false;
        
                Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    ItemsListBox.SelectedItem = null;
                }));
            }
        }

        #endregion

        #region Private Methods

        private void UpdateItemsSource()
        {
            if (ItemsSource != null)
            {
                _collectionView = CollectionViewSource.GetDefaultView(ItemsSource);
                ItemsListBox.ItemsSource = _collectionView;

                // Устанавливаем ItemTemplate если есть, иначе DisplayMemberPath
                if (ItemTemplate != null)
                {
                    ItemsListBox.ItemTemplate = ItemTemplate;
                }
                else
                {
                    ItemsListBox.DisplayMemberPath = DisplayMemberPath;
                }
            }
        }

        private void FilterItems(string searchBoxText)
        {
            if (_collectionView != null)
            {
                _collectionView.Filter = item =>
                {
                    if (string.IsNullOrEmpty(searchBoxText))
                        return true;

                    string itemText = GetItemText(item);
                    return itemText.ToLower().Contains(searchBoxText.ToLower());
                };
            }
        }

        private string GetItemText(object item)
        {
            if (item == null)
                return string.Empty;

            // Если есть DisplayMemberPath, используем его
            if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                var property = item.GetType().GetProperty(DisplayMemberPath);
                if (property != null)
                    return property.GetValue(item)?.ToString() ?? string.Empty;
            }

            // Для DataTemplate пытаемся найти свойство Name или Title
            var nameProperty = item.GetType().GetProperty("Name")
                               ?? item.GetType().GetProperty("Title");
            if (nameProperty != null)
                return nameProperty.GetValue(item)?.ToString() ?? string.Empty;

            return item.ToString();
        }


        private void UpdateDisplayText()
        {
            var text = GetItemText(SelectedItem);
            DisplayTextBox.Text = SelectedItem != null ? text : PlaceholderText;
        }

        #endregion

        private void DropDownButton_Click(object sender, RoutedEventArgs e)
        {
            Popup.IsOpen = !Popup.IsOpen;
        }

        private void ListBoxItem_Click(object sender, MouseButtonEventArgs e)
        {
            var originalSource = e.OriginalSource as DependencyObject;
            if (originalSource != null)
            {
                var listBoxItem = FindParent<ListBoxItem>(originalSource);
                if (listBoxItem != null)
                {
                    SelectedItem = listBoxItem.DataContext;
                    DisplayTextBox.Text = GetItemText(SelectedItem);
            
                    Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                    {
                        Popup.IsOpen = false;
                    }));
                }
            }
        }
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
    
            if (parentObject == null) return null;
    
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }
    }
}