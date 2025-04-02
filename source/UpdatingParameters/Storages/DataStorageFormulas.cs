using UpdatingParameters.Models;

namespace UpdatingParameters.Storages;

public abstract class DataStorageFormulas : DataStorageBase
{
    public static event EventHandler OnDataStorageFormulasChanged;
    protected readonly IDataLoader DataLoader;
    public IEnumerable<Formula> NameFormulas { get; set; }
    public IEnumerable<Formula> NoteFormulas { get; set; }
    public IEnumerable<Formula> QuantityFormulas { get; set; }
    public bool NameIsChecked { get; set; }
    public bool NoteIsChecked { get; set; }
    public bool QuantityIsChecked { get; set; }
    protected DataStorageFormulas(IDataLoader dataLoader)
    {
        DataLoader = dataLoader;
        LoadData();
    }
    public sealed override void LoadData()
    {
        var loadedFormulas = DataLoader.LoadData<CategoryFormulas>();
        if (loadedFormulas == null)
        {
            InitializeDefault();
            loadedFormulas = DataLoader.LoadData<CategoryFormulas>() ?? new CategoryFormulas();
        }
        Initialize(loadedFormulas);
    }
    private void Initialize(CategoryFormulas loadedFormulas)
    {
        NameFormulas = loadedFormulas.AdskNameFormulas;
        NoteFormulas = loadedFormulas.AdskNoteFormulas;
        QuantityFormulas = loadedFormulas.AdskQuantityFormulas;
        NameIsChecked = loadedFormulas.NameIsChecked;
        NoteIsChecked = loadedFormulas.NoteIsChecked;
        QuantityIsChecked = loadedFormulas.QuantityIsChecked;
    }
    public override void Save()
    {
        var allFormulas = new CategoryFormulas
        {
            AdskNameFormulas = NameFormulas,
            AdskNoteFormulas = NoteFormulas,
            AdskQuantityFormulas = QuantityFormulas,
            NameIsChecked = NameIsChecked,
            NoteIsChecked = NoteIsChecked,
            QuantityIsChecked = QuantityIsChecked
        };
        DataLoader.SaveData(allFormulas);
        OnDataStorageFormulasChanged?.Invoke(this,EventArgs.Empty);
    }

}