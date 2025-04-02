using Newtonsoft.Json;
using System.IO;
using System.Windows;


namespace UpdatingParameters.Storages;

public class JsonDataLoader : IDataLoader
{
    private readonly string _fileFullPath;

    public JsonDataLoader(string fileName)
    {
        // Задаем путь к директории AppData\Roaming\NoNameData
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directoryPath = Path.Combine(appDataPath, "NoNameData/UpdatingParameters");
        Directory.CreateDirectory(directoryPath);
        // Формируем полный путь к файлу
        _fileFullPath = Path.Combine(directoryPath, fileName);
    }

    public T LoadData<T>() where T : class
    {
        if (!File.Exists(_fileFullPath))
            return null;
        try
        {
            var json = File.ReadAllText(_fileFullPath);
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
        File.WriteAllText(_fileFullPath, json);
    }
}