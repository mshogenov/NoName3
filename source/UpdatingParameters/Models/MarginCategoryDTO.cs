namespace UpdatingParameters.Models;

public class MarginCategoryDto
{
    public string CategoryName { get; set; }
    public long CategoryId { get; set; }
    public double Margin { get; set; }
    public bool IsChecked { get; set; }
    public bool IsCopyInParameter { get; set; }
    public ParameterDto FromParameter { get; set; }
    public ParameterDto InParameter { get; set; }
}