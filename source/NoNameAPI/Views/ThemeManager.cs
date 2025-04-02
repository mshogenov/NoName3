using NoNameApi.Models;
using System;
using System.Windows;

namespace NoNameApi.Views;

public static class ThemeManager
    {
        private static ThemeType _currentTheme = ThemeType.Light;

        // Событие для уведомления о смене темы
        public static event EventHandler<ThemeType> ThemeChanged;

        // Текущая тема
        public static ThemeType CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(null, _currentTheme);
                }
            }
        }

        // Инициализация темы при запуске
        public static void Initialize()
        {
            // Можно загрузить сохраненную тему из настроек
            // Например: 
            // string savedTheme = Settings.Default.Theme;
            // if (savedTheme == "Dark")
            //    CurrentTheme = ThemeType.Dark;

            ApplyTheme(CurrentTheme);
        }

        // Переключение темы
        public static void ToggleTheme()
        {
            ThemeType newTheme = CurrentTheme == ThemeType.Light ? 
                ThemeType.Dark : ThemeType.Light;

            ApplyTheme(newTheme);
        }

        // Установка конкретной темы
        public static void ApplyTheme(ThemeType theme)
        {
            // Обновляем текущую тему
            CurrentTheme = theme;

            // Сохраняем выбор темы (опционально)
            // Settings.Default.Theme = theme.ToString();
            // Settings.Default.Save();

            // Очищаем текущие словари ресурсов
            Application.Current.Resources.MergedDictionaries.Clear();

            // Загружаем общие определения
            ResourceDictionary baseDict = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/RevitUI.Common;component/Resources/Themes/ThemeDefinitions.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Add(baseDict);

            // Загружаем тему
            ResourceDictionary themeDict = new ResourceDictionary
            {
                Source = new Uri(theme == ThemeType.Light
                    ? "pack://application:,,,/RevitUI.Common;component/Resources/Themes/LightTheme.xaml"
                    : "pack://application:,,,/RevitUI.Common;component/Resources/Themes/DarkTheme.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Add(themeDict);

            // Загружаем стили окон
            ResourceDictionary windowStyles = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/RevitUI.Common;component/Resources/WindowStyles.xaml")
            };
            Application.Current.Resources.MergedDictionaries.Add(windowStyles);
        }
    }