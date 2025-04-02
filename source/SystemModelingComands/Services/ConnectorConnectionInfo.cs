namespace SystemModelingCommands.Services;

public class ConnectorConnectionInfo
{
    public ElementId SourceElementId { get; set; }
    public string SourceConnectorId { get; set; } // Обычно можно использовать уникальный идентификатор или имя
    public ElementId TargetElementId { get; set; }
    public string TargetConnectorId { get; set; }    
}