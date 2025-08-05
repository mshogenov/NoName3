using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace NoNameApi.Extensions;

public static class ElementExtensions
{
    public static Parameter? FetchParameter(this Element element, BuiltInParameter parameter)
    {
        // Получаем тип элемента
        var elementTypeId = element.GetTypeId();
        if (elementTypeId == ElementId.InvalidElementId)
            return null;

        // Получаем элемент типа и проверяем на null
        var elementType = element.Document.GetElement(elementTypeId);
        if (elementType == null)
            return null;

        // Получаем параметр из типа
        var typeParameter = elementType.get_Parameter(parameter);

        // Важно: проверяем, что параметр существует и имеет значение
        if (typeParameter is not null)
            return typeParameter;

        // Проверяем параметр у экземпляра
        var instanceParameter = element.get_Parameter(parameter);
        if (instanceParameter is not null)
            return instanceParameter;

        return null;
    }

    public static Parameter? FetchParameter(this Element element, string parameter)
    {
        // Получаем тип элемента
        var elementTypeId = element.GetTypeId();
        if (elementTypeId == ElementId.InvalidElementId)
            return null;

        // Получаем элемент типа и проверяем на null
        var elementType = element.Document.GetElement(elementTypeId);
        if (elementType == null)
            return null;

        // Получаем параметр из типа
        var typeParameter = elementType.LookupParameter(parameter);

        // Важно: проверяем, что параметр существует и имеет значение
        if (typeParameter is not null)
            return typeParameter;

        // Проверяем параметр у экземпляра
        var instanceParameter = element.LookupParameter(parameter);
        if (instanceParameter is not null)
            return instanceParameter;

        return null;
    }

    public static Parameter? FetchParameter(this Element element, Definition definition)
    {
        // Получаем тип элемента
        var elementTypeId = element.GetTypeId();
        if (elementTypeId == ElementId.InvalidElementId)
            return null;

        // Получаем элемент типа и проверяем на null
        var elementType = element.Document.GetElement(elementTypeId);
        if (elementType == null)
            return null;

        // Получаем параметр из типа
        var typeParameter = elementType.get_Parameter(definition);

        // Важно: проверяем, что параметр существует и имеет значение
        if (typeParameter is not null)
            return typeParameter;

        // Проверяем параметр у экземпляра
        var instanceParameter = element.get_Parameter(definition);
        if (instanceParameter is not null)
            return instanceParameter;

        return null;
    }

    /// <summary>
    /// Находит 
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    public static List<Element> GetConnectedMEPElements(this Element element)
    {
        var connectedElements = new HashSet<ElementId>();
        var result = new List<Element>();
        if (element is PipeInsulation or DuctInsulation)
        {
            return result;
        }

        var connectors = GetConnectors(element);
        foreach (var connector in connectors)
        {
            if (!connector.IsConnected) continue;

            foreach (var connectedConnector in connector.AllRefs.Cast<Connector>())
            {
                if (connectedConnector.Owner.Id == element.Id) continue;

                if (connectedElements.Add(connectedConnector.Owner.Id))
                {
                    result.Add(connectedConnector.Owner);
                }
            }
        }

        return result;
    }

    public static IEnumerable<Connector> GetConnectors(this Element element)
    {
        return element switch
        {
            FamilyInstance { MEPModel: not null } family =>
                family.MEPModel.ConnectorManager.Connectors.Cast<Connector>(),
            MEPCurve curve =>
                curve.ConnectorManager.Connectors.Cast<Connector>(),
            _ => []
        };
    }

    public static List<Connector> GetConnectedMEPConnectors(this Element element)
    {
        var mepConnectors = new List<Connector>();

        try
        {
            switch (element)
            {
                case FamilyInstance { MEPModel: not null } family:
                    var familyConnectors = family.MEPModel.ConnectorManager.Connectors.Cast<Connector>();
                    mepConnectors.AddRange(familyConnectors.Where(connector => connector.IsConnected));
                    break;

                case MEPCurve curve:
                    var curveConnectors = curve.ConnectorManager.Connectors.Cast<Connector>();
                    mepConnectors.AddRange(curveConnectors.Where(connector => connector.IsConnected));
                    break;
            }
        }

        catch (Exception)
        {
            // Обработка возможных ошибок
        }

        return mepConnectors;
    }

    /// <summary>
    /// Находит соединяющий коннектор с присоединенным элементом
    /// </summary>
    /// <param name="element"></param>
    /// <param name="attachElement"></param>
    /// <returns></returns>
    public static Connector FindCommonConnector(this Element element, Element attachElement)
    {
        ConnectorSet connectorSet1 = element switch
        {
            // Проверяем тип первого элемента
            MEPCurve mepCurve => mepCurve.ConnectorManager?.Connectors,
            FamilyInstance familyInstance => familyInstance.MEPModel?.ConnectorManager?.Connectors,
            _ => null
        };

        if (connectorSet1 == null) return null;

        // Перебираем коннекторы первого элемента
        foreach (Connector connector1 in connectorSet1)
        {
            foreach (Connector connected in connector1.AllRefs)
            {
                if (connected.Owner.Id == attachElement.Id)
                {
                    return connector1;
                }
            }
        }

        return null;
    }
}