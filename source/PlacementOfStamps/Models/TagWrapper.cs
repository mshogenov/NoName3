namespace PlacementOfStamps.Models;

public class TagWrapper
{
    public IndependentTag IndependentTag { get; set; }
    public XYZ TagHeadPosition => IndependentTag.TagHeadPosition;
    public double Parameter { get; set; }
    public List<ElementWrapper> TaggedElements { get; set; } = [];
    public ElementId TagTypeId => IndependentTag.GetTypeId();
    public bool HasLeader => IndependentTag.HasLeader;
    public string Name => IndependentTag.Name;
    public BoundingBoxXYZ BoundingBox { get; set; }
    public double Distance { get; set; }


    public Rectangle Rectangle { get; set; }


    // public List<LeaderElbowModel> LeadersElbow { get; set; } = [];
    // public List<LeaderEndModel> LeadersEnd { get; set; } = [];
    public ICollection<Element> TaggedLocalElements { get; set; } = [];

    public TagWrapper(IndependentTag independentTag)
    {
        if (independentTag == null) return;
        Document doc = independentTag.Document;
        IndependentTag = independentTag;

        BoundingBox = independentTag.get_BoundingBox(doc.ActiveView);
        foreach (var taggedLocalElement in independentTag.GetTaggedLocalElements())
        {
            TaggedLocalElements.Add(taggedLocalElement);
        }

        ICollection<LinkElementId> taggedElementIds = independentTag.GetTaggedElementIds();
        if (taggedElementIds is { Count: > 0 })
        {
            foreach (var taggedElementId in taggedElementIds)
            {
                TaggedElements.Add(new ElementWrapper(doc.GetElement(taggedElementId.HostElementId)));
            }
        }

        // if (HasLeader)
        // {
        //     foreach (var taggedElement in TaggedElements)
        //     {
        //         LeadersEnd.Add(new LeaderEndModel(tag, taggedElement));
        //         LeadersElbow.Add(new LeaderElbowModel(tag, taggedElement));
        //     }
        // }
        Rectangle = GetTagBoundingBox(independentTag, doc.ActiveView);
    }

    /// <summary>
    /// Получает прямоугольник, занимаемый IndependentTag на виде
    /// </summary>
    /// <param name="tag">Марка (IndependentTag)</param>
    /// <param name="view">Вид, на котором расположена марка</param>
    /// <returns>Прямоугольник, определяющий границы марки на виде</returns>
    public Rectangle GetTagBoundingBox(IndependentTag tag, View view)
    {
        // Получаем основной BoundingBox марки
        BoundingBoxXYZ bb = tag.get_BoundingBox(view);

        // Начальные координаты из BoundingBox
        double minX = bb?.Min.X ?? double.MaxValue;
        double minY = bb?.Min.Y ?? double.MaxValue;
        double maxX = bb?.Max.X ?? double.MinValue;
        double maxY = bb?.Max.Y ?? double.MinValue;

        // Возможно, стоит добавить небольшой запас к размеру (например, 5%)
        double width = maxX - minX;
        double height = maxY - minY;
        double padding = 0;

        return new Rectangle(
            minX - width * padding,
            minY - height * padding,
            width * (1 + padding * 2),
            height * (1 + padding * 2));
    }

    /// <summary>
    /// Получает прямоугольник, занимаемый IndependentTag на виде через детальный анализ геометрии
    /// </summary>
    public Rectangle GetTagBoundingBoxFromGeometry(IndependentTag tag, View view)
    {
        // Начальные значения для границ
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        // Создаем опции для получения геометрии
        Options options = new Options();
        options.View = view; // Важно указать вид, на котором анализируем геометрию
        options.ComputeReferences = true;
        options.IncludeNonVisibleObjects = false; // Исключаем невидимые элементы

        // Получаем геометрию марки
        GeometryElement geomElem = tag.get_Geometry(options);
        if (geomElem != null)
        {
            // Рекурсивно обходим все элементы геометрии
            AnalyzeGeometry(geomElem, ref minX, ref minY, ref maxX, ref maxY);
        }

        // Если геометрия не дала результатов, используем позицию марки как запасной вариант
        if (minX == double.MaxValue || minY == double.MaxValue)
        {
            XYZ tagHeadPosition = tag.TagHeadPosition;
            minX = tagHeadPosition.X - 1.0; // Добавляем запас в 1 единицу Revit
            minY = tagHeadPosition.Y - 1.0;
            maxX = tagHeadPosition.X + 1.0;
            maxY = tagHeadPosition.Y + 1.0;
        }

        // Добавляем небольшой запас для надежности
        double width = maxX - minX;
        double height = maxY - minY;
        double padding = 0.05; // 5% запас

        return new Rectangle(
            minX - width * padding,
            minY - height * padding,
            width * (1 + padding * 2),
            height * (1 + padding * 2));
    }

