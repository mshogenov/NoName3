namespace UpdatingParameters.Storages.Parameters;

public interface IParameterDataStorage
{
    void InitializeDefault();
    void Load();
    void Save();
}