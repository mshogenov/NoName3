using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using NoNameApi.Views;
using SystemModelingCommands.Services;


namespace SystemModelingCommands.Views;

public sealed partial class BloomView : BaseRevitWindow, INotifyPropertyChanged
{
    public List<MEPCurveType> MepCurveTypes { get; } = [];
    private MEPCurveType _selectedMepCurveType;

    public MEPCurveType SelectedMepCurveType
    {
        get => _selectedMepCurveType;
        set
        {
            if (_selectedMepCurveType == value) return;
            _selectedMepCurveType = value;
            OnPropertyChanged();
        }
    }


    public MEPCurveType MepCurveType { get; private set; }
    private string _message;

    public string Message
    {
        get => _message;
        private set
        {
            if (_message == value) return;
            _message = value;
            OnPropertyChanged();
        }
    }

    public BloomView(Document doc, Element selectedElement)
    {
        InitializeComponent();
        DataContext = this;
        LoadWindowTemplate();
        Connector[] source = SystemModelingServices.ConnectorArrayUnused(selectedElement);
        switch (selectedElement?.Category.BuiltInCategory)
        {
            case BuiltInCategory.OST_DuctFitting or BuiltInCategory.OST_DuctAccessory:
                var ductTypes = new FilteredElementCollector(doc).OfClass(typeof(DuctType))
                    .WhereElementIsElementType()
                    .Cast<MEPCurveType>().OrderBy(x => x.Name).ToList();
                foreach (var connector in source)
                {
                    if (connector.Shape == ConnectorProfileType.Round)
                    {
                        MepCurveTypes.AddRange(ductTypes.Where(d =>
                            d.FamilyName == "Воздуховод круглого сечения"));
                        break;
                    }

                    if (connector.Shape == ConnectorProfileType.Oval)
                    {
                        MepCurveTypes.AddRange(ductTypes.Where(d =>
                            d.FamilyName == "Воздуховод овального сечения"));
                        break;
                    }

                    if (connector.Shape == ConnectorProfileType.Rectangular)
                    {
                        MepCurveTypes.AddRange(ductTypes.Where(d =>
                            d.FamilyName == "Воздуховод прямоугольного сечения"));
                        break;
                    }
                }

                break;
            case BuiltInCategory.OST_PipeFitting or BuiltInCategory.OST_PipeAccessory:
                MepCurveTypes = new FilteredElementCollector(doc).OfClass(typeof(PipeType))
                    .WhereElementIsElementType()
                    .Cast<MEPCurveType>().OrderBy(x => x.Name).ToList();
                break;
        }
    }

    private void ButtonClose_OnClick(object sender, RoutedEventArgs e)
    {
        // Находим родительское окно из кнопки (sender)
        Window window = GetWindow((Button)sender);
        window?.Close();
    }

    private void ButtonInsertPipe_OnClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMepCurveType == null)
        {
            Message = "Пожалуйста, выберите тип трубы из списка.";
        }
        else
        {
            MepCurveType = SelectedMepCurveType;
            Window window = GetWindow((Button)sender);
            window?.Close();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}