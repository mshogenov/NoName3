namespace ViewOfPipeSystems.Model;

public partial class MEPSystemModel : ObservableObject
{
    public MEPSystem MEPSystem { get; set; }
    public string Name { get; set; }
    [ObservableProperty] private bool _isChecked;

    public MEPSystemModel(MEPSystem mepSystem)
    {
        if (mepSystem == null) return;
        MEPSystem = mepSystem;
        Name = mepSystem.Name;
    }
}