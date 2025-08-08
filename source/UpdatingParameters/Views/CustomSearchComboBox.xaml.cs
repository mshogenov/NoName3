using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Binding = System.Windows.Data.Binding;


namespace UpdatingParameters.Views;

  public partial class CustomSearchComboBox : UserControl
    {
        private CollectionView _itemsView;

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), 
                typeof(CustomSearchComboBox), new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), 
                typeof(CustomSearchComboBox), new FrameworkPropertyMetadata(null, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), 
                typeof(CustomSearchComboBox));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), 
                typeof(CustomSearchComboBox), new PropertyMetadata(string.Empty));

        public CustomSearchComboBox()
        {
            InitializeComponent();
        }

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CustomSearchComboBox)d;
            if (e.NewValue != null)
            {
                control._itemsView = CollectionViewSource.GetDefaultView(e.NewValue) as CollectionView;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTextBox = sender as TextBox;
            
            if (_itemsView != null)
            {
                _itemsView.Filter = item =>
                {
                    if (string.IsNullOrEmpty(searchTextBox.Text))
                        return true;

                    string itemText = GetItemText(item);
                    return itemText.IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
                };
            }
        }

        private string GetItemText(object item)
        {
            if (item == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(DisplayMemberPath))
            {
                var property = item.GetType().GetProperty(DisplayMemberPath);
                if (property != null)
                {
                    var value = property.GetValue(item);
                    return value?.ToString() ?? string.Empty;
                }
            }

            return item.ToString();
        }
    }