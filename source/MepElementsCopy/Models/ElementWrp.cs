namespace MepElementsCopy.Models;

public class ElementWrp
{
    public Element Element { get; set; }

    public ElementId Id { get; set; }

    public Level BindingLevel => GetBindingLevel(Element);

    public ElementWrp(Element element)
    {
        Element = element;
        Id = element.Id;
    }

    private Level GetBindingLevel(Element element)
    {
        switch (element)
        {
            case FamilyInstance familyInstance:
                if (familyInstance.MEPModel == null)
                    break;
                return element.Document.GetElement(element.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM)
                    .AsElementId()) as Level;

            case MEPCurve mepCurve:
                return mepCurve.ReferenceLevel;
        }

        return null;
    }
}