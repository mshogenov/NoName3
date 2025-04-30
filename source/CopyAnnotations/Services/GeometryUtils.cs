namespace CopyAnnotations.Services;

public static class GeometryUtils
{
    // -------------------------------------------------
    //  Публичный метод – ищем ближайший экземпляр
    // -------------------------------------------------
    public static Element FindNearestElementOfCategory(
        Document doc,
        XYZ searchPoint,
        BuiltInCategory category)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (searchPoint == null) throw new ArgumentNullException(nameof(searchPoint));

        // 1. Берём экземпляры (а не типы) нужной категории в текущем виде
        var collector = new FilteredElementCollector(doc, doc.ActiveView.Id)
            .WhereElementIsNotElementType()
            .OfCategory(category);

        Element nearest = null;
        double minDist2 = double.MaxValue; // квадрат расстояния

        foreach (Element e in collector)
        {
            // ------ Быстрая проверка по bounding-box -----------------
            BoundingBoxXYZ bb = e.get_BoundingBox(null);
            if (bb == null) continue; // у некоторых элементов bbox может отсутствовать

            double bboxDist2 = DistanceSquaredToBoundingBox(searchPoint, bb);
            if (bboxDist2 >= minDist2) continue; // Уже хуже текущего лидера – пропускаем

            // ------ Уточняем расстояние (при необходимости) ----------
            //  Можно оставить только bboxDist2, если точность достаточна.
            //  Ниже – более тщательный расчёт по «репрезентативным» точкам.
            double realDist2 = GetClosestDistanceSquared(searchPoint, e);

            if (realDist2 < minDist2)
            {
                minDist2 = realDist2;
                nearest = e;
            }
        }

        return nearest;
    }

    // Квадрат расстояния до bounding-box (AABB)
    private static double DistanceSquaredToBoundingBox(XYZ p, BoundingBoxXYZ bb)
    {
        double dx = Clamp(p.X, bb.Min.X, bb.Max.X) - p.X;
        double dy = Clamp(p.Y, bb.Min.Y, bb.Max.Y) - p.Y;
        double dz = Clamp(p.Z, bb.Min.Z, bb.Max.Z) - p.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    private static double Clamp(double v, double min, double max) =>
        v < min ? min - v : (v > max ? v - max : 0.0);

    // Точный расчёт: перебираем «репрезентативные» точки элемента
    private static double GetClosestDistanceSquared(XYZ searchPoint, Element element)
    {
        double min2 = double.MaxValue;

        foreach (XYZ pt in GetElementPoints(element))
        {
            double d2 = (pt - searchPoint).GetLengthSquared();
            if (d2 < min2) min2 = d2;
        }

        return min2;
    }

    // -------------------------------------------------
    //  Сбор репрезентативных точек элемента
    // -------------------------------------------------
    private static IEnumerable<XYZ> GetElementPoints(Element element)
    {
        var pts = new HashSet<XYZ>(new XyzEquality()); // свой компаратор по AlmostEqual

        // 1. Location -------------------------------------------------
        if (element.Location is LocationPoint lp)
        {
            pts.Add(lp.Point);
        }
        else if (element.Location is LocationCurve lc)
        {
            pts.Add(lc.Curve.GetEndPoint(0));
            pts.Add(lc.Curve.GetEndPoint(1));
        }

        // 2. Bounding-box (Min, Max, Center) -------------------------
        BoundingBoxXYZ bb = element.get_BoundingBox(null);
        if (bb != null)
        {
            pts.Add(bb.Min);
            pts.Add(bb.Max);
            pts.Add((bb.Min + bb.Max) * 0.5);
        }

        // 3. Геометрия (только если ещё не набрали точек) ------------
        if (pts.Count == 0)
        {
            Options opt = new Options
            {
                ComputeReferences = false,
                IncludeNonVisibleObjects = false,
                View = element.Document.ActiveView
            };

            GeometryElement geom = element.get_Geometry(opt);
            if (geom != null)
            {
                foreach (GeometryObject gObj in geom)
                {
                    switch (gObj)
                    {
                        case Solid solid when solid.Volume > 0:
                            foreach (Edge edge in solid.Edges)
                            {
                                pts.Add(edge.AsCurve().GetEndPoint(0));
                                pts.Add(edge.AsCurve().GetEndPoint(1));
                            }

                            break;

                        case Curve curve:
                            pts.Add(curve.GetEndPoint(0));
                            pts.Add(curve.GetEndPoint(1));
                            break;

                        case Point p:
                            pts.Add(p.Coord);
                            break;
                    }
                }
            }
        }

        return pts;
    }

    // -------------------------------------------------
    //  Компаратор XYZ с допуском Revit (≈1e-9)
    // -------------------------------------------------
    private class XyzEquality : IEqualityComparer<XYZ>
    {
        public bool Equals(XYZ a, XYZ b) => a.IsAlmostEqualTo(b);

        public int GetHashCode(XYZ p) =>
            (p.X.GetHashCode() * 397) ^ (p.Y.GetHashCode() * 17) ^ p.Z.GetHashCode();
    }

    // Extension – квадрат длины вектора
    private static double GetLengthSquared(this XYZ v) => v.DotProduct(v);
}