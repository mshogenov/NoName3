using Autodesk.Revit.UI.Selection;

namespace SystemModelingCommands.Filters;

public class CategorySelectionFilter : ISelectionFilter
{
    public Element SelectedElement { get; set; }

    public CategorySelectionFilter()
    {
    }

    public bool AllowElement(Element element)
    {
        if (SelectedElement != null && SelectedElement.Id.Value == element.Id.Value)
        {
            return false;
        }

        // Проверяем, что это не изоляция
        if (element?.Category?.Id.Value == (int)BuiltInCategory.OST_PipeInsulations ||
            element?.Category?.Id.Value == (int)BuiltInCategory.OST_DuctInsulations)
        {
            return false;
        }

        // Проверяем, что это элемент MEP и имеет открытые коннекторы
        bool isMepElement = false;
        bool hasOpenConnector = false;

        // Для элементов MEPCurve (трубы, воздуховоды, кабельные лотки)
        if (element is MEPCurve mepCurve)
        {
            isMepElement = true;
            ConnectorManager connectorManager = mepCurve.ConnectorManager;
            hasOpenConnector = HasOpenConnector(connectorManager);
        }
        // Для оборудования и фитингов (FamilyInstance)
        else if (element is FamilyInstance familyInstance)
        {
            // Проверяем, принадлежит ли категория элементу MEP
            var categoryId = element.Category.Id.Value;
            if (categoryId == (int)BuiltInCategory.OST_MechanicalEquipment ||
                categoryId == (int)BuiltInCategory.OST_PipeFitting ||
                categoryId == (int)BuiltInCategory.OST_PipeAccessory ||
                categoryId == (int)BuiltInCategory.OST_DuctFitting ||
                categoryId == (int)BuiltInCategory.OST_DuctAccessory ||
                categoryId == (int)BuiltInCategory.OST_DuctTerminal ||
                categoryId == (int)BuiltInCategory.OST_PlumbingFixtures ||
                categoryId == (int)BuiltInCategory.OST_CableTrayFitting ||
                categoryId == (int)BuiltInCategory.OST_Sprinklers)
            {
                isMepElement = true;
                MEPModel mepModel = familyInstance.MEPModel;
                if (mepModel != null)
                {
                    ConnectorManager connectorManager = mepModel.ConnectorManager;
                    if (connectorManager != null)
                    {
                        hasOpenConnector = HasOpenConnector(connectorManager);
                    }
                }
            }
        }

        // Возвращаем true только если это элемент MEP и имеет открытые коннекторы
        return isMepElement && hasOpenConnector;
    }

    private bool HasOpenConnector(ConnectorManager connectorManager)
    {
        // Проходим по всем коннекторам
        foreach (Connector connector in connectorManager.Connectors)
        {
            if (connector == null)
                continue;

            // Проверяем, что коннектор поддерживает свойство IsConnected
            if (!ConnectorSupportsIsConnected(connector))
                continue;

            // Проверяем, если коннектор не соединен ни с чем
            if (!connector.IsConnected)
            {
                return true; // Найден открытый коннектор
            }
        }

        return false; // Нет открытых коннекторов
    }

// Вспомогательный метод для проверки поддерживаемых коннекторов
    private bool ConnectorSupportsIsConnected(Connector connector)
    {
        // Проверяем, что коннектор имеет физический домен
        return connector.Domain == Domain.DomainHvac ||
               connector.Domain == Domain.DomainPiping ||
               connector.Domain == Domain.DomainElectrical;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        return true; // Разрешаем выбор точки на элементе
    }
}