using Autodesk.Revit.DB.Plumbing;
using NumberingOfRisers.Models;

namespace NumberingOfRisers.Services
{
    public class NumberingOfRisersServices
    {
        public void AutomaticRiserNumbering(List<RiserSystemType> riserSystemTypeMdls)
        {
            // foreach (var riserSystemTypeMdl in riserSystemTypeMdls)
            // {
            //     int n = int.Parse(riserSystemTypeMdl.InitialValue);
            //     if (riserSystemTypeMdl.IsChecked)
            //     {
            //         if (riserSystemTypeMdl.CountRisers > 0)
            //         {
            //             var sortedRiserGroups = riserSystemTypeMdl.Risers
            //                 .OrderBy(x => (x.Key.Location as LocationCurve).Curve.GetEndPoint(0).X)
            //                 .ThenBy(x => (x.Key.Location as LocationCurve).Curve.GetEndPoint(0).Y);
            //             foreach (var riserGroup in sortedRiserGroups)
            //             {
            //                 foreach (var riser in riserGroup)
            //                 {
            //                     Parameter parameter = riser.FindParameter("ADSK_Номер стояка");
            //                     if (parameter != null && parameter.AsValueString() != n.ToString())
            //                     {
            //                         parameter.Set(n.ToString());
            //                     }
            //
            //                     if (parameter == null)
            //                         throw new InvalidOperationException(
            //                             $"Параметр 'ADSK_Номер стояка' не найден в элементе с ID: {riser.Id}");
            //                 }
            //
            //                 n++;
            //             }
            //         }
            //     }
            // }
        }

        public void ManualRiserNumbering(List<RiserSystemType> riserSystemTypeMdls)
        {
            // foreach (var riserSystemTypeMdl in riserSystemTypeMdls)
            // {
            //     foreach (var verticalPipesAlongLocation in riserSystemTypeMdl.Risers)
            //     {
            //         var numbersRiser = verticalPipesAlongLocation
            //             .Select(x => x.FindParameter("ADSK_Номер стояка").AsValueString())
            //             .GroupBy(x => x);
            //         var maxCountnumberRiser = numbersRiser
            //             .Max(x => x.Count());
            //         var groupWithMaxCount = numbersRiser
            //             .OrderByDescending(x => x.Count())
            //             .FirstOrDefault(); // Получим группу с наибольшим числом элементов
            //         var valueWithMaxCount = groupWithMaxCount?.Key; // Берем значение из группы
            //         foreach (var pipe in verticalPipesAlongLocation)
            //         {
            //             Parameter param = pipe.FindParameter("ADSK_Номер стояка");
            //             param?.Set(valueWithMaxCount);
            //         }
            //     }
            // }
        }

        public IEnumerable<RiserSystemType> ManualRiserReceipt(IEnumerable<Pipe> pipes)
        {
            List<RiserSystemType> riserSystemTypeMdls = [];
            var p = pipes.Where(x => !string.IsNullOrEmpty(x.FindParameter("ADSK_Номер стояка").AsString()));
            var verticalPipesOfSystemTypes = p.GroupBy(x =>
                x.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsValueString());
            // foreach (var verticalPipesOfSystemType in verticalPipesOfSystemTypes)
            // {
            //     var verticalPipesAlongLocations =
            //         verticalPipesOfSystemType.GroupBy(p => p, new PipeIEqualityComparer());
            //     riserSystemTypeMdls.Add(new RiserSystemTypeMdl(verticalPipesAlongLocations));
            // }

            return riserSystemTypeMdls;
        }

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

        public IEnumerable<RiserSystemType> AutomaticRiserReceipt(IEnumerable<Pipe> verticalPipes, int countPipes,
            double length)
        {
            List<RiserSystemType> riserSystemTypeMdls = [];
            var verticalPipesOfSystemTypes = verticalPipes.GroupBy(x =>
                x.get_Parameter(BuiltInParameter.RBS_DUCT_PIPE_SYSTEM_ABBREVIATION_PARAM).AsValueString());
            foreach (var verticalPipesOfSystemType in verticalPipesOfSystemTypes)
            {
                var verticalPipesAlongLocation = verticalPipesOfSystemType.GroupBy(p => p, new PipeIEqualityComparer());
                var filteredPipes = verticalPipesAlongLocation.Where(g => g.Count() > countPipes && g.Any(p =>
                        p.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble().ToMillimeters() > length))
                    .ToList();
                // if (filteredPipes.Count > 0)
                // {
                //     riserSystemTypeMdls.Add(new RiserSystemTypeMdl(filteredPipes));
                // }
            }

            return riserSystemTypeMdls;
        }
    }
}