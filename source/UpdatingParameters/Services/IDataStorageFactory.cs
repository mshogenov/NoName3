using UpdatingParameters.Models;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.Parameters;
using UpdatingParameters.Storages.Settings;

namespace UpdatingParameters.Services;

public interface IDataStorageFactory
{
    IDataStorage CreateDataStorage(DataStorageType dataStorageType);
    ISettingStorage CreateSettingStorage();
    IParameterDataStorage CreateParameterDataStorage();
}