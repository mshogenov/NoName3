using System.Windows;
using System.Windows.Media.Animation;

namespace NoNameApi.Views.Resources.Themes;

 public class ThemeManager
    {
        // Singleton для управления темой
        private static ThemeManager _instance;
        public static ThemeManager Instance => _instance ?? (_instance = new ThemeManager());

        // Текущая тема
        private bool _isDarkTheme = false;
        public bool IsDarkTheme => _isDarkTheme;

        // Событие изменения темы для уведомления подписчиков
        public event EventHandler<bool> ThemeChanged;

        private ThemeManager() { }

        // Ссылка на основное окно приложения
        private Window _mainWindow;
        private FrameworkElement _mainContent;

        // Инициализация
        public void Initialize(Window mainWindow, FrameworkElement mainContent)
        {
            _mainWindow = mainWindow;
            _mainContent = mainContent;

            // Загрузка сохраненной темы
            _isDarkTheme = GetSavedThemePreference();
            ApplyThemeWithoutAnimation(_isDarkTheme);
        }

        // Переключение темы с анимацией
        public void ToggleTheme()
        {
            _isDarkTheme = !_isDarkTheme;

            // Анимация смены темы
            AnimateThemeChange(() => 
            {
                ApplyThemeWithoutAnimation(_isDarkTheme);
                SaveThemePreference(_isDarkTheme);
                ThemeChanged?.Invoke(this, _isDarkTheme);
            });
        }

        // Анимированное переключение темы
        private void AnimateThemeChange(Action applyThemeAction)
        {
            if (_mainContent == null) 
            {
                applyThemeAction?.Invoke();
                return;
            }

            // Создаем анимацию исчезновения
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));

            fadeOut.Completed += (s, e) =>
            {
                // Применяем тему
                applyThemeAction?.Invoke();

                // Создаем анимацию появления
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                _mainContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };

            // Запускаем анимацию исчезновения
            _mainContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        // Применение темы без анимации
        private void ApplyThemeWithoutAnimation(bool isDarkTheme)
        {
            // Если нет окна, выходим
            if (_mainWindow == null) return;

            // Получаем корневой словарь ресурсов
            if (_mainWindow.Resources.MergedDictionaries.Count > 0)
            {
                var mainDict = _mainWindow.Resources.MergedDictionaries[0];

                if (mainDict.MergedDictionaries.Count > 0)
                {
                    // Удаляем текущий словарь темы
                    mainDict.MergedDictionaries.RemoveAt(0);

                    // Создаем новый словарь темы
                    var newThemeDict = new ResourceDictionary();
                    string themePath = isDarkTheme
                        ? "pack://application:,,,/NoNameAPI;component/Views/Resources/Themes/DarkTheme.xaml"
                        : "pack://application:,,,/NoNameAPI;component/Views/Resources/Themes/LightTheme.xaml";

                    newThemeDict.Source = new Uri(themePath);

                    // Добавляем новый словарь
                    mainDict.MergedDictionaries.Insert(0, newThemeDict);
                }
            }
        }

        // Загрузка сохраненных настроек
        private bool GetSavedThemePreference()
        {
            try
            {
                // Пример использования ключа реестра, вы можете изменить способ хранения
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\NoNameAPI"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("DarkTheme");
                        if (value != null && value is string strValue)
                        {
                            return bool.Parse(strValue);
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки чтения настроек
            }

            return false; // По умолчанию светлая тема
        }

        // Сохранение настроек
        private void SaveThemePreference(bool isDark)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("SOFTWARE\\NoNameAPI"))
                {
                    if (key != null)
                    {
                        key.SetValue("DarkTheme", isDark.ToString());
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки сохранения настроек
            }
        }
    }