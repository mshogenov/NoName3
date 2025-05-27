namespace SystemModelingCommands.Models;

public readonly struct AlignContext
{
    public ElementWrapper Target { get; }
    public ElementWrapper Attach { get; }
    public ConnectorWrapper TargetConn { get; }
    public ConnectorWrapper AttachConn { get; }

    

    public AlignContext(ElementWrapper target,ElementWrapper attach,ConnectorWrapper targetConn,ConnectorWrapper attachConn)
    {
        Target = target;
        Attach = attach;
        TargetConn = targetConn;
        AttachConn = attachConn;
    }
}