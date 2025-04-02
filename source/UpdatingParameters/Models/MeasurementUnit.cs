using System.ComponentModel;

namespace UpdatingParameters.Models
{
    public enum MeasurementUnit
    {

        [Description("м"), Category(MeasurementCategory.Length)]
        Meter,

        [Description("мм"), Category(MeasurementCategory.Length)]
        Millimeter,

        [Description("м³"), Category(MeasurementCategory.Volume)]
        CubicMeter,
        [Description("м²"), Category(MeasurementCategory.Area)]
        SquareMeters,
        [Description("шт"), Category(MeasurementCategory.Quantity)]
        Piece


    }
}
