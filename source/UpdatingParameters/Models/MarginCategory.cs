namespace UpdatingParameters.Models;

public class MarginCategory
{
    public Category Category { get; set; }
    public double Margin { get; set; }
    public Parameter FromParameter { get; set; }
    public bool IsCopyInParameter { get; set; }
    public Parameter InParameter { get; set; }
    public bool IsChecked { get; set; }
    public bool IsFromParameterValid { get; set; }
    public bool IsInParameterValid { get; set; }

    // Сохраняем оригинальные имена из DTO (для случаев когда Parameter == null)
    public string OriginalFromParameterName { get; set; }
    public string OriginalInParameterName { get; set; }

    // Computed properties для отображения
    public string FromParameterName => FromParameter?.Definition?.Name ?? OriginalFromParameterName ?? "Не выбран";
    public string InParameterName => InParameter?.Definition?.Name ?? OriginalInParameterName ?? "Не выбран";
    public string CategoryName => Category?.Name ?? "Неизвестная категория";
}