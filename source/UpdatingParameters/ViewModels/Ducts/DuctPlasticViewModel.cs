using UpdatingParameters.Services;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.Ducts;

namespace UpdatingParameters.ViewModels.Ducts;

public class DuctPlasticViewModel(DataStorageFormulas dataStorageFormulas, DataStorageFactory storageFactory)
    : DuctBaseViewModel(dataStorageFormulas, storageFactory);