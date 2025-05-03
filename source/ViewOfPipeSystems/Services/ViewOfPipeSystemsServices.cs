
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using ViewOfPipeSystems.Model;
using Binding = Autodesk.Revit.DB.Binding;
using View = Autodesk.Revit.DB.View;

namespace ViewOfPipeSystems.Services;

public class ViewOfPipeSystemsServices
{
    private readonly View _activeView;
    private readonly Document _doc;
    private const string SetParamName = "ADSK_Система_Имя";
    private const string GetParamName = "Имя системы";
    private readonly IList<Element> _elements;

    private readonly List<ElementId> _mepCategories =
    [
        new(BuiltInCategory.OST_PipeCurves),
        new(BuiltInCategory.OST_PlumbingFixtures),
        new(BuiltInCategory.OST_FlexPipeCurves),
        new(BuiltInCategory.OST_MechanicalEquipment),
        new(BuiltInCategory.OST_PipeAccessory),
        new(BuiltInCategory.OST_PipeFitting),
        new(BuiltInCategory.OST_PipeInsulations),
        new(BuiltInCategory.OST_Sprinklers),
        new(BuiltInCategory.OST_PlumbingEquipment),
        new(BuiltInCategory.OST_DuctCurves),
        new(BuiltInCategory.OST_DuctFitting),
        new(BuiltInCategory.OST_FlexDuctCurves),
        new(BuiltInCategory.OST_DuctAccessory),
        new(BuiltInCategory.OST_DuctTerminal),
        new(BuiltInCategory.OST_DuctInsulations),
        new(BuiltInCategory.OST_DuctLinings),
    ];

    public ViewOfPipeSystemsServices()
    {
        _doc = Context.ActiveDocument;
        var categoryFilter = new ElementMulticategoryFilter(_mepCategories);
        _activeView = Context.ActiveView;
        _elements = new FilteredElementCollector(_doc).WherePasses(categoryFilter).WhereElementIsNotElementType()
            .ToElements();
    }

    public void ProcessMepSystems(List<MEPSystemModel> mechanicalSystems,Dictionary<string, ElementId> existingViews,
        Dictionary<string, ParameterFilterElement> existingFilters)
    {
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
        parameterFilter = ParameterFilterElement.Create(_doc, filterName, _mepCategories);

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
            ParameterElement paramElement = GetParameterElement(SetParamName);
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

    /// <summary>
    /// Проверяет и подключает параметр к категориям
    /// </summary>
    private void CheckAndBindParameter(Document doc, string parameterName, SubTransaction sT)
    {
        try
        {
            sT.Start();
            // Получаем непривязанные категории
            var unboundCategories = GetUnboundCategories(doc, parameterName);

            if (!unboundCategories.Any())
            {
                sT.RollBack();
                return;
            }

            // Создаем набор категорий для привязки
            CategorySet categorySet = new();
            foreach (var categoryId in unboundCategories)
            {
                Category category = Category.GetCategory(doc, categoryId);
                if (category is { AllowsBoundParameters: true })
                {
                    categorySet.Insert(category);
                }

                if (category?.SubCategories?.IsEmpty != false) continue;
                foreach (var value in category.SubCategories)
                {
                    if (value is not Category subCategory) continue;
                    if (subCategory.AllowsBoundParameters)
                        categorySet.Insert(subCategory);
                }
            }

            // Получаем определение параметра
            Definition definition = GetParameterDefinition(doc, parameterName);
            if (definition == null)
            {
                sT.RollBack();
                TaskDialog.Show("Ошибка", "Параметр не найден");
                return;
            }

            // Получаем существующую привязку, если она есть
            BindingMap bindingMap = doc.ParameterBindings;
            Binding existingBinding = bindingMap.get_Item(definition);

            if (existingBinding != null)
            {
                CategorySet existingCategorySet = null;

                if (existingBinding is InstanceBinding instanceBinding)
                {
                    existingCategorySet = instanceBinding.Categories;
                }
                else if (existingBinding is TypeBinding typeBinding)
                {
                    existingCategorySet = typeBinding.Categories;
                }

                if (existingCategorySet != null)
                {
                    foreach (Category category in categorySet)
                    {
                        if (!existingCategorySet.Contains(category))
                        {
                            existingCategorySet.Insert(category);
                        }
                    }

                    // Обновляем существующую привязку
                    bindingMap.ReInsert(definition, existingBinding);
                }
            }
            else
            {
                // Создаем новую привязку
                InstanceBinding binding = new(categorySet);
                bindingMap.Insert(definition, binding, GroupTypeId.Data);
            }

            sT.Commit();
        }
        catch (Exception ex)
        {
            sT.RollBack();
            TaskDialog.Show("Ошибка", ex.Message);
        }
    }

    /// <summary>
    /// Получает список категорий, к которым параметр не привязан
    /// </summary>
    private List<ElementId> GetUnboundCategories(Document doc, string parameterName)
    {
        List<ElementId> unboundCategories = new();
        foreach (ElementId categoryId in _mepCategories)
        {
            if (!IsParameterBoundToCategory(doc, parameterName, categoryId))
            {
                unboundCategories.Add(categoryId);
            }
        }

        return unboundCategories;
    }

    /// <summary>
    /// Проверяет, привязан ли параметр к категории
    /// </summary>
    private bool IsParameterBoundToCategory(Document doc, string parameterName, ElementId categoryId)
    {
        Category category = Category.GetCategory(doc, categoryId);
        if (category == null || !category.AllowsBoundParameters)
            return false;

        // Получаем первый элемент категории
        FilteredElementCollector collector = new(doc);
        collector.OfCategoryId(categoryId)
            .WhereElementIsNotElementType();

        Element element = collector.FirstElement();
        if (element == null)
            return false;

        // Проверяем наличие параметра
        return element.LookupParameter(parameterName) != null;
    }

    /// <summary>
    /// Получает определение параметра
    /// </summary>
    private Definition GetParameterDefinition(Document doc, string parameterName)
    {
        BindingMap bindingMap = doc.ParameterBindings;
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
        iterator.Reset();

        while (iterator.MoveNext())
        {
            Definition definition = iterator.Key;
            if (definition.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

    /// <summary>
    /// Проверяет, связан ли общий параметр с документом Revit
    /// </summary>
    /// <param name="doc">Документ Revit</param>
    /// <param name="parameterName">Имя параметра</param>
    /// <returns>true если параметр связан с документом</returns>
    private bool IsSharedParameterBound(Document doc, string parameterName)
    {
        if (doc == null || string.IsNullOrEmpty(parameterName))
            return false;

        BindingMap bindingMap = doc.ParameterBindings;
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();

        while (iterator.MoveNext())
        {
            Definition definition = iterator.Key;
            if (definition?.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateViews()
    {
        Transaction tr = new(_doc, "Обновить виды");
        tr.Start();
        try
        {
            foreach (var element in _elements)
            {
                CopyParameterMep(Context.ActiveDocument, element, GetParamName, SetParamName);
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