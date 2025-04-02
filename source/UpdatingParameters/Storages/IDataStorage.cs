namespace UpdatingParameters.Storages;

public interface IDataStorage
{
    void InitializeDefault();
    void UpdateData();
    void Save();
}