using Autodesk.Revit.UI;

namespace RevitAddIn2.Services
{
    public class FailureReplacement : IExternalEventHandler
    {
        private readonly ExternalEvent externalEvent;
 
        private readonly List<ElementId> failingElementIds = [];

        private readonly FailureDefinitionId
            failureDefinitionId = new(new Guid("bc0dc2ef-d928-42e4-9c9b-521cb822d3fd"));

        public FailureReplacement()
        {
            externalEvent = ExternalEvent.Create(this);

            FailureDefinition.CreateFailureDefinition(failureDefinitionId, FailureSeverity.Warning,
                "Не удалось изменить типоразмер");
        }

        public void PostFailure(IEnumerable<ElementId> failingElements)
        {
            failingElementIds.Clear();

            // Фильтруем только валидные ElementId
            var validIds = failingElements.Where(id => id != null && id != ElementId.InvalidElementId).ToList();

            if (validIds.Count == 0) return;
            failingElementIds.AddRange(validIds);
            externalEvent.Raise();
        }

        public void Execute(UIApplication app)
        {
            // Проверяем, что есть элементы для обработки
            if (!failingElementIds.Any())
            {
                TaskDialog.Show("Предупреждение", "Нет элементов для обработки");
                return;
            }

            // Получаем первый валидный ElementId
            var elementId = failingElementIds.FirstOrDefault(id => id != null && id != ElementId.InvalidElementId);

            if (elementId == null || elementId == ElementId.InvalidElementId)
            {
                TaskDialog.Show("Ошибка", "Не найден валидный ElementId");
                return;
            }

            var element = Context.ActiveDocument?.GetElement(elementId);
            if (element == null)
            {
                TaskDialog.Show("Ошибка", "Элемент не найден в документе");
                return;
            }

            if (element is not FamilyInstance family)
            {
                TaskDialog.Show("Ошибка", "Элемент не является экземпляром семейства");
                return;
            }

            // Проверяем наличие MEP модели
            if (family.MEPModel?.ConnectorManager == null)
            {
                TaskDialog.Show("Ошибка", "У элемента нет MEP модели или соединителей");
                return;
            }

            // Сохраняем соединения
            ConnectorManager connectorManager = family.MEPModel.ConnectorManager;
            List<KeyValuePair<Connector, List<Connector>>> connections = [];

            foreach (Connector connector in connectorManager.Connectors)
            {
                List<Connector> connectedConnectors = [];
                foreach (Connector refConnector in connector.AllRefs)
                {
                    if (refConnector.Owner.Id.Equals(family.Id)) continue;
                    if (ShouldDisconnectFrom(refConnector.Owner))
                    {
                        connectedConnectors.Add(refConnector);
                    }
                }

                connections.Add(new KeyValuePair<Connector, List<Connector>>(connector, connectedConnectors));
            }

            try
            {
                using Transaction tr = new(Context.ActiveDocument, "Отсоединить соединение");
                var failureHandlingOptions = tr.GetFailureHandlingOptions();
                failureHandlingOptions.SetForcedModalHandling(false);
                tr.SetFailureHandlingOptions(failureHandlingOptions);
                tr.Start();

                var failureMessage = new FailureMessage(failureDefinitionId);
                failureMessage.SetFailingElements(
                    failingElementIds.Where(id => id != null && id != ElementId.InvalidElementId).ToList());
                Context.ActiveDocument?.PostFailure(failureMessage);

                // Отсоединяем соединения
                foreach (var (connector, value) in connections)
                {
                    foreach (Connector refConnector in value)
                    {
                        try
                        {
                            connector.DisconnectFrom(refConnector);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Ошибка", ex.Message);
                        }
                    }
                }

                Context.ActiveUiDocument.Selection.SetElementIds(new List<ElementId>()
                {
                    family.Id
                });
                tr.Commit();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
            }
        }

        private bool ShouldDisconnectFrom(Element connectedElement)
        {
            // Список категорий, от которых НЕ нужно отсоединяться
            var excludedCategories = new[]
            {
                BuiltInCategory.OST_PipeCurves, // Трубы
                BuiltInCategory.OST_DuctCurves, // Воздуховоды
                BuiltInCategory.OST_CableTray, // Кабельные лотки
                BuiltInCategory.OST_Conduit // Кабельные каналы
            };

            var categoryId = connectedElement.Category?.Id.Value;
            return excludedCategories.All(cat => (int)cat != categoryId);
        }

        public string GetName() => nameof(FailureReplacement);
    }
}