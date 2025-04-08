using System.Windows;
using System.Windows.Media;

namespace NoNameApi.Views.Services;

/// <summary>
/// Менеджер тем для приложений Revit, позволяющий динамически переключать 
/// темы и получать доступ к ресурсам темы для конвертеров и других компонентов
/// </summary>
public static  class RevitThemeManager
{
    private static Dictionary<string, ResourceDictionary> _themeCache = new Dictionary<string, ResourceDictionary>();

    // Ключи цветовых ресурсов, используемые в конвертерах
    public static readonly string DefaultButtonBackgroundKey = "DefaultButtonBrush";
    public static readonly string SelectedButtonBackgroundKey = "IsSelectedBrush";
    /// <summary>
    /// Текущая тема
    /// </summary>
    public static string CurrentThemeName { get; private set; } = "Light";
   
    /// <summary>
    /// Получает ресурс из текущей темы по ключу
    /// </summary>
    public static object GetResource(string resourceKey)
    {
        ResourceDictionary theme = GetCurrentThemeDictionary();
        return theme.Contains(resourceKey) ? theme[resourceKey] : null;
    }
    
    /// <summary>
    /// Получает ресурс типа SolidColorBrush из текущей темы
    /// </summary>
    public static SolidColorBrush GetBrush(string resourceKey, SolidColorBrush defaultBrush = null)
    {
        var resource = GetResource(resourceKey);
        return resource as SolidColorBrush ?? defaultBrush ?? Brushes.White;
    }
    /// <summary>
    /// Получает словарь ресурсов текущей темы
    /// </summary>
    public static ResourceDictionary GetCurrentThemeDictionary()
    {
        string themePath = GetThemePath(CurrentThemeName);

        if (!_themeCache.ContainsKey(themePath))
        {
            LoadTheme(themePath);
        }

        return _themeCache[themePath];
    }
    
    /// <summary>
    /// Обновляет текущую тему на основе настроек Revit
    /// </summary>
    public static void UpdateCurrentTheme(bool forceReload = false)
    {
        string themeName = BaseRevitWindow.IsDarkTheme() ? "Dark" : "Light";

        if (themeName != CurrentThemeName || forceReload)
        {
            CurrentThemeName = themeName;
            string themePath = GetThemePath(themeName);

            // Перезагружаем тему если нужно
            if (forceReload && _themeCache.ContainsKey(themePath))
            {
                _themeCache.Remove(themePath);
            }

            // Убеждаемся, что тема загружена
            if (!_themeCache.ContainsKey(themePath))
            {
                LoadTheme(themePath);
            }
        }
    }
    /// <summary>
    /// Получает путь к ресурсам темы
    /// </summary>
    private static string GetThemePath(string themeName)
    {
        return $"pack://application:,,,/NoNameAPI;component/Views/Resources/Themes/{themeName}Theme.xaml";
    }
    /// <summary>
    /// Загружает тему в кэш
    /// </summary>
    private static void LoadTheme(string themePath)
    {
        try
        {
            ResourceDictionary themeDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Absolute)
            };
            _themeCache[themePath] = themeDictionary;
        }
        catch (Exception ex)
        {
            // Создаем запасной словарь с базовыми ресурсами
            ResourceDictionary fallbackDictionary = new ResourceDictionary();

            // Добавляем базовые ресурсы для светлой темы
            fallbackDictionary[DefaultButtonBackgroundKey] = Brushes.Transparent;
            fallbackDictionary[SelectedButtonBackgroundKey] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d3d3d3"));

            _themeCache[themePath] = fallbackDictionary;
        }
    }
   
}