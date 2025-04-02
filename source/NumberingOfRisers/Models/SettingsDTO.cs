namespace NumberingOfRisers.Models
{
    public class SettingsDTO
    {
        public bool ManualFillingIsChecked { get; set; }
        public bool AutomaticFillingIsChecked { get; set; }
        public double MinimumPipeLengthRiserPipe { get; set; }
        public int MinimumNumberPipesRiserPipe { get; set; }
    }
}
