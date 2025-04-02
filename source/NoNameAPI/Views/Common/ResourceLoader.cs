using System.Windows;

namespace NoNameApi.Views.Common;

public static class ResourceLoader
{
    private static bool _resourcesLoaded = false;
    private static readonly object _lock = new object();

    public static void LoadResources()
    {
        // Проверка, загружены ли уже ресурсы
        lock (_lock)
        {
            if (_resourcesLoaded)
                return;

            // Загрузка ресурсного словаря
            ResourceDictionary resources = new ResourceDictionary
            {
                
                Source = new Uri("pack://application:,,,/NoNameAPI;component/Views/Common/Themes/Generic.xaml", UriKind.Absolute)
            };

            // Добавление в словарь приложения
            Application.Current.Resources.MergedDictionaries.Add(resources);

            _resourcesLoaded = true;
        }
    }
}