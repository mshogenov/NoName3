using System.Windows;
using Autodesk.Revit.DB.Architecture;
using RoomsInSpaces.Models;
using RoomsInSpaces.Services;

namespace RoomsInSpaces.ViewModels;

public sealed partial class RoomsInSpacesViewModel : ObservableObject
{
    [ObservableProperty] private List<RevitLinkInstance> _linkedFiles = [];
    [ObservableProperty] private RevitLinkInstance _selectedLinkedFile;
    private readonly Document _doc;
    private readonly RoomsInSpacesServices _roomsInSpacesServices;
    [ObservableProperty] private List<LevelInfo> _levelInfos = [];
    private readonly List<Room> _linkedRooms;

    public RoomsInSpacesViewModel()
    {
        _doc = Context.ActiveDocument;
        _roomsInSpacesServices = new RoomsInSpacesServices();
        // Собираем все экземпляры связанных файлов в документе
        var linkInstances = new FilteredElementCollector(_doc)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>();
        // Перебираем каждый экземпляр связанного файла
        foreach (RevitLinkInstance linkInstance in linkInstances)
        {
            if (linkInstance.GetLinkDocument() != null && _doc.GetElement(linkInstance.GetTypeId()).FindParameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING).AsBool() )
            {
                LinkedFiles.Add(linkInstance);
            }
        }

        if (LinkedFiles.Count != 0)
        {
            SelectedLinkedFile = LinkedFiles.First();
            Document linkedDoc = SelectedLinkedFile.GetLinkDocument();
            _linkedRooms = _roomsInSpacesServices.GetRooms(linkedDoc).ToList();
            LoadLevelsWithRoomCounts(_linkedRooms, linkedDoc);
        }
    }

    private void LoadLevelsWithRoomCounts(List<Room> linkedRooms, Document linkedDoc)
    {
        LevelInfos.Clear();

        var levelsInfo = linkedRooms
            .Where(room => room.Level != null)
            .GroupBy(room => room.Level.Id)
            .Select(group => new LevelInfo
            {
                Level = linkedDoc.GetElement(group.Key) as Level,
                LevelId = group.Key,
                LevelName = (linkedDoc.GetElement(group.Key) as Level)?.Name,
                RoomCount = group.Count()
            })
            .OrderBy(item => item.Level.Elevation)
            .ToList();

        foreach (var levelInfo in levelsInfo)
        {
            LevelInfos.Add(levelInfo);
        }
    }

    [RelayCommand]
    private void RoomsInSpaces(Window window)
    {
        window.Close();
        var selectedLinkedRooms = new List<Room>();
        foreach (var levelInfo in LevelInfos.Where(l => l.IsChecked))
        {
            selectedLinkedRooms.AddRange(_linkedRooms.Where(linkedRoom =>
                linkedRoom.Level.Id.Value == levelInfo.LevelId.Value));
        }
        _roomsInSpacesServices.RoomsInSpaces(_doc, SelectedLinkedFile, selectedLinkedRooms);
    }
    [RelayCommand]
    private void Close(object parameter)
    {
        if (parameter is Window window)
        {
            window.Close();
        }
    }
}