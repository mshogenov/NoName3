using SystemModelingCommands.Model;

namespace SystemModelingCommands.Services;

public readonly struct AlignContext
{
    public ElementWrapper Target { get; }
    public ElementWrapper Attach { get; }
    public Connector TargetConn { get; }
    public Connector AttachConn { get; }

    public ElementId TargetId => Target.Id;
    public ElementId AttachId => Attach.Id;

    public AlignContext(
        ElementWrapper target,
        ElementWrapper attach,
        Connector targetConn,
        Connector attachConn)
    {
        Target = target;
        Attach = attach;
        TargetConn = targetConn;
        AttachConn = attachConn;
    }
}