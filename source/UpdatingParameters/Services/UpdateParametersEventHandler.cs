namespace UpdatingParameters.Services;

using Autodesk.Revit.UI;

public class UpdateParametersEventHandler : IExternalEventHandler
{
    private Action<UIApplication> _action;

    public void Execute(UIApplication app)
    {
        _action?.Invoke(app);
    }

    public string GetName()
    {
        return "UpdateParametersEventHandler";
    }

    public void SetAction(Action<UIApplication> action)
    {
        _action = action;
    }
}