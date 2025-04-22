using System.Windows;
using Autodesk.Revit.UI;
using UpdatingParameters.Services;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.Ducts;
using UpdatingParameters.Storages.Parameters;

namespace UpdatingParameters.ViewModels.Ducts;

public class DuctRoundViewModel(DataStorageFormulas dataStorageFormulas, DataStorageFactory storageFactory)
    : DuctBaseViewModel(dataStorageFormulas, storageFactory);