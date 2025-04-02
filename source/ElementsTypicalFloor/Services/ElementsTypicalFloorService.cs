using Autodesk.Revit.UI;
using NoNameApi.Utils;


namespace ElementsTypicalFloor.Services;

public class ElementsTypicalFloorService
{
    private readonly Document _doc = Context.ActiveDocument;

    public void SelectedElements(UIDocument uidoc, List<ElementId> elementIds)
    {
        if (elementIds.Count != 0)
        {
            // Устанавливаем выделение в Revit
            uidoc.Selection.SetElementIds(elementIds);
        }
    }

    public void UpdateElementParametersForTypicalFloors(List<BuiltInCategory> categories,
        string paramMshNumberWithTypicalFloors, string paramMshTypeFloorElement, int typicalFloorsCount)
    {
        var categoryFilter = new ElementMulticategoryFilter(categories);
        var collector = new FilteredElementCollector(_doc)
            .WherePasses(categoryFilter)
            .WhereElementIsNotElementType();

        using Transaction tr = new(_doc, "Обновление параметров элементов для типовых этажей");
        tr.Start();
        SubTransaction subTransaction1 = new SubTransaction(_doc);
        var flag1 =Helpers.BindParameter(_doc, paramMshTypeFloorElement, categories, subTransaction1);
        if (!flag1)
        {
            return;
        }
        SubTransaction subTransaction2 = new SubTransaction(_doc);
        var flag2 =Helpers.BindParameter(_doc, paramMshNumberWithTypicalFloors, categories, subTransaction2);
        if (!flag2)
        {
            return;
        }
        foreach (var element in collector)
        {
            const string paramAdskQuantity = "ADSK_Количество";
            var quantityParameterValue = element.FindParameter(paramAdskQuantity)?.AsDouble();
            var targetParameter = element.FindParameter(paramMshNumberWithTypicalFloors);

            if (element.FindParameter(paramMshTypeFloorElement)?.AsBool() == true)
            {
                targetParameter?.Set((double)(quantityParameterValue * (typicalFloorsCount)));
            }
            else
            {
                if (quantityParameterValue != null) targetParameter?.Set((double)quantityParameterValue);
            }
        }
        tr.Commit();
    }
}