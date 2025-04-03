using Autodesk.Revit.DB.Plumbing;
using DesignationOfRisers.Services;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace DesignationOfRisers.Models;

public partial class PipingSystemMdl : ObservableObject
{
    private Element _selectedMark;
    public string Name { get; }
    public PipingSystemType PipingSystem { get; }
    [ObservableProperty]
    private bool _isChecked = false;
    public ObservableCollection<Element> Marks { get; set; }

    // Свойство SelectedMarkId
    public Element SelectedMark
    {
        get => _selectedMark;
        set
        {
            _selectedMark = value;
            OnPropertyChanged(nameof(SelectedMark));
            OnPropertyChanged(nameof(SelectedMarkId)); // Добавьте это чтобы обновить SelectedMarkId при изменении SelectedMark
        }
    }
    public long? SelectedMarkId
    {



        get => SelectedMark?.Id.Value;
        set
        {
            if (value.HasValue)
            {
                SelectedMark = Marks.FirstOrDefault(mark => mark.Id.Value == value.Value);
            }
            else
            {
                SelectedMark = null;
            }
            OnPropertyChanged();
        }

    }
    public PipingSystemMdl(PipingSystemType pipingSystem)
    {
        Name = pipingSystem.Name;
        PipingSystem = pipingSystem;
        Marks = new ObservableCollection<Element>(
            new FilteredElementCollector(Context.ActiveDocument)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(x => x.Family.Name == "Обозначение стояка")
                .ToList<Element>());
    }
    public static PipingSystemMdlSerializable ToSerializable(PipingSystemMdl model)
    {
        return new PipingSystemMdlSerializable
        {
            Name = model.Name,
            PipingSystemId = model.PipingSystem.Id.ToString(),
            IsChecked = model.IsChecked,
            MarkIds = model.Marks.Select(mark => mark.Id.ToString()).ToList(),
            SelectedMarkId = model.SelectedMark?.Id.ToString()
        };
    }
    public static PipingSystemMdl FromSerializable(PipingSystemMdlSerializable serializable, Document document)
    {
        var pipingSystem = new FilteredElementCollector(document)
            .OfClass(typeof(PipingSystemType))
            .Cast<PipingSystemType>()
            .FirstOrDefault(x => x.Id.ToString() == serializable.PipingSystemId);

        if (pipingSystem == null)
        {
            throw new Exception("Piping system not found in document.");
        }

        var marks = serializable.MarkIds
            .Select(id => document.GetElement(new ElementId(long.Parse(id))))
            .ToList();

        var selectedMark = serializable.SelectedMarkId != null
            ? document.GetElement(new ElementId(long.Parse(serializable.SelectedMarkId)))
            : null;

        var pipingSystemMdl = new PipingSystemMdl(pipingSystem)
        {
            IsChecked = serializable.IsChecked,
            Marks = new ObservableCollection<Element>(marks),
            SelectedMark = selectedMark
        };

        return pipingSystemMdl;
    }

    public PipingSystemMdl Deserialize(string json, Document document)
    {
        var serializable = JsonConvert.DeserializeObject<PipingSystemMdlSerializable>(json);
        return PipingSystemMdl.FromSerializable(serializable, document);
    }

}