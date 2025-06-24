#nullable enable
using Autodesk.Revit.DB.Plumbing;

namespace PlacementOfStamps.Models;

public class PipeWrp
{
    public Pipe Pipe { get; }
    public ElementId Id { get; }

    public bool IsDisplaced { get; set; }
    public XYZ? DisplacedPoint { get; set; }

    public bool HasInsulation { get; }
    public bool IsRiser { get; }
    public bool UsesOuterDiameterNotation { get; }

    public Curve Curve => ((LocationCurve)Pipe.Location).Curve;
    public double Length => Curve.Length;
    public XYZ StartPoint => Curve.GetEndPoint(0);
    public XYZ EndPoint => Curve.GetEndPoint(1);
    public XYZ Direction => (EndPoint - StartPoint).Normalize();

    public PipeWrp(Pipe pipe)
    {
        Id = pipe.Id;
        Pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        IsRiser = CalculateIsRiser(pipe);
        HasInsulation = pipe.FindParameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE)?.HasValue == true;
        UsesOuterDiameterNotation = pipe
            .FindParameter(BuiltInParameter.WINDOW_TYPE_ID)?
            .AsValueString() == "Днар х Стенка";
    }


    private static bool CalculateIsRiser(Pipe pipe)
    {
        var slopeParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_SLOPE);

        // Если параметра нет/не заполнен, а также если значение превышает порог > 1°,
        // считаем трубу стояком (IsRiser = true)
        return slopeParam == null ||
               !slopeParam.HasValue ||
               slopeParam.AsDouble() > 1;
    }
}