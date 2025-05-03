namespace ViewOfPipeSystems.Model;

public partial class MEPSystemTypeModel:ObservableObject
{
    public string Abbreviation { get; set; }
    public List<MEPSystemModel> MEPSystemModels { get; set; } = [];
    [ObservableProperty] private bool _isChecked; 

    public MEPSystemTypeModel(MEPSystemType mepSystemType)
    {
        if (mepSystemType == null) return;
        Document doc = mepSystemType.Document;
        Abbreviation = mepSystemType.Abbreviation;
        var collector =
            new FilteredElementCollector(doc)
                .OfClass(typeof(MEPSystem))
                .WhereElementIsNotElementType()
                .Cast<MEPSystem>().Where(x =>
                    (doc.GetElement(x.GetTypeId()) as MEPSystemType)?.Abbreviation == Abbreviation);
        foreach (var mepSystem in collector)
        {
            MEPSystemModels.Add( new MEPSystemModel(mepSystem));
        }
    }
}