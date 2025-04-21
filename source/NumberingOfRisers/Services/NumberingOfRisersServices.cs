using Autodesk.Revit.DB.Plumbing;

namespace NumberingOfRisers.Services;

public class NumberingOfRisersServices
{
    public IEnumerable<Pipe> GetVerticalPipes(Document doc)
    {
        var pipeCollector = new FilteredElementCollector(doc)
            .OfClass(typeof(Pipe))
            .WhereElementIsNotElementType()
            .Cast<Pipe>();
        List<Pipe> verticalPipes = new List<Pipe>();

        // Выявляем вертикальные трубы (стояки)
        foreach (Pipe pipe in pipeCollector)
        {
            // Проверка на вертикальность (может быть разной логики)
            if (pipe.Location is not LocationCurve location) continue;
            Curve curve = location.Curve;
            XYZ direction = (curve.GetEndPoint(1) - curve.GetEndPoint(0)).Normalize();

            // Если труба вертикальная (направление по Z)
            if (Math.Abs(direction.Z) > 0.9)
            {
                verticalPipes.Add(pipe);
            }
        }
        return verticalPipes;
    }
}