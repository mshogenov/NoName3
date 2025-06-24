using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Nice3point.Revit.Extensions;
using Nice3point.Revit.Toolkit;
using NoNameApi.Utils;
using ViewOfPipeSystems.Model;
using View = Autodesk.Revit.DB.View;

namespace ViewOfPipeSystems.Services;

public class ViewOfPipeSystemsServices
{
    private readonly View _activeView;
    private readonly Document _doc;
    private const string ParamAdsk_Система_Имя = "ADSK_Система_Имя";
    private const string GetParamName = "Имя системы";
    private readonly IList<Element> _elements;

    private readonly List<BuiltInCategory> _mepCategories =
    [
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_FlexPipeCurves,
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_PipeInsulations,
        BuiltInCategory.OST_Sprinklers,
        BuiltInCategory.OST_PlumbingEquipment,
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_FlexDuctCurves,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_DuctTerminal,
        BuiltInCategory.OST_DuctInsulations,
        BuiltInCategory.OST_DuctLinings
    ];

    public ViewOfPipeSystemsServices()
    {
        _doc = Context.ActiveDocument;
        var categoryFilter = new ElementMulticategoryFilter(_mepCategories);
        _activeView = Context.ActiveView;
        _elements = new FilteredElementCollector(_doc).WherePasses(categoryFilter).WhereElementIsNotElementType()
            .ToElements();
    }

    public void ProcessMepSystems(List<MEPSystemModel> mechanicalSystems, Dictionary<string, ElementId> existingViews,
        Dictionary<string, ParameterFilterElement> existingFilters)
    {
        foreach (var element in _elements)
        {
            CopyParameterMep(Context.ActiveDocument, element, GetParamName, ParamAdsk_Система_Имя);
        }
        SubTransaction subTransaction = new SubTransaction(_doc);
        Helpers.BindParameter(_doc, ParamAdsk_Система_Имя, _mepCategories, subTransaction);
        foreach (var mepSystem in mechanicalSystems)
        {
            string viewName = $"Схема системы {mepSystem.Name}";
        
            // Получение или создание вида
            if (!existingViews.TryGetValue(viewName, out _))
            {
                CreateAndSetupNewView(mepSystem.MEPSystem, viewName, existingViews, existingFilters);
            }
        }
    }

    private void CreateAndSetupNewView(MEPSystem mepSystem, string viewName,
        Dictionary<string, ElementId> existingViews,
        Dictionary<string, ParameterFilterElement> existingFilters)
    {
        ElementId newViewId = _activeView.Duplicate(ViewDuplicateOption.Duplicate);
        if (_doc.GetElement(newViewId) is not View newView) return;
        // Настройка нового вида
        SetupView(newView, viewName);
        // Добавление вида в словарь
        existingViews.Add(viewName, newViewId);

        // Создание и применение фильтра
        ApplySystemFilter(mepSystem, newView, existingFilters);
    }

    private void SetupView(View view, string viewName)
    {
        // Удаление существующих фильтров
        RemoveExistingFilters(view);

        // Настройка вида
        view.ViewTemplateId = ElementId.InvalidElementId;
        view.Name = viewName;

        // Копирование настроек из активного вида
        CopyViewSettings(_activeView, view);
    }

    private void RemoveExistingFilters(View view)
    {
        ICollection<ElementId> viewFilters = view.GetFilters();
        foreach (ElementId filterId in viewFilters)
        {
            view.RemoveFilter(filterId);
        }
    }

    private void ApplySystemFilter(MEPSystem mepSystem, View view,
        Dictionary<string, ParameterFilterElement> existingFilters)
    {
        string filterName = $"Система {mepSystem.Name}";

        // Получение или создание фильтра
        ParameterFilterElement parameterFilter = GetOrCreateFilter(mepSystem, filterName, existingFilters);

        if (parameterFilter == null) return;

        // Применение фильтра к виду
        ApplyFilterToView(view, parameterFilter, filterName);
    }

