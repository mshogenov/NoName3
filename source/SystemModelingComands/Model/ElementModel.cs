namespace SystemModelingCommands.Model;

public class ElementModel
{
    public Reference Reference { get; set; }
    public Element Element { get; set; }
    public XYZ SelectPoint { get; set; }
    public ElementId Id { get; set; }
    public ConnectorManager ConnectorManager { get; set; }
    public Connector ClosestConnector { get; set; }

    public ElementModel(Document doc, Reference reference)
    {
        Reference = reference;
        SelectPoint = reference.GlobalPoint;
        Element = doc.GetElement(reference);
        Id = Element.Id;
        ConnectorManager = GetConnectorManager(Element);
        ClosestConnector = FindClosestConnector(ConnectorManager, SelectPoint);
    }


    private ConnectorManager GetConnectorManager(Element element)
    {
        switch (element)
        {
            // Проверяем тип элемента
            case MEPCurve pipe:
            {
                // Логика для Pipe
                return pipe.ConnectorManager;
            }
            case FamilyInstance familyInstance:
            {
                // Логика для FamilyInstance
                // Получаем MEPModel, только если это MEP-тип семейства
                MEPModel mepModel = familyInstance.MEPModel;
                {
                    return mepModel.ConnectorManager;
                }
            }
            default: return null;
        }
    }

    /// <summary>
    ///  Вспомогательный метод для поиска ближайшего соединителя
    /// </summary>
    /// <param name="connectorManager"></param>
    /// <param name="pickedPoint"></param>
    /// <returns></returns>
    private Connector FindClosestConnector(ConnectorManager connectorManager, XYZ pickedPoint)
    {
        Connector closestConnector = null;

        // Все соединители элемента
        var connectors = connectorManager.Connectors.Cast<Connector>();

        double closestDistance = double.MaxValue;

        foreach (Connector connector in connectors)
        {
            if (connector.IsConnected)
            {
                continue;
            }

            // Координаты текущего соединителя
            XYZ connectorOrigin = connector.Origin;

            // Расстояние между выбранной точкой и соединителем
            double distance = pickedPoint.DistanceTo(connectorOrigin);

            // Проверяем, является ли это расстояние минимальным
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestConnector = connector;
            }
        }

        return closestConnector;
    }
}