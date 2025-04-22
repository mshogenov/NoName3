using UpdatingParameters.Storages;

namespace UpdatingParameters.Services;

public  class SettingsManager
{
    private static DataStorageFactory _storageFactory;
    public static event Action OnSettingsChanged;

    public SettingsManager(DataStorageFactory dataStorage)
    {
        _storageFactory = dataStorage;
    }
   
    public static void ResetSettings()
    {
       var dataStorages = _storageFactory.GetAllStorages();
        foreach (var dataStorage in dataStorages)
        {
            dataStorage.InitializeDefault();
        }
        OnSettingsChanged?.Invoke();
    }
}