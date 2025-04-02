using System.Windows;
using RoomsInSpaces.Services;

namespace RoomsInSpaces.ViewModels
{
    public sealed partial class RoomsInSpacesViewModel : ObservableObject
    {
        [ObservableProperty] private List<RevitLinkInstance> _linkedFiles = [];
        [ObservableProperty] private RevitLinkInstance _selectedLinkedFile;
        private readonly Document _doc;
        private readonly RoomsInSpacesServices _roomsInSpacesServices;
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
                Document linkDoc = linkInstance.GetLinkDocument();
                if (linkDoc != null)
                {
                    LinkedFiles.Add(linkInstance);
                }
            }

            if (LinkedFiles.Count != 0)
            {
                SelectedLinkedFile = LinkedFiles.First();
            }
           
        }

        [RelayCommand]
        private void RoomsInSpaces(Window window)
        {
            window.Close();
            _roomsInSpacesServices.RoomsInSpaces(_doc, SelectedLinkedFile);
         }
    }
}