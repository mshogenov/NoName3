using Autodesk.Revit.UI;

namespace ShowIn3D.Services;

public class ShowIn3DService
{
    private readonly Document _doc = Context.ActiveDocument;
    private readonly UIDocument _uiDoc = Context.ActiveUiDocument;
    private readonly View _activeView = Context.ActiveView;

    public void ShowIn3D()
    {
        ICollection<ElementId> selectedIds = _uiDoc.Selection.GetElementIds();
        if (selectedIds.Count == 0) return;

        View3D view3D;
        if (_activeView.ViewType == ViewType.ThreeD)
        {
            view3D = _activeView as View3D;
        }
        else
        {
            // Поиск открытого 3D вида
            IList<UIView> openUIViews = _uiDoc.GetOpenUIViews();
            view3D = openUIViews
                .Select(uiView => _doc.GetElement(uiView.ViewId) as View3D)
                .FirstOrDefault(v => v?.Name == "{3D}");

            // Если нет открытого 3D вида, ищем существующий или создаем новый
            if (view3D == null)
            {
                view3D = Get3DView("{3D}");
                if (view3D == null)
                {
                    view3D = CreateView3D("3D вид");
                }
            }
        }

        if (view3D == null) return;
        // Проверяем есть ли элементы на виде
        if (AreElementsVisibleInView(selectedIds, view3D))
        {
            // Активируем 3D вид
            _uiDoc.ActiveView = view3D;

            // Зумирование к выбранным элементам
            _uiDoc.ShowElements(selectedIds);
        }
        else
        {
            if (view3D.Name=="3D вид") return;
            view3D = Get3DView("3D вид");
            if (view3D == null)
            {
                view3D = CreateView3D("3D вид");
            }
            if (AreElementsVisibleInView(selectedIds, view3D))
            {
                // Активируем 3D вид
                _uiDoc.ActiveView = view3D;

                // Зумирование к выбранным элементам
                _uiDoc.ShowElements(selectedIds);
            }
        }
    }

    private View3D CreateView3D(string viewName)
    {
        var view3D = Get3DView(viewName);
        if (view3D != null) return view3D;
        using Transaction trans = new Transaction(_doc, "Создать 3D вид");
        trans.Start();
        view3D = View3D.CreateIsometric(_doc, Get3DViewFamilyType(_doc));
        view3D.Name = viewName; // Устанавливаем имя для нового вида
        view3D.DetailLevel = ViewDetailLevel.Fine;
        trans.Commit();
        return view3D;
    }

    public bool AreElementsVisibleInView(ICollection<ElementId> elementIds, View3D view3D)
    {
        // Получаем все видимые элементы в виде
        var visibleElements = new FilteredElementCollector(_doc, view3D.Id)
            .WhereElementIsNotElementType()
            .ToElementIds();

        // Проверяем есть ли хотя бы один элемент из выбранных в видимых элементах
        return elementIds.Any(id => visibleElements.Contains(id));
    }

    public View3D Get3DView(string viewName)
    {
        return new FilteredElementCollector(_doc)
            .OfClass(typeof(View3D))
            .Cast<View3D>()
            .Where(v => !v.IsTemplate) // исключаем шаблоны видов
            .FirstOrDefault(x => x.Name == viewName);
    }

    private ElementId Get3DViewFamilyType(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType))
            .Cast<ViewFamilyType>()
            .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional)
            ?.Id;
    }
}