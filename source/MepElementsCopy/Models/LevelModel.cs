namespace MepElementsCopy.Models;

public partial class LevelModel : ObservableObject
{
    public Level Level { get; set; }
    public string Name { get; set; }
    [ObservableProperty] private bool _isChecked;
    public ElementId Id { get; set; }
    public double Elevation => Level.Elevation;


    public LevelModel(Level level)
    {
        if (level == null) return;
        Level = level;
        Id = level.Id;
        string formattedElevation = GetFormattedElevation(level.Elevation);
        Name = $"{level.Name} ({formattedElevation})";
    }

    private static string GetFormattedElevation(double elevation)
    {
        // Конвертируем в миллиметры
        double elevationInMm = elevation.ToMillimeters();

        // Переводим в метры
        double elevationInMeters = elevationInMm / 1000.0;

        // Форматируем с нужной точностью
        string formattedValue = Math.Abs(elevationInMeters).ToString("0.000");

        // Добавляем знак
        return elevationInMeters >= 0
            ? $"+{formattedValue}"
            : $"-{formattedValue}";
    }
}