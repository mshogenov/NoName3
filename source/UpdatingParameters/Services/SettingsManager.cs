using UpdatingParameters.Storages;

namespace UpdatingParameters.Services;

public static class SettingsManager
{
    public static event Action OnSettingsChanged;

    public static void ResetSettings()
    {
        var dataStorages = DataStorageFactory.Instance.GetAllStorages();
        foreach (var dataStorage in dataStorages)
        {
            dataStorage.InitializeDefault();
        }
        OnSettingsChanged?.Invoke();
    }
}