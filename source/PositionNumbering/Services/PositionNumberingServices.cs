using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using NoNameApi.Views;
using PositionNumbering.Models;

namespace PositionNumbering.Services;

public class PositionNumberingServices
{
    private readonly Document _doc = Context.ActiveDocument;

    public void AssignPositionNumbers(List<NumberingGroupModel> numberingGroups, List<Element> elements)
    {
        if (numberingGroups == null || elements == null)
            return;

        using var transaction = new Transaction(_doc, "Нумерация позиций");
        transaction.Start();
        using var progressBar = new ProgressWindow(numberingGroups.Count);
        progressBar.Show();
        try
        {
            for (var i = 0; i < numberingGroups.Count; i++)
            {
                if (progressBar.IsCancelling)
                {
                    transaction.RollBack();
                    return;
                }
                var group = numberingGroups[i];
                var systemTypeIds = group.Systems.Select(s => s.SystemTypeId).ToHashSet();
                var filteredElements = FilterElementsBySystem(elements, systemTypeIds);
                AssignNumbersToFamilies(filteredElements);
                progressBar.UpdateProgress(i + 1);
            }

            transaction.Commit();
            TaskDialog.Show("Результат", $"Выполнено");
        }
        catch (Exception e)
        {
            transaction.RollBack();
            TaskDialog.Show("ошибка", e.Message);
        }
    }

    private List<Element> FilterElementsBySystem(List<Element> elements, HashSet<long> systemTypeIds)
    {
        return elements
            .Where(e => GetMepSystemTypeId(e) is { } id && systemTypeIds.Contains(id.Value))
            .ToList();
    }

    private ElementId GetMepSystemTypeId(Element element)
    {
        switch (element)
        {
            case PipeInsulation pipeInsulation:
                return pipeInsulation.FindParameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)?.AsElementId();
            case DuctInsulation ductInsulation:
                return ductInsulation.FindParameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM)?.AsElementId();
            case MEPCurve mepCurve:
                return mepCurve.MEPSystem?.GetTypeId();
            case FamilyInstance fi:
                return GetConnectorSystemTypeId(fi);
            default:
                return null;
        }
    }

    private ElementId GetConnectorSystemTypeId(FamilyInstance fi)
    {
        var mepModel = fi.MEPModel;
        if (mepModel?.ConnectorManager?.Connectors is { } connectors)
        {
            foreach (Connector connector in connectors)
            {
                if (IsPhysicalDomain(connector.Domain) && connector.MEPSystem != null)
                {
                    return connector.MEPSystem.GetTypeId();
                }
            }
        }

        return null;
    }

    private bool IsPhysicalDomain(Domain domain)
    {
        return domain == Domain.DomainHvac || domain == Domain.DomainPiping || domain == Domain.DomainElectrical;
    }

    private void AssignNumbersToFamilies(List<Element> elements)
    {
        // Получаем все вложенные элементы
        var nestedFamilies = GetNestedFamilies(elements);
        var nestedFamilyIds = nestedFamilies.Select(e => e.Id).ToHashSet();

        // Объединяем основные элементы и вложенные для анализа
        var allElements = elements.Concat(nestedFamilies).ToList();

        // Словарь для хранения имен и соответствующих позиций
        var nameToPosition = new Dictionary<string, string>();

        // Собираем все имена родительских элементов
        var parentNames = new HashSet<string>();
        foreach (var element in elements)
        {
            if (nestedFamilyIds.Contains(element.Id))
                continue;

            var name = element.FindParameter("ADSK_Наименование")?.AsValueString() ?? "";
            if (!string.IsNullOrEmpty(name))
            {
                parentNames.Add(name);
            }
        }

        // Нумеруем родительские элементы
        var parentElements = elements
            .Where(e => !nestedFamilyIds.Contains(e.Id))
            .Where(e =>
            {
                var param = e.FindParameter("ADSK_Количество");
                return param != null && param.AsDouble() > 0;
            })
            .OrderBy(e => e.FindParameter("ADSK_Наименование")?.AsString() ?? string.Empty)
            .GroupBy(e => e.FindParameter("ADSK_Наименование")?.AsValueString() ?? "")
            .ToList();

        int familyNumber = 1;

        foreach (var group in parentElements)
        {
            string familyNumberStr = familyNumber.ToString();
            string familyName = group.Key;

            if (!string.IsNullOrEmpty(familyName))
            {
                nameToPosition[familyName] = familyNumberStr;
            }

            foreach (var element in group)
            {
                SetParameterValue(element, "ADSK_Позиция", familyNumberStr);

                // Обрабатываем вложенные элементы
                ProcessNestedElements(element, "ADSK_Позиция", familyNumberStr, nameToPosition, parentNames);
            }

            familyNumber++;
        }
    }
    private IList<Element> GetNestedFamilies(IList<Element> elements)
    {
        var nestedElements = new List<Element>();

        foreach (var element in elements.OfType<FamilyInstance>())
        {
            var subComponentIds = element.GetSubComponentIds();
            foreach (var id in subComponentIds)
            {
                if (id != null)
                {
                    var subElement = _doc.GetElement(id);
                    if (subElement is FamilyInstance)
                    {
                        nestedElements.Add(subElement);
                    }
                }
            }
        }

        return nestedElements;
    }
// Метод для нахождения родителя вложенного элемента
private ElementId FindParentElementId(Element nestedElement)
{
    FilteredElementCollector collector = new FilteredElementCollector(_doc);
    collector.OfClass(typeof(FamilyInstance));

    foreach (FamilyInstance potentialParent in collector)
    {
        if (potentialParent.GetSubComponentIds().Contains(nestedElement.Id))
            return potentialParent.Id;
    }

    return null;
}

