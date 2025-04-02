namespace UpdatingParameters.Storages.Settings;

public interface ISettingStorage
{
    void Save();
    void Load();
    void InitializeDefault();
}