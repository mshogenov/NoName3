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
                    // Находим ближайший нижний уровень
                    Level nearestLevelBelow = FindNearestLevelBelow(element, levels);
                    if (nearestLevelBelow != null)
                    {
                        var nearestLevelBelowId = nearestLevelBelow.Id;
                        // Получаем elevation выбранного уровня
                        double newLevelElevation = nearestLevelBelow.Elevation;
                        // Получаем текущие координаты элемента
                        XYZ currentPosition = GetElementPosition(element);
                        if (currentPosition == null)
                            continue;
                        // Получаем текущий уровень элемента
                        ElementId currentLevelId = null;
                        Parameter baseLevelParam = null;
                        Parameter offsetParam = null;
                        switch (element)
                        {
                            case FamilyInstance fi:
                                baseLevelParam = fi.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                                offsetParam = fi.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                                break;

                            case MEPCurve curve:
                                baseLevelParam = curve.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                                offsetParam = curve.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                                break;
                        }

                        if (baseLevelParam == null || offsetParam == null || baseLevelParam.IsReadOnly)
                            continue;
                        currentLevelId = baseLevelParam.AsElementId();
                        if (currentLevelId.Value < 0 || currentLevelId.Equals(nearestLevelBelowId))
                            continue;
                        // Получаем elevation текущего уровня
                        Level currentLevel = _doc.GetElement(currentLevelId) as Level;
                        if (currentLevel == null)
                            continue;
                        double currentLevelElevation = currentLevel.Elevation;

                        // Получаем текущее смещение от уровня
                        double currentOffset = offsetParam.AsDouble();

                        // Вычисляем абсолютную высоту элемента
                        double absoluteElevation = currentLevelElevation + currentOffset;

                        // Вычисляем новое смещение от нового уровня
                        double newOffset = absoluteElevation - newLevelElevation;
                        // Устанавливаем базовый уровень
                        baseLevelParam.Set(nearestLevelBelowId);
                        offsetParam.Set(newOffset);
                        successCount++;
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


    // Вспомогательный метод для получения позиции элемента
    private XYZ GetElementPosition(Element element)
    {
        if (element == null)
            return null;

        switch (element)
        {
            case FamilyInstance fi:
                return fi.Location is LocationPoint locationPoint ? locationPoint.Point : null;

            case MEPCurve mepCurve:
                if (mepCurve.Location is LocationCurve locationCurve)
                {
                    Curve curve = locationCurve.Curve;
                    return curve?.GetEndPoint(0); // Берем начальную точку кривой
                }

                return null;

            default:
                return null;
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

        // Исключаем вложенные семейства
        mepElements = mepElements.Where(e =>
        {
            // Если элемент - это экземпляр семейства
            if (e is FamilyInstance familyInstance)
            {
                // Убедимся, что у него нет хозяина, если он имеет хост, это вложенное семейство
                return familyInstance.Host == null;
            }

            // Если элемент не экземпляр семейства, оставить его
            return true;
        }).ToList();

        return mepElements;
    }

    private Level FindNearestLevelBelow(Element element, List<Level> sortedLevels, double maxDistanceAbove = 300)
    {
        double elementZ = GetElementZPosition(element);

        if (double.IsNaN(elementZ))
            return null;

        // Конвертируем миллиметры в футы (Revit использует футы)
        double maxDistanceInFeet = maxDistanceAbove / 304.8;

        // Находим ближайший нижний уровень
        Level nearestLevelBelow = null;
        Level levelAbove = null;

        // Проверяем все уровни
        foreach (Level level in sortedLevels)
        {
            // Если уровень ниже или равен элементу
            if (level.Elevation <= elementZ)
            {
                nearestLevelBelow = level;
            }
            // Если уровень выше элемента
            else
            {
                // Проверяем, находится ли уровень на указанном расстоянии или меньше над элементом
                if (level.Elevation - elementZ <= maxDistanceInFeet)
                {
                    levelAbove = level;
                }
                break;
            }
        }

        // Если есть уровень в пределах указанного расстояния над элементом, возвращаем его
        if (levelAbove != null)
        {
            return levelAbove;
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