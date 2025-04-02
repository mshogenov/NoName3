using Newtonsoft.Json;
using System.IO;
using System.Windows;


namespace NumberingOfRisers.Services
{
    public class JsonDataLoader
    {
        private readonly string fileFullPath;

        public JsonDataLoader(string fileName)
        {
            // Задаем путь к директории AppData\Roaming\NoNameData
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string directoryPath = Path.Combine(appDataPath, "NoNameData/NumberingOfRisers");
            Directory.CreateDirectory(directoryPath);
            // Формируем полный путь к файлу
            fileFullPath = Path.Combine(directoryPath, fileName);
        }

        public T LoadData<T>() where T : class
        {
            if (!File.Exists(fileFullPath))
                return null;
            try
            {
                var json = File.ReadAllText(fileFullPath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public void SaveData<T>(T data) where T : class
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(fileFullPath, json);
        }


    }
}

