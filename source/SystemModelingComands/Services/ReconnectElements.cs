using Autodesk.Revit.UI;

namespace SystemModelingCommands.Services;

public class ReconnectElements
{
    /// <summary>
    /// Сохраняет информацию об оригинальных соединениях элемента
    /// </summary>
    public Dictionary<Connector, Connector> SaveConnections(Element selectedElement)
    {
        var connectionMap = new Dictionary<Connector, Connector>();

        // Получаем ConnectorManager элемента
        ConnectorManager connectorManager = GetConnectorManager(selectedElement);

        if (connectorManager == null)
        {
            return null;
        }

        // Запоминаем соединения каждого коннектора
        foreach (Connector connector in connectorManager.Connectors)
        {
            foreach (Connector refConnector in connector.AllRefs)
            {
                if (!connectionMap.ContainsKey(connector))
                {
                    connectionMap.Add(connector, refConnector);
                }
            }
        }

        return connectionMap;
    }

    private ConnectorManager GetConnectorManager(Element element)
    {
        MEPSystem system = element as MEPSystem;
        FamilyInstance familyInstance = element as FamilyInstance;

        if (system != null && system.ConnectorManager != null)
        {
            return system.ConnectorManager;
        }
        else if (familyInstance != null)
        {
            return familyInstance.MEPModel?.ConnectorManager;
        }

        return null;
    }

    /// <summary>
    /// Присоединяет элементы обратно к перемещённому элементу
    /// </summary>
    public void ReconnectAllConnections(Element selectedElement, Document doc,
        Dictionary<Connector, Connector> connectionMap)
    {
        if (connectionMap == null || connectionMap.Count == 0)
        {
            TaskDialog.Show("Ошибка", "Нет сохранённых соединений для восстановления.");
            return;
        }

        ConnectorManager connectorManager = GetConnectorManager(selectedElement);

        if (connectorManager == null)
        {
            TaskDialog.Show("Ошибка", "Элемент не содержит коннекторов.");
            return;
        }

        using (Transaction trans = new Transaction(doc, "Восстановить соединения"))
        {
            trans.Start();

            // Для каждого сохранённого соединения находим новые координаты и соединяем
            foreach (var pair in connectionMap)
            {
                Connector newConnector = FindClosestConnector(pair.Key, connectorManager);

                if (newConnector != null)
                {
                    newConnector.ConnectTo(pair.Value);
                }
            }

            trans.Commit();
        }
    }

    /// <summary>
    /// Ищем ближайший новый коннектор из перемещённого элемента
    /// </summary>
    private Connector FindClosestConnector(Connector oldConnector, ConnectorManager newConnectorManager)
    {
        Connector closestConnector = null;
        double minDistance = double.MaxValue;

        foreach (Connector newConnector in newConnectorManager.Connectors)
        {
            double distance = oldConnector.Origin.DistanceTo(newConnector.Origin);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestConnector = newConnector;
            }
        }

        return closestConnector;
    }
}