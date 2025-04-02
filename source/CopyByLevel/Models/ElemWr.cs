namespace CopyByLevel.Models
{
    internal class ElemWr
    {
        public ElemWr(Element element)
        {
            Element = element;
            Id = element.Id;
            switch (element)
            {
                case FamilyInstance familyInstance:
                    if (familyInstance.MEPModel != null)
                    {
                        var levelId = element.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsElementId();
                        if (levelId != null && levelId != ElementId.InvalidElementId)
                        {
                           BindingLevel = element.Document.GetElement(levelId) as Level;
                        }
                    }
                    break;
                case MEPCurve mepCurve:
                    this.BindingLevel = mepCurve.ReferenceLevel;
                    break;
            }
        }

        public Element Element { get; }

        public ElementId Id { get; }

        public Level BindingLevel { get; }
    }
}