    private ParameterFilterElement GetOrCreateFilter(MEPSystem mepSystem, string filterName,
        Dictionary<string, ParameterFilterElement> existingFilters)
    {
        if (existingFilters.TryGetValue(filterName, out ParameterFilterElement parameterFilter))
        {
            return parameterFilter;
        }

        // Создание нового фильтра
        // Проверка и фильтрация категорий перед созданием
        var categoryIds = _mepCategories
            .Where(x => x != 0) // Отфильтруем нулевые или недопустимые категории
            .Select(x => new ElementId(x))
            .ToList(); // Важно использовать ToList() вместо as ICollection<ElementId>

// Проверка, что список не пустой
        if (categoryIds.Count == 0)
        {
            TaskDialog.Show("Ошибка", "Список категорий МЕП пуст или содержит недопустимые значения.");
        }

// Создание фильтра с проверенным списком категорий
        parameterFilter = ParameterFilterElement.Create(_doc, filterName, categoryIds);

        // Настройка правила фильтра
        SetupFilterRule(parameterFilter, mepSystem.Name);

        // Добавление фильтра в словарь
        existingFilters.Add(filterName, parameterFilter);

        return parameterFilter;
    }

    private void SetupFilterRule(ParameterFilterElement filter, string systemName)
    {
        try
        {
            // Сначала проверяем, что фильтр имеет категории
            if (!filter.GetCategories().Any())
            {
                TaskDialog.Show("Предупреждение", "Фильтр не имеет выбранных категорий.");
                return;
            }

            ParameterElement paramElement = GetParameterElement(ParamAdsk_Система_Имя);
            ElementId parameterId = paramElement?.Id;
            FilterRule rule = ParameterFilterRuleFactory.CreateNotContainsRule(parameterId, systemName);

            // Создаем и применяем фильтр
            ElementParameterFilter elementFilter = new ElementParameterFilter(rule);
            filter.SetElementFilter(elementFilter);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", $"Не удалось настроить правило фильтра: {ex.Message}");
        }
    }

