using System.Windows;
using Autodesk.Revit.UI;
using UpdatingParameters.Services;
using UpdatingParameters.Storages;
using UpdatingParameters.Storages.Parameters;
using UpdatingParameters.Views.Parameters;

namespace UpdatingParameters.ViewModels.Parameters
{
    public partial class ParametersViewModel : ViewModelBase
    {
        private readonly Document _doc;
        private readonly Autodesk.Revit.ApplicationServices.Application _app;
        private readonly List<Element> _elements;
        private bool _wallThicknessIsChecked;

        public bool WallThicknessIsChecked
        {
            get => _wallThicknessIsChecked;
            set
            {
                if (value == _wallThicknessIsChecked) return;
                _wallThicknessIsChecked = value;
                OnPropertyChanged();
                _parametersDataStorage.WallThicknessIsChecked = value;
            }
        }

        private bool _hermeticClassIsChecked;

        public bool HermeticClassIsChecked
        {
            get => _hermeticClassIsChecked;
            set
            {
                if (value == _hermeticClassIsChecked) return;
                _hermeticClassIsChecked = value;
                OnPropertyChanged();
                _parametersDataStorage.HermeticClassIsChecked = value;
            }
        }

        private bool _systemAbbreviationIsChecked;

        public bool SystemAbbreviationIsChecked
        {
            get => _systemAbbreviationIsChecked;
            set
            {
                _systemAbbreviationIsChecked = value;
                OnPropertyChanged();
                _parametersDataStorage.SystemAbbreviationIsChecked = value;
            }
        }

        private bool _systemNameIsChecked;

        public bool SystemNameIsChecked
        {
            get => _systemNameIsChecked;
            set
            {
                _systemNameIsChecked = value;
                OnPropertyChanged();
                _parametersDataStorage.SystemNameIsChecked = value;
            }
        }

        private string[] _parameterNames =
        [
            "ADSK_Система_Сокращение",
            "ADSK_Система_Имя",
            //"ADSK_Система_Тип",
            //"ADSK_Система_Классификация",
        ];

       

        private readonly ParametersDataStorage _parametersDataStorage;
        private readonly DuctParametersDataStorage _ductParametersDataStorage;

        public ParametersViewModel()
        {
            _doc = Context.ActiveDocument;
            _app = Context.Application;
            _parametersDataStorage = DataStorageFactory.Instance.GetStorage<ParametersDataStorage>();
            _elements = _parametersDataStorage.GetElements();
            SystemAbbreviationIsChecked = _parametersDataStorage.SystemAbbreviationIsChecked;
            SystemNameIsChecked = _parametersDataStorage.SystemNameIsChecked;
            WallThicknessIsChecked = _parametersDataStorage.WallThicknessIsChecked;
            HermeticClassIsChecked = _parametersDataStorage.HermeticClassIsChecked;
            _ductParametersDataStorage = DataStorageFactory.Instance.GetStorage<DuctParametersDataStorage>();
        }

        [RelayCommand]
        private void Save()
        {
            _parametersDataStorage?.Save();
            MessageBox.Show("Настройки сохранены");
        }

        [RelayCommand]
        private void UpdateParameters(object window)
        {
            var view = window as Window;
            Transaction tr = new Transaction(_doc, "Обновление параметров");
            try
            {
                tr.Start();
                var nestedFamilies = _elements.OfType<FamilyInstance>()
                    .SelectMany(fi => fi.GetSubComponentIds()).Where(subId => subId != null).ToList();
                //Отделение от всех семейств вложенных семейств
                var elementsIdNotNestedFamilies = _elements.Select(x => x.Id).Except(nestedFamilies)
                    .Select(x => x.ToElement(Context.ActiveDocument)).ToList();
                if (SystemAbbreviationIsChecked)
                {
                    UpdaterParametersService.UpdateParamSystemAbbreviation(_doc, elementsIdNotNestedFamilies);
                }

                if (SystemNameIsChecked)
                {
                    UpdaterParametersService.UpdateParamSystemName(_doc, elementsIdNotNestedFamilies);
                }

                if (HermeticClassIsChecked)
                {
                    UpdaterParametersService.UpdateParamHermeticСlass(_doc, _elements);
                }

                if (WallThicknessIsChecked)
                {
                    UpdaterParametersService.UpdateParamWallThickness(_doc, _elements, _ductParametersDataStorage.DuctParameters);
                }
                tr.Commit();
                TaskDialog.Show("Информация", "Параметры обновлены");
                // Возвращаем активность окну
                ReturnWindowState(view);
            }
            catch (Exception e)
            {
                tr.RollBack();
                TaskDialog.Show("Ошибка", e.Message);
                ReturnWindowState(view);
            }
        }

        private static void ReturnWindowState(Window window)
        {
            if (window == null || window.WindowState == WindowState.Minimized) return;
            window.Activate(); // Делаем окно активным
            window.Topmost = true; // Устанавливаем окно поверх всех остальных
            window.Topmost = false; // Сбрасываем флаг, чтобы окно могло быть отправлено назад при необходимости
        }

        [RelayCommand]
        private void AdjustWallThickness()
        {
            var viewModel = new DuctThicknessViewModel(_ductParametersDataStorage);
            var view = new DuctThicknessWindow(viewModel);
            view.ShowDialog();
        }
    }
}