// Метод для получения вложенных элементов с тем же именем у родителя
private List<Element> GetSiblingElements(Element parent, string name)
{
    var siblings = new List<Element>();

    if (parent is FamilyInstance familyInstance)
    {
        foreach (var nestedId in familyInstance.GetSubComponentIds())
        {
            var nested = _doc.GetElement(nestedId);
            if (nested != null)
            {
                var nestedName = nested.FindParameter("ADSK_Наименование")?.AsValueString() ?? "";
                if (nestedName == name)
                {
                    siblings.Add(nested);
                }
            }
        }
    }

    return siblings;
}


// Проверяет, содержатся ли элементы с одинаковым именем в разных родительских элементах
    private bool HasElementsInDifferentParents(List<Element> elements, HashSet<ElementId> nestedFamilyIds)
    {
        // Словарь, хранящий элемент и его родителей
        var elementToParents = new Dictionary<ElementId, HashSet<ElementId>>();

        // Заполняем словарь для вложенных элементов
        foreach (var element in elements)
        {
            if (nestedFamilyIds.Contains(element.Id))
            {
                elementToParents[element.Id] = new HashSet<ElementId>();
            }
        }

        // Для каждого возможного родителя проверяем его вложенные элементы
        foreach (var parent in _doc.GetElements().OfType<FamilyInstance>())
        {
            foreach (var nestedId in parent.GetSubComponentIds())
            {
                if (elementToParents.ContainsKey(nestedId))
                {
                    elementToParents[nestedId].Add(parent.Id);
                }
            }
        }

        // Проверяем, есть ли элементы с разными родителями
        var allParents = new HashSet<ElementId>();
        foreach (var parentSet in elementToParents.Values)
        {
            foreach (var parentId in parentSet)
            {
                if (allParents.Contains(parentId))
                {
                    return true; // Найдены разные родители
                }

                allParents.Add(parentId);
            }
        }

        return false;
    }

// Находит имена вложенных элементов, которые встречаются в разных родительских элементах
    private HashSet<string> GetDuplicateNestedNames(List<Element> elements, HashSet<ElementId> nestedFamilyIds)
    {
        var nestedElements = nestedFamilyIds
            .Select(id => _doc.GetElement(id))
            .Where(e => e != null);

        // Словарь, который связывает имя вложенного элемента с ID родителя
        var nameToParentIds = new Dictionary<string, HashSet<ElementId>>();

        foreach (var parent in elements.OfType<FamilyInstance>())
        {
            foreach (var nestedId in parent.GetSubComponentIds())
            {
                var nested = _doc.GetElement(nestedId);
                if (nested != null)
                {
                    var name = nested.FindParameter("ADSK_Наименование")?.AsValueString() ?? "";
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!nameToParentIds.ContainsKey(name))
                        {
                            nameToParentIds[name] = new HashSet<ElementId>();
                        }

                        nameToParentIds[name].Add(parent.Id);
                    }
                }
            }
        }

        // Возвращаем имена, которые встречаются в разных родителях
        return nameToParentIds
            .Where(pair => pair.Value.Count > 1)
            .Select(pair => pair.Key)
            .ToHashSet();
    }

    private void ProcessNestedElements(Element parent, string paramName, string parentNumber,
        Dictionary<string, string> nameToPosition, HashSet<string> parentNames)
    {
        if (!(parent is FamilyInstance familyInstance))
            return;

        var nestedInstances = GetNestedFamilyInstances(parent)
            .OrderBy(e => e.FindParameter("ADSK_Наименование")?.AsString() ?? string.Empty)
            .GroupBy(e => e.FindParameter("ADSK_Наименование")?.AsValueString() ?? "");

        int childIndex = 1;
        foreach (var group in nestedInstances)
        {
            string familyName = group.Key;

            // Если имя вложенного семейства совпадает с одним из родительских имен,
            // то нужно присвоить ему тот же номер, что и у родительского элемента с тем же именем
            bool isParentName = parentNames.Contains(familyName);
            string positionToSet = null;

            // Определяем позицию вложенного элемента
            if (isParentName && nameToPosition.TryGetValue(familyName, out var existingPosition))
            {
                // Если имя вложенного элемента совпадает с родительским,
                // используем номер родительского элемента
                positionToSet = existingPosition;
            }
            else
            {
                // Иначе создаем иерархический номер
                positionToSet = $"{parentNumber}.{childIndex++}";
            }

            // Применяем номер ко всем элементам группы
            foreach (var child in group)
            {
                var quantityParam = child.FindParameter("ADSK_Количество");
                if (quantityParam == null || quantityParam.AsDouble() <= 0) continue;

                SetParameterValue(child, paramName, positionToSet);

                // Рекурсивно обрабатываем более глубоко вложенные элементы
                // Но только если это не родительское имя, чтобы избежать бесконечной рекурсии
                if (!isParentName)
                {
                    ProcessNestedElements(child, paramName, positionToSet, nameToPosition, parentNames);
                }
            }
        }
    }

    private HashSet<ElementId> GetNestedFamilyIds(List<Element> elements)
    {
        return elements
            .OfType<FamilyInstance>()
            .SelectMany(fi => fi.GetSubComponentIds())
            .Where(id => id != null && _doc.GetElement(id) is FamilyInstance)
            .ToHashSet();
    }

    private List<FamilyInstance> GetNestedFamilyInstances(Element element)
    {
        if (element is not FamilyInstance fi) return new List<FamilyInstance>();

        return fi.GetSubComponentIds()
            .Select(id => _doc.GetElement(id))
            .OfType<FamilyInstance>()
            .ToList();
    }


    private void SetParameterValue(Element element, string paramName, string value)
    {
        var param = element.FindParameter(paramName);
        if (param is { IsReadOnly: false })
        {
            param.Set(value);
        }
    }
}