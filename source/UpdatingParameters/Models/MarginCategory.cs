namespace UpdatingParameters.Models;

public class MarginCategory
{
    public Category Category { get; set; }
    public double Margin { get; set; }
    public Parameter FromParameter { get; set; }
    public Parameter InParameter { get; set; }
}