using Autodesk.Revit.DB.Plumbing;

namespace PlacementOfStamps.Models;

public class PipeMdl
{
    public bool IsDisplaced { get; set; }
    public XYZ PointDisplaced { get; set; }
    public bool IsInsulation { get; set; }
    public Pipe Pipe { get; set; }
    public double Lenght { get; set; }
    public bool IsRiser { get; set; }
    public XYZ StartPoint { get; set; }
    public XYZ EndPoint { get; set; }
    public XYZ Direction { get; set; }
    public ElementId Id { get; set; }
    public Curve Curve { get; set; }
    public bool IsPipesOuterDiameter { get; set; }

    public PipeMdl( Element element)
    {
       if (element is not Pipe pipe) return;
        Pipe = pipe;
        Id = Pipe.Id;
        LocationCurve locationCurve = (LocationCurve)pipe.Location;
        Curve = locationCurve.Curve;
        Lenght = locationCurve.Curve.Length;
        StartPoint = Curve.GetEndPoint(0);
        EndPoint = Curve.GetEndPoint(1);
        Direction = (EndPoint - StartPoint).Normalize();
        if (pipe.FindParameter(BuiltInParameter.RBS_PIPE_SLOPE) is { HasValue: false } ||
            pipe.FindParameter(BuiltInParameter.RBS_PIPE_SLOPE)?.AsDouble() > 1)
        {
            IsRiser = true;
        }

        if (pipe.FindParameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE) is { HasValue: true })
        {
            IsInsulation = true;
        }

        if (pipe.FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString() == "Днар х Стенка")
        {
            IsPipesOuterDiameter = true;
        }
    }

   
}