    private ParameterElement GetParameterElement(string parameterName)
    {
        return new FilteredElementCollector(_doc)
            .OfClass(typeof(ParameterElement))
            .Cast<ParameterElement>()
            .FirstOrDefault(pe => pe.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyFilterToView(View view, ParameterFilterElement filter, string filterName)
    {
        if (view.GetFilters().Contains(filter.Id)) return;

        try
        {
            view.AddFilter(filter.Id);
            view.SetFilterVisibility(filter.Id, false);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка",
                $"Не удалось применить фильтр '{filterName}' к виду '{view.Name}'.\n{ex.Message}");
        }
    }

    public void UpdateViews()
    {
        Transaction tr = new(_doc, "Обновить виды");
        tr.Start();
        try
        {
            foreach (var element in _elements)
            {
                CopyParameterMep(Context.ActiveDocument, element, GetParamName, ParamAdsk_Система_Имя);
            }
        }
        catch (Exception ex)
        {
            TaskDialog.Show("Ошибка", ex.Message);
            tr.RollBack();
        }

        tr.Commit();
        TaskDialog.Show("Информация", "Виды обновлены");
    }

    private void CopyParameterMep(Document doc, Element element, string getParamName, string setParamName)
    {
        try
        {
            Parameter getParam = element.FindParameter(getParamName);
            Parameter setParam = element.FindParameter(setParamName);
            if (element.Category.BuiltInCategory == BuiltInCategory.OST_MechanicalEquipment)
            {
                if (getParamName == "Сокращение для системы")
                {
                    var connectors = (element as FamilyInstance)?.MEPModel?.ConnectorManager?.Connectors;
                    string parameterValue = string.Empty;
                    setParam = element.FindParameter(setParamName);
                    if (connectors != null)
                    {
                        foreach (Connector connector in connectors)
                        {
                            if (connector.MEPSystem == null) continue;
                            ElementId systemTypeId = connector.MEPSystem.GetTypeId();
                            Element systemType = doc.GetElement(systemTypeId);
                            Parameter getParamAbbreviation =
                                systemType?.FindParameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
                            if (getParamAbbreviation != null &&
                                !parameterValue.Contains(getParamAbbreviation.AsValueString()))
                            {
                                parameterValue += getParamAbbreviation.AsValueString() + ", ";
                            }
                        }

                        string sortedParameter = SortParameter(parameterValue);
                        if (!string.IsNullOrEmpty(sortedParameter))
                        {
                            // Устанавливаем значение для родительского элемента
                            SetParameter(sortedParameter, setParam);
                            // Рекурсивно обрабатываем все вложенные семейства
                            ProcessNestedElements(doc, element, sortedParameter, setParamName);
                            return;
                        }
                    }
                }
            }

            if (getParam == null || setParam == null) return;
            {
                string sortedParameter = SortParameter(getParam.AsValueString());
                if (string.IsNullOrEmpty(sortedParameter)) return;
                // Устанавливаем значение для родительского элемента
                SetParameter(sortedParameter, setParam);
                // Рекурсивно обрабатываем все вложенные семейства
                ProcessNestedElements(doc, element, sortedParameter, setParamName);
            }
        }
        catch (Exception ex)
        {
            var elementType = doc.GetElement(element.GetTypeId()) as ElementType;
            var familyName = elementType?.FamilyName;
            TaskDialog.Show("Ошибка",
                $"{ex.Message}\nСемейство: {familyName}\nЭлемент: {element.Name}\nID: {element.Id}");
        }
    }

    private void SetParameter(string sortedParameter, Parameter setParam)
    {
        if (!setParam.IsReadOnly)
        {
            setParam.Set(sortedParameter);
        }
    }

    // Метод для сортировки аббревиатуры
    private string SortParameter(string paramValue)
    {
        if (string.IsNullOrWhiteSpace(paramValue))
            return string.Empty;
        return string.Join(", ", paramValue
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x)) // Убираем пустые строки после Trim
            .OrderBy(x => x));
    }

    // Рекурсивный метод для обработки всех вложенных семейств
    private void ProcessNestedElements(Document doc, Element parentElement, string sortedParameter,
        string setParamName)
    {
        List<FamilyInstance> nestedFamilyInstances = GetNestedFamilyInstances(parentElement);
        foreach (var instance in nestedFamilyInstances)
        {
            try
            {
                Parameter setParam = instance.FindParameter(setParamName);
                // Устанавливаем значение параметра для текущего вложенного элемента
                SetParameter(sortedParameter, setParam);
                // Рекурсивно обрабатываем вложенные экземпляры этого элемента
                ProcessNestedElements(doc, instance, sortedParameter, setParamName);
            }
            catch (Exception ex)
            {
                var elementType = doc.GetElement(instance.GetTypeId()) as ElementType;
                var familyName = elementType?.FamilyName;
                TaskDialog.Show("Ошибка",
                    $"{ex.Message}\nСемейство: {familyName}\nЭлемент: {instance.Name}\nID: {instance.Id}");
            }
        }
    }

    private List<FamilyInstance> GetNestedFamilyInstances(Element element)
    {
        List<FamilyInstance> nestedFamilyInstances = [];
        if (element is not FamilyInstance familyInstance) return nestedFamilyInstances;
        foreach (ElementId subElementId in familyInstance.GetSubComponentIds())
        {
            Element nestedElement = element.Document.GetElement(subElementId) as FamilyInstance;
            if (nestedElement != null)
            {
                nestedFamilyInstances.Add((FamilyInstance)nestedElement);
            }
        }

        return nestedFamilyInstances;
    }

    /// <summary>
    /// Копирует графические настройки из одного вида в другой.
    /// </summary>
    /// <param name="sourceView">Исходный вид, из которого копируются настройки.</param>
    /// <param name="targetView">Целевой вид, в который копируются настройки.</param>
    private void CopyViewSettings(View sourceView, View targetView)
    {
        targetView.DetailLevel = ViewDetailLevel.Fine;
        // Копируем видимость категорий
        foreach (Category category in sourceView.Document.Settings.Categories)
        {
            if (category == null || !targetView.CanCategoryBeHidden(category.Id)) continue;
            bool isVisible = !sourceView.GetCategoryHidden(category.Id);
            targetView.SetCategoryHidden(category.Id, !isVisible);
        }
    }
}