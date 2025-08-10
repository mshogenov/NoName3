using System.Windows;
using System.Windows.Controls;
using Control = System.Windows.Controls.Control;

namespace NoNameApi.Views.Controls
{
    public class SearchBox : Control
    {
        private TextBox _searchTextBox;

        static SearchBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchBox), 
                new FrameworkPropertyMetadata(typeof(SearchBox)));
        }

        #region Dependency Properties

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchBox),
                new FrameworkPropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchBox),
                new PropertyMetadata("Поиск..."));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(SearchBox),
                new PropertyMetadata("🔍"));

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(nameof(ShowIcon), typeof(bool), typeof(SearchBox),
                new PropertyMetadata(true));

        public bool ShowIcon
        {
            get => (bool)GetValue(ShowIconProperty);
            set => SetValue(ShowIconProperty, value);
        }

        #endregion

        #region Events

        public static readonly RoutedEvent TextChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(TextChanged), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(SearchBox));

        public event RoutedEventHandler TextChanged
        {
            add { AddHandler(TextChangedEvent, value); }
            remove { RemoveHandler(TextChangedEvent, value); }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Отписываемся от старых обработчиков
            if (_searchTextBox != null)
            {
                _searchTextBox.TextChanged -= OnSearchTextBoxTextChanged;
            }

            // Получаем элементы из шаблона
            _searchTextBox = GetTemplateChild("PART_SearchTextBox") as TextBox;
            var clearButton = GetTemplateChild("PART_ClearButton") as Button;

            // Подписываемся на события
            if (_searchTextBox != null)
            {
                _searchTextBox.TextChanged += OnSearchTextBoxTextChanged;
                _searchTextBox.Text = Text;
            }

            if (clearButton != null)
            {
                clearButton.Click += OnClearButtonClick;
            }
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            Clear();
        }


        private void OnSearchTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            Text = _searchTextBox.Text;
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var searchBox = (SearchBox)d;
            if (searchBox._searchTextBox != null && searchBox._searchTextBox.Text != e.NewValue as string)
            {
                searchBox._searchTextBox.Text = e.NewValue as string;
            }
            
            searchBox.RaiseEvent(new RoutedEventArgs(TextChangedEvent));
        }

        public void Clear()
        {
            Text = string.Empty;
        }

        public void Focus()
        {
            _searchTextBox?.Focus();
        }
    }
}
