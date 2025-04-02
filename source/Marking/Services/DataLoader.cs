using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marking.Services
{
    public class DataLoader
    {
        private readonly string fileFullPath;

        public DataLoader(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }
            if (!File.Exists(fileFullPath))
            {
                // Задаем путь к директории AppData\Roaming\NoNameData
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string directoryPath = Path.Combine(appDataPath, "NoNameData");

                // Формируем полный путь к файлу
                fileFullPath = Path.Combine(directoryPath, fileName);
            }

        }
        public List<T> LoadData<T>(List<T> collection, string keyProperty, params string[] updateProperties)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (keyProperty == null)
                throw new ArgumentNullException(nameof(keyProperty));
            if (updateProperties == null || updateProperties.Length == 0)
                throw new ArgumentNullException(nameof(updateProperties));

            try
            {
                if (File.Exists(fileFullPath))
                {
                    string json = File.ReadAllText(fileFullPath);
                    var loadedCollection = JsonConvert.DeserializeObject<List<T>>(json);

                    if (loadedCollection != null)
                    {
                        foreach (var item in collection)
                        {
                            var itemKeyValue = typeof(T).GetProperty(keyProperty)?.GetValue(item);
                            var loadedItem = loadedCollection.Find(x =>
                                typeof(T).GetProperty(keyProperty)?.GetValue(x)?.Equals(itemKeyValue) == true
                            );

                            if (loadedItem != null)
                            {
                                foreach (var updateProperty in updateProperties)
                                {
                                    var propertyInfo = typeof(T).GetProperty(updateProperty);
                                    if (propertyInfo != null)
                                    {
                                        var loadedValue = propertyInfo.GetValue(loadedItem);
                                        propertyInfo.SetValue(item, loadedValue);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируйте или обрабатывайте исключение по необходимости
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            return collection;
        }
        public T LoadData<T>() where T : new()
        {
            if (File.Exists(fileFullPath))
            {
                string json = File.ReadAllText(fileFullPath);
                var settings = JsonConvert.DeserializeObject<T>(json);
                return settings.Equals(default(T)) ? new T() : settings;
            }
            return new T();
        }
        public void SaveData<T>(T data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            try
            {
                var directoryName = Path.GetDirectoryName(fileFullPath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(fileFullPath, json);
            }
            catch (Exception ex)
            {
                LogError(ex); // Вызов метода логирования ошибок
            }
        }
        private void LogError(Exception ex)
        {
            // Использование TaskDialog из Revit API для отображения ошибки
            TaskDialog.Show("Error", $"An unexpected error occurred: {ex.Message}");
            // Здесь можно добавить более сложное логирование, например, в файл или систему мониторинга
        }

    }
}
