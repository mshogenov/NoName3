namespace ViewOfPipeSystems.Model;

public class MEPSystemTypeModel:ObservableObject
{
    public string Name { get; set; }
    public List<MEPSystemModel> MEPSystemModels { get; set; } = [];
    private bool _isChecked;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (value == _isChecked) return;
            _isChecked = value;
            OnPropertyChanged();
            MEPSystemModels.ForEach(x=>x.IsChecked=value);
        }
    }

    public MEPSystemTypeModel(MEPSystemType mepSystemType)
    {
        if (mepSystemType == null) return;
        Document doc = mepSystemType.Document;
        Name = mepSystemType.Name;
        var collector =
            new FilteredElementCollector(doc)
                .OfClass(typeof(MEPSystem))
                .WhereElementIsNotElementType()
                .Cast<MEPSystem>().Where(x =>
                    (doc.GetElement(x.GetTypeId()) as MEPSystemType)?.Name == Name);
        foreach (var mepSystem in collector)
        {
            MEPSystemModels.Add( new MEPSystemModel(mepSystem));
        }
    }
}