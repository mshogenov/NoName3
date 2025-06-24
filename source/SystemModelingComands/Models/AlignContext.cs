namespace SystemModelingCommands.Models;

public readonly struct AlignContext
{
    public ElementWrapper Target { get; }
    public ElementWrapper Attach { get; }
    public ConnectorWrapper TargetConn { get; }
    public ConnectorWrapper AttachConn { get; }


    public AlignContext([NotNull] Element target, [NotNull] Element attach, [NotNull] XYZ targetPt, [NotNull] XYZ attachPt)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (attach == null) throw new ArgumentNullException(nameof(attach));
        if (targetPt == null) throw new ArgumentNullException(nameof(targetPt));
        if (attachPt == null) throw new ArgumentNullException(nameof(attachPt));
        Target = new ElementWrapper(target) ;
        Attach = new ElementWrapper(attach) ;
        TargetConn= new ConnectorWrapper(Target.FindClosestFreeConnector(targetPt)) ;
        AttachConn = new ConnectorWrapper(Attach.FindClosestFreeConnector(attachPt)) ;
       
    }
}