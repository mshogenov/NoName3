using System.ComponentModel;

namespace UpdatingParameters.Models;

public class DuctParameters : IDataErrorInfo
{
    public int Id { get; set; }

    private string _material;
    public string Material 
    { 
        get => _material;
        set
        {
            _material = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Material)));
        }
    }
    private string _shape;
    public string Shape 
    { 
        get => _shape;
        set
        {
            _shape = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Shape)));
        }
    }
    public string ExternalInsulation { get; set; }
    public string InternalInsulation { get; set; }
    public double? Size { get; set; }
    public double Thickness { get; set; }

    public string Error => null;

    public string this[string columnName]
    {
        get
        {
            switch (columnName)
            {
                case nameof(Material):
                    if (string.IsNullOrWhiteSpace(Material))
                        return "Материал обязателен для заполнения";
                    break;

                case nameof(Shape):
                    if (string.IsNullOrWhiteSpace(Shape))
                        return "Сечение обязательно для заполнения";
                    break;

                case nameof(Size):
                    if (Size <= 0)
                        return "Размер должен быть больше 0";
                    break;

                case nameof(Thickness):
                    if (Thickness <= 0)
                        return "Толщина должна быть больше 0";
                    break;
            }
            return null;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}