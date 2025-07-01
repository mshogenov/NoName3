using Autodesk.Revit.DB;

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

    public static List<Element> GetConnectedMEPElements(this Element element)
    {
        var connectedElements = new List<Element>();

        try
        {
            switch (element)
            {
                case FamilyInstance { MEPModel: not null } family:
                    var familyConnectors = family.MEPModel.ConnectorManager.Connectors.Cast<Connector>();
                    foreach (var connector in familyConnectors)
                    {
                        if (connector.IsConnected)
                        {
                            var connected = connector.AllRefs.Cast<Connector>();
                            if (connected == null) continue;
                            foreach (var c in connected)
                            {
                                connectedElements.Add(c.Owner);
                            }
                        }
                    }

                    break;

                case MEPCurve curve:
                    var curveConnectors = curve.ConnectorManager.Connectors.Cast<Connector>();
                    foreach (var connector in curveConnectors)
                    {
                        if (connector.IsConnected)
                        {
                            var connected = connector.AllRefs.Cast<Connector>();
                            if (connected != null)
                            {
                                foreach (var c in connected)
                                {
                                    connectedElements.Add(c.Owner);
                                }
                            }
                        }
                    }

                    break;
            }
        }
        catch (Exception)
        {
            // Обработка возможных ошибок
        }

        return connectedElements;
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
}