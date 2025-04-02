using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Parameters;

public class DuctParametersDataStorage : IDataStorage
{
    public List<DuctParameters> DuctParameters = [];
    private readonly IDataLoader _dataLoader;

    public DuctParametersDataStorage(IDataLoader dataLoader)
    {
        _dataLoader = dataLoader;
        LoadData();
    }

    public void InitializeDefault()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "UpdatingParameters.Resources.DefaultDuctParameters.json";

        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new Exception($"Ресурс {resourceName} не найден.");
        using StreamReader reader = new StreamReader(stream);
        string json = reader.ReadToEnd();
        DuctParameters = JsonConvert.DeserializeObject<List<DuctParameters>>(json);
        Save();
    }

    public void UpdateData()
    {
       LoadData();
    }

    private void LoadData()
    {
        var loaded = _dataLoader.LoadData<List<DuctParameters>>();
        if (loaded == null)
        {
            InitializeDefault();
        }
        else
        {
            DuctParameters = loaded;
        }
    }

    public void Save()
    {
        _dataLoader.SaveData(DuctParameters);
    }

    public void Add(DuctParameters newParameter)
    {
        DuctParameters.Add(newParameter);
    }

    public void Delete(int selectedParameterId)
    {
        var firstOrDefault = DuctParameters.FirstOrDefault(x => x.Id == selectedParameterId);
        DuctParameters.Remove(firstOrDefault);
    }

    public void Update(DuctParameters selectedParameter)
    {
        var existingParameter = DuctParameters.FirstOrDefault(x => x.Id == selectedParameter.Id);

        if (existingParameter != null)
        {
            existingParameter.Material = selectedParameter.Material;
            existingParameter.Shape = selectedParameter.Shape;
            existingParameter.ExternalInsulation = selectedParameter.ExternalInsulation;
            existingParameter.InternalInsulation = selectedParameter.InternalInsulation;
            existingParameter.Size = selectedParameter.Size;
            existingParameter.Thickness = selectedParameter.Thickness;
        }
        else
        {
            // Если объект не найден, можно добавить его в коллекцию
            DuctParameters.Add(selectedParameter);
        }
    }
}