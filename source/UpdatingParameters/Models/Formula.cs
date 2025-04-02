namespace UpdatingParameters.Models
{
    public partial class Formula : ObservableObject
    {
        [ObservableProperty] private string _parameterName;
        [ObservableProperty] private string _prefix;
        [ObservableProperty] private string _suffix;
        [ObservableProperty] private string _significance;
        [ObservableProperty] private string _stockpile;
        [ObservableProperty] private MeasurementUnit _measurementUnit;
       
        public MeasurementCategory MeasurementCategory
        {
            get
            {
                return MeasurementUnit switch
                {
                    MeasurementUnit.Millimeter => MeasurementCategory.Length,
                    MeasurementUnit.Meter => MeasurementCategory.Length,
                    MeasurementUnit.CubicMeter => MeasurementCategory.Volume,
                    MeasurementUnit.SquareMeters => MeasurementCategory.Area,
                    MeasurementUnit.Piece => MeasurementCategory.Quantity,
                    _ => throw new NotImplementedException(),
                };
            }
        }
    }
}
