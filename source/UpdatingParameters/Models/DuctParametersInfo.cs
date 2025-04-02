namespace UpdatingParameters.Models;

public class DuctParametersInfo
{
    public string Material { get; set; }
    public string Shape { get; set; }
    public string ExternalInsulation { get; set; }
    public string InternalInsulation { get; set; }
    public double? Size { get; set; }

    public bool IsValid => !string.IsNullOrEmpty(Material) && !string.IsNullOrEmpty(Shape);
}