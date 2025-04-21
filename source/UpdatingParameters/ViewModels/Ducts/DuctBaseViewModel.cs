using System.Windows;
using Autodesk.Revit.UI;
using UpdatingParameters.Services;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.Parameters;

namespace UpdatingParameters.ViewModels.Ducts;

public class DuctBaseViewModel :ElementTypeViewModelBase
{
    private readonly DataStorageFactory _storageFactory;

    public DuctBaseViewModel(DataStorageFormulas dataStorageFormulas,DataStorageFactory storageFactory) : base(dataStorageFormulas)
    {
        _storageFactory = storageFactory;
    }
    protected override void UpdateElements(object window)
    {
        var view = window as Window;
        using Transaction tr = new(Context.ActiveDocument, "Обновление параметров");
        try
        {
            tr.Start();
            if (Elements == null || Elements.Count == 0)
            {
                tr.RollBack();
                TaskDialog.Show("Ошибка", "Нет труб для обновления.");
                UpdaterParametersService.ReturnWindowState(view);
                return;
            }

            var parametersDataStorage = _storageFactory.GetStorage<ParametersDataStorage>();
            if (parametersDataStorage.HermeticClassIsChecked)
            {
                UpdaterParametersService.UpdateParamHermeticСlass(Doc, Elements);
            }

            if (parametersDataStorage.WallThicknessIsChecked)
            {
                var ductParametersDataStorage = _storageFactory.GetStorage<DuctParametersDataStorage>();
                UpdaterParametersService.UpdateParamWallThickness(Doc, Elements,
                    ductParametersDataStorage.DuctParameters);
            }

            int current = 0;
            foreach (var element in Elements)
            {
                UpdaterParametersService.UpdateParameters(element, DataStorageFormulas);
                current++;
            }

            tr.Commit();
            TaskDialog.Show("Обновление элементов", $"Обновлено элементов: {current}");
            UpdaterParametersService.ReturnWindowState(view);
        }
        catch (Exception ex)
        {
            tr.RollBack();
            TaskDialog.Show("Ошибка", ex.Message);
            UpdaterParametersService.ReturnWindowState(view);
        }
    }
}