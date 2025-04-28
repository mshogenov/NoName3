using UpdatingParameters.Services;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.Ducts;

namespace UpdatingParameters.ViewModels.Ducts;

public class FlexibleDuctsRoundViewModel(DataStorageFormulas dataStorageFormulas, DataStorageFactory storageFactory)
    : DuctBaseViewModel(dataStorageFormulas, storageFactory);