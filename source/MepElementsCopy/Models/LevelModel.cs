namespace MepElementsCopy.Models;

public class LevelModel
{
    public Level Level { get; set; }
    public string Name { get; set; }
    public bool IsChecked { get; set; }
    public ElementId Id { get; set; }
    public double Elevation { get; set; }


    public LevelModel(Level level)
    {
        if (level != null)
        {
            Level = level;
            Id = level.Id;
            Elevation = level.Elevation;
            string formattedElevation = GetFormattedElevation(level.Elevation);
            Name = $"{level.Name} ({formattedElevation})";
        }
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