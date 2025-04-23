using Autodesk.Revit.UI;

namespace RevitAddIn2.Services
{

    public class FailureReplacement : IExternalEventHandler
    {
        private readonly ExternalEvent externalEvent;

        private readonly List<ElementId> failingElementIds = new List<ElementId>();

        private readonly FailureDefinitionId failureDefinitionId = new FailureDefinitionId(new Guid("bc0dc2ef-d928-42e4-9c9b-521cb822d3fd"));

        public FailureReplacement()
        {
            externalEvent = ExternalEvent.Create(this);

            FailureDefinition.CreateFailureDefinition(failureDefinitionId, FailureSeverity.Warning, "My accurate message replacement");
        }
        public void PostFailure(IEnumerable<ElementId> failingElements)
        {
            failingElementIds.Clear();
            failingElementIds.AddRange(failingElements);
            externalEvent.Raise();
        }
        public void Execute(UIApplication app)
        {

            var element = failingElementIds.FirstOrDefault().ToElement(Context.ActiveDocument);
            if (element!= null && element is FamilyInstance family)
            {
                // Сохраняем соединения
                ConnectorManager connectorManager = family.MEPModel.ConnectorManager;
                List<KeyValuePair<Connector, List<Connector>>> connections = [];

                foreach (Connector connector in connectorManager.Connectors)
                {
                    List<Connector> connectedConnectors = new List<Connector>();
                    foreach (Connector refConnector in connector.AllRefs)
                    {
                        if (!refConnector.Owner.Id.Equals(family.Id))
                        {
                            connectedConnectors.Add(refConnector);
                        }
                    }
                    connections.Add(new KeyValuePair<Connector, List<Connector>>(connector, connectedConnectors));
                }
                try
                {
                    using Transaction tr = new(Context.ActiveDocument, "Отсоединяем соединения");
                    var failureHandlingOptions = tr.GetFailureHandlingOptions();
                    failureHandlingOptions.SetForcedModalHandling(false);
                    tr.SetFailureHandlingOptions(failureHandlingOptions);
                    tr.Start();
                    var failureMessage = new FailureMessage(failureDefinitionId);
                    Context.ActiveDocument.PostFailure(failureMessage);
                    failureMessage.SetFailingElements(failingElementIds);
                    // Отсоединяем соединения
                    foreach (var pair in connections)
                    {
                        Connector connector = pair.Key;
                        foreach (Connector refConnector in pair.Value)
                        {
                            connector.DisconnectFrom(refConnector);
                        }
                    }
                    tr.Commit();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Ошибка", ex.Message);
                }
            }
        }
        public string GetName() => nameof(FailureReplacement);
    }
}
