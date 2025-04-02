using Autodesk.Revit.UI;
using Newtonsoft.Json;
using NumberingOfRisers.Models;
using System.IO;


namespace NumberingOfRisers.Services
{
    public class DataLoader
    {
        private readonly string _fileFullPath;

        public DataLoader(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }

            // Задаем путь к директории AppData\Roaming\NoNameData
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directoryPath = Path.Combine(appDataPath, "NoNameData/NumberingOfRisers");

            // Формируем полный путь к файлу
            _fileFullPath = Path.Combine(directoryPath, fileName);
        }

        public List<RiserSystemType> LoadData(List<RiserSystemType> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            try
            {
                if (File.Exists(_fileFullPath))
                {
                    string json = File.ReadAllText(_fileFullPath);
                    var loadedCollection = JsonConvert.DeserializeObject<List<RiserSystemType>>(json);

                    foreach (var collect in collection)
                    {
                        var loadedItem = loadedCollection?.Find(x => x.MepSystemTypeName == collect.MepSystemTypeName);
                        // if (loadedItem != null)
                        // {
                        //     collect.IsChecked = loadedItem.IsChecked;
                        //     collect.InitialValue = loadedItem.InitialValue;
                        // }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex); // Вызов метода логирования ошибок
            }

            return collection;
        }

        public void SaveData<T>(List<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            try
            {
                var directoryName = Path.GetDirectoryName(_fileFullPath);
                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                string json = JsonConvert.SerializeObject(collection, Formatting.Indented);
                File.WriteAllText(_fileFullPath, json);
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