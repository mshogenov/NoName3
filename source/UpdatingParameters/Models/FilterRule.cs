using System.Windows;
using System.Windows.Controls;

namespace UpdatingParameters.Models;

public partial class FilterRule : ObservableObject
{
    [ObservableProperty] private Condition _selectedCondition = Condition.Equally;
    [ObservableProperty] private List<Parameter>  _parameters;
    [ObservableProperty] private string value;
    [ObservableProperty] private UIElement _currentPopupTarget;
    [ObservableProperty] private bool _isPopupOpen;
    [ObservableProperty] private List<Parameter> _instanceParameters = [];
    [ObservableProperty] private List<Parameter> _typeParameters = [];
    [ObservableProperty] private Parameter _selectedParameter;
    public FilterGroup Parent { get; set; } // Ссылка на родительскую группу
    public Array Conditions => Enum.GetValues(typeof(Condition));

    [RelayCommand]
    private void Remove()
    {
        Parent?.Items.Remove(this);
    }
    
    [RelayCommand]
    private void SelectParameter(Button button)
    {
        if (button == null) return;
        // Устанавливаем целевой элемент для Popup
        CurrentPopupTarget = button;

        // Открываем Popup
        IsPopupOpen = true;
    }
    [RelayCommand]
    private void ClosePopup()
    {
        IsPopupOpen = false;
    }
}