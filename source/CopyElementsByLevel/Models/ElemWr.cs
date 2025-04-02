using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyElementsByLevel.Models
{
    public class ElemWr:ObservableObject
    {
        public Element Element { get; }

        public ElementId Id { get; }

        public Level BindingLevel { get; }
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
                    BindingLevel = mepCurve.ReferenceLevel;
                    break;
            }
        }

       
    }
}
