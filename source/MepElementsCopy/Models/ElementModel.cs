namespace MepElementsCopy.Models;

public class ElementModel
{
    public Element Element { get; set; }

    public ElementId Id { get; set; }

    public Level BindingLevel { get; set; }

    public ElementModel(Element element)
    {
       Element = element;
       Id = element.Id;
        switch (element)
        {
            case FamilyInstance familyInstance:
                if (familyInstance.MEPModel == null)
                    break;
               BindingLevel = element.Document.GetElement(element.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM).AsElementId()) as Level;
                break;
            case MEPCurve mepCurve:
                BindingLevel = mepCurve.ReferenceLevel;
                break;
        }
    }
}