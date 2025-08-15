namespace UpdatingParameters.Models;

public class ParameterInfo
{
    public string Name { get; set; }
   
    public bool IsShared { get; set; }
    public bool IsInstance { get; set; }
    public Guid? Guid { get; set; }
    public List<string> Categories { get; set; }
}