    /// <summary>
    /// Рекурсивно анализирует геометрию элемента, обновляя граничные координаты
    /// </summary>
    private void AnalyzeGeometry(GeometryElement geomElem,
        ref double minX, ref double minY,
        ref double maxX, ref double maxY)
    {
        foreach (GeometryObject geomObj in geomElem)
        {
            // Для вложенных GeometryInstance получаем их геометрию
            GeometryInstance geomInstance = geomObj as GeometryInstance;
            if (geomInstance != null)
            {
                GeometryElement instanceGeom = geomInstance.GetInstanceGeometry();
                if (instanceGeom != null)
                {
                    AnalyzeGeometry(instanceGeom, ref minX, ref minY, ref maxX, ref maxY);
                }

                continue;
            }

            // Для вложенных GeometryElement рекурсивно вызываем анализ
            GeometryElement nestedGeomElem = geomObj as GeometryElement;
            if (nestedGeomElem != null)
            {
                AnalyzeGeometry(nestedGeomElem, ref minX, ref minY, ref maxX, ref maxY);
                continue;
            }

            // Обработка точек (в Revit API нет типа GeometryPoint)
            // Вместо этого обрабатываем точки из других геометрических объектов

            // Обработка линий и кривых
            Curve curve = geomObj as Curve;
            if (curve != null)
            {
                AnalyzeCurve(curve, ref minX, ref minY, ref maxX, ref maxY);
                continue;
            }

            // Обработка поверхностей и сеток
            Mesh mesh = geomObj as Mesh;
            if (mesh != null)
            {
                foreach (XYZ vertex in mesh.Vertices)
                {
                    UpdateBounds(vertex, ref minX, ref minY, ref maxX, ref maxY);
                }

                continue;
            }

            // Обработка сплошных тел
            Solid solid = geomObj as Solid;
            if (solid != null && solid.Faces.Size > 0)
            {
                foreach (Face face in solid.Faces)
                {
                    Mesh faceMesh = face.Triangulate();
                    foreach (XYZ vertex in faceMesh.Vertices)
                    {
                        UpdateBounds(vertex, ref minX, ref minY, ref maxX, ref maxY);
                    }
                }

                continue;
            }

            // Обработка текста (Edge объекты в новых версиях API)
            Edge edge = geomObj as Edge;
            if (edge != null)
            {
                IList<XYZ> edgePoints = edge.Tessellate();
                foreach (XYZ point in edgePoints)
                {
                    UpdateBounds(point, ref minX, ref minY, ref maxX, ref maxY);
                }

                continue;
            }

            // Обработка специфических для 2D представлений объектов (для более новых версий API)
            if (geomObj is PolyLine)
            {
                PolyLine polyline = geomObj as PolyLine;
                foreach (XYZ point in polyline.GetCoordinates())
                {
                    UpdateBounds(point, ref minX, ref minY, ref maxX, ref maxY);
                }

                continue;
            }
        }
    }

    /// <summary>
    /// Анализирует кривую, обновляя граничные координаты
    /// </summary>
    private void AnalyzeCurve(Curve curve,
        ref double minX, ref double minY,
        ref double maxX, ref double maxY)
    {
        // Получаем точки на концах кривой
        XYZ startPoint = curve.GetEndPoint(0);
        XYZ endPoint = curve.GetEndPoint(1);

        UpdateBounds(startPoint, ref minX, ref minY, ref maxX, ref maxY);
        UpdateBounds(endPoint, ref minX, ref minY, ref maxX, ref maxY);

        // Для более сложных кривых также анализируем промежуточные точки
        if (!(curve is Line)) // Если это не прямая линия
        {
            // Используем метод tesselate для получения точек аппроксимации кривой
            IList<XYZ> curvePoints = curve.Tessellate();
            foreach (XYZ point in curvePoints)
            {
                UpdateBounds(point, ref minX, ref minY, ref maxX, ref maxY);
            }
        }
    }

    /// <summary>
    /// Обновляет граничные координаты на основе точки
    /// </summary>
    private void UpdateBounds(XYZ point,
        ref double minX, ref double minY,
        ref double maxX, ref double maxY)
    {
        // Обновляем минимальные и максимальные координаты
        minX = Math.Min(minX, point.X);
        minY = Math.Min(minY, point.Y);
        maxX = Math.Max(maxX, point.X);
        maxY = Math.Max(maxY, point.Y);
    }
}