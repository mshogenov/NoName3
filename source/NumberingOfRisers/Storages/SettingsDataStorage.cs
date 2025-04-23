using NoNameApi.Services;
using NumberingOfRisers.Models;
using NumberingOfRisers.Services;

namespace NumberingOfRisers.Storages;

public class SettingsDataStorage
{
    public double MinimumLengthRiser { get; set; }

    private readonly JsonDataLoader _dataLoader;

    public SettingsDataStorage()
    {
        _dataLoader = new JsonDataLoader("SettingsDataStorage");
        Load();
    }

    public void Save()
    {
        var dto = new SettingsDTO
        {
            MinimumLengthRiser = MinimumLengthRiser,
        };
        _dataLoader.SaveData(dto);
    }

    public void InitializeDefault()
    {
        MinimumLengthRiser = 2500;
    }

    public void Load()
    {
        var loaded = _dataLoader.LoadData<SettingsDTO>();
        if (loaded == null)
        {
            InitializeDefault();
        }
        else
        {
            MinimumLengthRiser = loaded.MinimumLengthRiser;
        }
    }
}