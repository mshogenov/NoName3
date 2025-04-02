namespace PipelineGradients.Models
{
    public class PipeMdl
    {
        public string Name { get; set; }
        public double Diameter { get; set; }
        public double UserSlope { get; set; }
        public PipeMdl(Element pipe)
        {
            Name = pipe.Name;
            Diameter = pipe.FindParameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble().ToMillimeters();
            UserSlope=pipe.FindParameter("ADSK_Уклон").AsDouble().ToMillimeters();
        }
    }
}
