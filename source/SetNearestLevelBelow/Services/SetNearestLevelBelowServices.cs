using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;

namespace RevitAddIn1.Services;

public class SetNearestLevelBelowServices
{
    UIDocument _uidoc = Context.ActiveUiDocument;
    Document _doc = Context.ActiveDocument;

    private readonly List<BuiltInCategory> _mepCategories =
    [
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_FlexPipeCurves,
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_PipeInsulations,
        BuiltInCategory.OST_Sprinklers,
        BuiltInCategory.OST_PlumbingEquipment,
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_FlexDuctCurves,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_DuctTerminal,
        BuiltInCategory.OST_DuctInsulations,
        BuiltInCategory.OST_DuctLinings
    ];

    public void SetNearestLevelBelow()
    {
        try
        {
            // Получаем все уровни в проекте и сортируем их по высоте
            List<Level> levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count == 0)
            {
                TaskDialog.Show("Ошибка", "В проекте не найдены уровни.");
                return;
            }

            // Получаем все MEP элементы в проекте
            List<Element> mepElements = GetAllMEPElements(_doc, _mepCategories);


            if (mepElements.Count == 0)
            {
                TaskDialog.Show("Ошибка", "Не выбрано ни одного элемента.");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            using (Transaction tx = new Transaction(_doc, "Установка базового уровня"))
            {
                tx.Start();

                foreach (Element element in mepElements)
                {
                    // Проверяем, есть ли у элемента параметр базового уровня
                    Parameter baseLevelParam;
                    switch (element)
                    {
                        case FamilyInstance:
                            baseLevelParam = element.FindParameter(BuiltInParameter.FAMILY_LEVEL_PARAM); break;
                        case MEPCurve:
                            baseLevelParam = element.FindParameter(BuiltInParameter.RBS_START_LEVEL_PARAM); break;
                        default: return;
                    }

                    if (baseLevelParam != null && !baseLevelParam.IsReadOnly)
                    {
                        // Находим ближайший нижний уровень
                        Level nearestLevelBelow = FindNearestLevelBelow(element, levels);

                        if (nearestLevelBelow != null)
                        {
                            // Устанавливаем базовый уровень
                            baseLevelParam.Set(nearestLevelBelow.Id);
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    else
                    {
                        failCount++;
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("Результат",
                $"Обработка завершена: успешно обработано {successCount} элементов, " +
                $"не удалось обработать {failCount} элементов.");

            return;
        }
        catch (Exception ex)
        {
            return;
        }
    }

    private List<Element> GetAllMEPElements(Document doc, List<BuiltInCategory> categories)
    {
        List<Element> mepElements = new List<Element>();
        foreach (var category in categories)
        {
            mepElements.AddRange(new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .ToElements());
        }

        return mepElements;
    }

    private Level FindNearestLevelBelow(Element element, List<Level> sortedLevels)
    {
        double elementZ = GetElementZPosition(element);

        if (double.IsNaN(elementZ))
            return null;

        // Находим ближайший нижний уровень
        Level nearestLevelBelow = null;

        foreach (Level level in sortedLevels)
        {
            if (level.Elevation >= elementZ)
            {
                nearestLevelBelow = level;
            }
            else
            {
                break;
            }
        }

        // Если не найден нижний уровень, берем первый (самый нижний) уровень из списка
        if (nearestLevelBelow == null && sortedLevels.Count > 0)
        {
            // Возвращаем ближайший верхний уровень (первый уровень, выше элемента)
            foreach (Level level in sortedLevels)
            {
                if (level.Elevation > elementZ)
                {
                    return level; // Возвращаем первый найденный уровень выше элемента
                }
            }

            // Если ни один уровень не выше элемента, возвращаем самый нижний уровень проекта
            return sortedLevels.First();
        }

        return nearestLevelBelow;
    }

    private double GetElementZPosition(Element element)
    {
        try
        {
            // Для разных типов элементов используем разные методы получения положения
            if (element.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point.Z;
            }
            else if (element.Location is LocationCurve locationCurve)
            {
                XYZ startPoint = locationCurve.Curve.GetEndPoint(0);
                XYZ endPoint = locationCurve.Curve.GetEndPoint(1);
                return (startPoint.Z + endPoint.Z) / 2;
            }
            else if (element.get_BoundingBox(null) != null)
            {
                return element.get_BoundingBox(null).Min.Z;
            }
        }
        catch (Exception)
        {
            // Игнорируем ошибки при получении положения
        }

        return double.NaN;
    }
}