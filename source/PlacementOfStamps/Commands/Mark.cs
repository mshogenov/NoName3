using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace PlacementOfStamps.Commands;
[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class Mark: IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIApplication uiApp = commandData.Application;
        UIDocument uiDoc = uiApp.ActiveUIDocument;
        Document doc = uiDoc.Document;

        try
        {
            // Шаг 1: Выбор трубы
            Reference pickedRef =
                uiDoc.Selection.PickObject(ObjectType.Element, "Выберите трубу для размещения маркировок");
            Element pipe = doc.GetElement(pickedRef);

            // Проверка категории элемента
            if (pipe.Category.Id.IntegerValue != (int)BuiltInCategory.OST_PipeCurves)
            {
                TaskDialog.Show("Ошибка", "Выбранный элемент не является трубой.");
                return Result.Failed;
            }

            // Шаг 2: Получение геометрии трубы
            Options geomOptions = new Options();
            GeometryElement geomElement = pipe.get_Geometry(geomOptions);
            Solid pipeSolid = null;

            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                {
                    pipeSolid = solid;
                    break;
                }
            }

            if (pipeSolid == null)
            {
                TaskDialog.Show("Ошибка", "Не удалось получить геометрию трубы.");
                return Result.Failed;
            }

            // Определение центральной точки трубы
            BoundingBoxXYZ bbox = pipe.get_BoundingBox(null);
            XYZ pipeCenter = (bbox.Min + bbox.Max) / 2;

            // Шаг 3: Определение области поиска
            double searchRadius = 1.0; // Радиус поиска в метрах
            double labelWidth = 0.3; // Ширина маркировки
            double labelHeight = 0.2; // Высота маркировки

            XYZ minPoint = new XYZ(pipeCenter.X - searchRadius, pipeCenter.Y - searchRadius, pipeCenter.Z);
            XYZ maxPoint = new XYZ(pipeCenter.X + searchRadius, pipeCenter.Y + searchRadius, pipeCenter.Z);

            BoundingBoxXYZ searchAreaBox = new BoundingBoxXYZ
            {
                Min = minPoint,
                Max = maxPoint
            };
            Outline outline = new Outline(searchAreaBox.Min, searchAreaBox.Max);
            {
                // Шаг 4: Сбор занятых элементов
                var collector = new FilteredElementCollector(doc)
                    .WherePasses(new BoundingBoxIntersectsFilter(outline))
                    .WhereElementIsNotElementType()
                    .Where(e => e.Category != null &&
                                (e.Category.CategoryType == CategoryType.Model ||
                                 e.Category.CategoryType == CategoryType.Annotation));

                List<BoundingBoxXYZ> occupiedAreas = new List<BoundingBoxXYZ>();

                foreach (Element elem in collector)
                {
                    BoundingBoxXYZ elemBBox = elem.get_BoundingBox(null);
                    if (elemBBox != null)
                    {
                        occupiedAreas.Add(elemBBox);
                    }
                }

                // Шаг 5a: Определение потенциальных позиций
                List<XYZ> potentialPositions = new List<XYZ>();
                double stepAngle = 10.0; // Шаг угла в градусах

                for (double angle = 0; angle < 360; angle += stepAngle)
                {
                    double rad = angle * Math.PI / 180;
                    double x = pipeCenter.X + searchRadius * Math.Cos(rad);
                    double y = pipeCenter.Y + searchRadius * Math.Sin(rad);
                    double z = pipeCenter.Z;

                    potentialPositions.Add(new XYZ(x, y, z));
                }

                // Шаг 5b: Проверка доступности позиций
                List<XYZ> suitablePositions = new List<XYZ>();

                foreach (XYZ pos in potentialPositions)
                {
                    BoundingBoxXYZ labelBBox = new BoundingBoxXYZ
                    {
                        Min = new XYZ(pos.X - labelWidth / 2, pos.Y - labelHeight / 2, pos.Z),
                        Max = new XYZ(pos.X + labelWidth / 2, pos.Y + labelHeight / 2, pos.Z)
                    };

                    bool isFree = true;

                    foreach (BoundingBoxXYZ occupied in occupiedAreas)
                    {
                        if (BoundingBoxIntersects(labelBBox, occupied))
                        {
                            isFree = false;
                            break;
                        }
                    }

                    if (isFree)
                    {
                        suitablePositions.Add(pos);
                    }
                }

                if (suitablePositions.Count == 0)
                {
                    TaskDialog.Show("Информация",
                        "Не найдено свободных мест для размещения маркировок вокруг выбранной трубы.");
                    return Result.Succeeded;
                }

                // Шаг 6: Размещение маркировок
                using (Transaction tx = new Transaction(doc, "Размещение маркировок вокруг трубы"))
                {
                    tx.Start();

                    foreach (XYZ pos in suitablePositions)
                    {
                        TextNoteOptions options = new TextNoteOptions()
                        {
                            HorizontalAlignment = HorizontalTextAlignment.Center,
                            VerticalAlignment = VerticalTextAlignment.Middle
                        };

                        // Создание TextNote. Предполагается, что активный вид — план
                        TextNote.Create(doc, doc.ActiveView.Id, pos, "Марка", options);
                    }

                    tx.Commit();
                }

                TaskDialog.Show("Готово", $"Размещено {suitablePositions.Count} маркировок вокруг выбранной трубы.");
                return Result.Succeeded;
            }
           
        }
        catch (Autodesk.Revit.Exceptions.OperationCanceledException)
        {
            // Пользователь отменил выбор
            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }


    }
    // Метод для проверки пересечения двух BoundingBoxXYZ
   private bool BoundingBoxIntersects(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
    {
        return (box1.Min.X <= box2.Max.X && box1.Max.X >= box2.Min.X) &&
               (box1.Min.Y <= box2.Max.Y && box1.Max.Y >= box2.Min.Y) &&
               (box1.Min.Z <= box2.Max.Z && box1.Max.Z >= box2.Min.Z);
    }
}