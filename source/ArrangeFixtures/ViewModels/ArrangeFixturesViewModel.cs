using ArrangeFixtures.Filters;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.External.Handlers;
using Nice3point.Revit.Toolkit.Options;
using NoNameApi.Extensions;
using NoNameApi.Utils;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

namespace ArrangeFixtures.ViewModels;

public sealed partial class ArrangeFixturesViewModel : ObservableObject
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly UIDocument _uidoc = Context.ActiveUiDocument;
    private ActionEventHandler _actionEventHandler = new();
    [ObservableProperty] private int _selectedPipesCount = 0;
    [ObservableProperty] private List<Pipe> _pipes = [];
    [ObservableProperty] private List<Element> _fixtures = [];
    [ObservableProperty] private Element _selectedFixture;


    public ArrangeFixturesViewModel()
    {
        ISet<ElementId> unusedElements = _doc.GetUnusedElements(new HashSet<ElementId>()
        {
            new(BuiltInCategory.OST_CableTrayFitting)
        });

        // Получаем неиспользуемые семейства
        List<FamilySymbol> unusedFixtures = [];
        foreach (ElementId id in unusedElements)
        {
            if (_doc.GetElement(id) is FamilySymbol element)
                unusedFixtures.Add(element);
        }

        // Получаем используемые экземпляры
        var fixtures = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_CableTrayFitting)
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>()
            .Where(fi => !fi.Symbol.Family.IsInPlace)
            .Where(fi => fi.Host == null)
            .ToList();

        var nestedFamilies = Helpers.GetNestedFamilies(_doc, fixtures);
        var nestedFamilyIds = nestedFamilies.Select(e => e.Id).ToHashSet();

        // Создаем словарь для хранения одного экземпляра каждого типа семейства
        var uniqueFixtures = new Dictionary<string, Element>();

        // Добавляем неиспользуемые типы семейств
        foreach (var fixture in unusedFixtures)
        {
            string familyAndTypeName = $"{fixture.FamilyName}_{fixture.Name}";
            if (!uniqueFixtures.ContainsKey(familyAndTypeName))
            {
                uniqueFixtures.Add(familyAndTypeName, fixture);
            }
        }

        // Добавляем по одному экземпляру каждого используемого семейства
        foreach (var fixture in fixtures)
        {
            if (nestedFamilyIds.Contains(fixture.Id))
                continue;

            string familyAndTypeName = $"{fixture.Symbol.FamilyName}_{fixture.Symbol.Name}";
            if (!uniqueFixtures.ContainsKey(familyAndTypeName))
            {
                uniqueFixtures.Add(familyAndTypeName, fixture);
            }
        }

        // Преобразуем в список
        Fixtures = uniqueFixtures.Values.ToList();
    }

    [RelayCommand]
    private void SelectedPipe()
    {
        try
        {
            var r = _uidoc.Selection.PickObjects(ObjectType.Element, new MEPCurveSelectionFilter());

            foreach (var reference in r)
            {
                Pipes.Add(_doc.GetElement(reference) as Pipe);
            }

            SelectedPipesCount = Pipes.Count;
        }
        catch (OperationCanceledException ex)
        {
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void ArrangeFixtures()
    {
        if (!Pipes.Any())
        {
            return;
        }

        FamilySymbol symbol = null;
        // Определяем символ семейства
        if (_selectedFixture is FamilySymbol fs)
        {
            symbol = fs;
        }
        else if (_selectedFixture is FamilyInstance instance)
        {
            symbol = instance.Symbol;
        }

        if (symbol == null)
            return;
        // Активируем символ семейства, если он еще не активирован
        if (!symbol.IsActive)
            symbol.Activate();

        var pipe = Pipes.FirstOrDefault();
        var point = pipe.GetPipeCenter();
        var level = _doc.GetElement(pipe.LevelId);
        _actionEventHandler.Raise(_ =>
        {
            using Transaction trans = new Transaction(_doc, "Размещение фитингов");
            try
            {
                trans.Start();
                FamilyInstance family = _doc.Create.NewFamilyInstance(point, symbol, level,
                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
              
                AlignInstanceWithPipe(family, pipe);
                trans.Commit();
            }
            catch (Exception e)
            {
                trans.RollBack();
            }
            finally
            {
                _actionEventHandler.Cancel();
            }
        });
    }

    /// <summary>
    /// Выравнивает экземпляр семейства с трубой
    /// </summary>
   /// <summary>
/// Выравнивает экземпляр семейства с трубой, используя коннекторы
/// </summary>
private void AlignInstanceWithPipe(FamilyInstance instance, Pipe pipe)
{
    // Получаем коннекторы трубы
    ConnectorSet pipeConnectors = pipe.ConnectorManager.Connectors;
    if (pipeConnectors.Size == 0)
        return;

    // Получаем коннекторы семейства
    ConnectorSet instanceConnectors = instance.MEPModel?.ConnectorManager?.Connectors;
    if (instanceConnectors == null || instanceConnectors.Size == 0)
        return;

    // Находим направление трубы
    XYZ pipeDirection = pipe.GetPipeDirection();

    // Находим локацию семейства
    LocationPoint locationPoint = instance.Location as LocationPoint;
    if (locationPoint == null)
        return;

    XYZ instancePoint = locationPoint.Point;

    // Находим основной коннектор семейства (например, первый)
    Connector primaryInstanceConnector = null;
    foreach (Connector conn in instanceConnectors)
    {
        primaryInstanceConnector = conn;
        break; // Берем первый коннектор
    }

    if (primaryInstanceConnector == null)
        return;

    // Получаем направление коннектора семейства
    XYZ instanceDirection = primaryInstanceConnector.CoordinateSystem.BasisZ;

    // Вычисляем угол между направлениями
    double angle = instanceDirection.AngleTo(pipeDirection);

    // Определяем ось вращения
    XYZ rotationAxis = instanceDirection.CrossProduct(pipeDirection);

    // Проверяем, не параллельны ли векторы
    if (rotationAxis.IsZeroLength())
    {
        // Если векторы параллельны, проверяем, нужно ли развернуть на 180 градусов
        if (instanceDirection.DotProduct(pipeDirection) < 0)
        {
            // Векторы направлены противоположно, используем перпендикулярную ось для поворота на 180°
            rotationAxis = XYZ.BasisX.CrossProduct(instanceDirection);
            if (rotationAxis.IsZeroLength())
                rotationAxis = XYZ.BasisY.CrossProduct(instanceDirection);

            angle = Math.PI; // 180 градусов
        }
        else
        {
            // Векторы уже сонаправлены
            return;
        }
    }

    // Создаем ось для вращения
    Line axis = Line.CreateBound(instancePoint, instancePoint + rotationAxis);

    // Поворачиваем экземпляр
    ElementTransformUtils.RotateElement(_doc, instance.Id, axis, angle);

}
    private void AlignConnectors(Connector targetConnector, Connector attachingConnector, Element attachingElement)
    {
        // Получаем нормализованные векторы BasisZ коннекторов
        XYZ targetBasisZ = targetConnector.CoordinateSystem.BasisZ.Normalize();
        XYZ attachingBasisZ = attachingConnector.CoordinateSystem.BasisZ.Normalize();

        // Желаемое направление для attachingBasisZ - противоположное targetBasisZ
        XYZ desiredDirection = -targetBasisZ;

        // Вычисляем скалярное произведение между attachingBasisZ и желаемым направлением
        double dotProduct = attachingBasisZ.DotProduct(desiredDirection);

        // Корректируем значение dotProduct на случай погрешностей вычислений
        dotProduct = Math.Min(Math.Max(dotProduct, -1.0), 1.0);

        // Вычисляем угол между attachingBasisZ и desiredDirection
        double angle = Math.Acos(dotProduct);

        // Вычисляем ось вращения
        XYZ rotationAxis = attachingBasisZ.CrossProduct(desiredDirection);

        // Если ось вращения имеет нулевую длину (векторы параллельны или антипараллельны)
        if (rotationAxis.IsZeroLength())
        {
            // Векторы параллельны или антипараллельны
            if (dotProduct < -0.9999)
            {
                // Векторы направлены в ту же сторону, нужно вращение на 180 градусов
                angle = Math.PI;

                // Выбираем произвольную ось вращения, перпендикулярную attachingBasisZ
                rotationAxis = attachingConnector.CoordinateSystem.BasisX;

                if (rotationAxis.IsZeroLength())
                {
                    rotationAxis = attachingConnector.CoordinateSystem.BasisY;
                }
            }
            else
            {
                // Векторы уже направлены в противоположные стороны, вращение не требуется
                angle = 0;
            }
        }
        else
        {
            // Нормализуем ось вращения
            rotationAxis = rotationAxis.Normalize();
        }

        // Выполняем вращение, если угол больше допустимого порога
        if (angle > 1e-6)
        {
            // Создаем неограниченную линию вращения с началом в attachingConnector.Origin и направлением rotationAxis
            Line rotationLine = Line.CreateUnbound(attachingConnector.Origin, rotationAxis);

            // Вращаем присоединяемый элемент
            ElementTransformUtils.RotateElement(_doc, attachingElement.Id, rotationLine, angle);
        }
    }
    
}