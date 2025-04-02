using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Nice3point.Revit.Extensions;


namespace LevellingOfRisers.Services
{
    public class AlignMepCurvesService
    {
       
        /// <summary>
        /// Выранивает стояк по вертикали
        /// </summary>
        /// <param name="separateRisers"></param>
        public void AlignRisersVertically(IGrouping<Element, Pipe> riser)
        {

            // Находим первую трубу один раз и запоминаем её данные
            var firstPipe = riser.First(x => x.FindParameter("Уклон").AsValueString() == null);
            var locationCurveFirstPipe = firstPipe.Location as LocationCurve;
            if (locationCurveFirstPipe != null)
            {
                // Извлекаем начальные и конечные точки первой трубы
                var startPointFirstPipe = locationCurveFirstPipe.Curve.GetEndPoint(0);
                var endPointFirstPipe = locationCurveFirstPipe.Curve.GetEndPoint(1);

                // Создаем локальные копии данных для ускорения доступа
                double startXFirst = startPointFirstPipe.X;
                double startYFirst = startPointFirstPipe.Y;
                double endXFirst = endPointFirstPipe.X;
                double endYFirst = endPointFirstPipe.Y;

                foreach (var pipe in riser)
                {
                    if (pipe.Id == firstPipe.Id) continue;

                    var locationCurve = pipe.Location as LocationCurve;
                    if (locationCurve != null)
                    {
                        // Извлекаем начальные и конечные точки текущей трубы
                        var startPoint = locationCurve.Curve.GetEndPoint(0);
                        var endPoint = locationCurve.Curve.GetEndPoint(1);

                        // Проверка совпадения координат с первой трубой
                        if (startPoint.X == startXFirst && startPoint.Y == startYFirst &&
                            endPoint.X == endXFirst && endPoint.Y == endYFirst)
                        {
                            continue;
                        }

                        // Обновляем линию только при необходимости
                        Line newLine = Line.CreateBound(
                            new XYZ(startXFirst, startYFirst, startPoint.Z),
                            new XYZ(startXFirst, startYFirst, endPoint.Z)
                        );
                        locationCurve.Curve = newLine;
                    }
                }
            }

        }
    }
}

