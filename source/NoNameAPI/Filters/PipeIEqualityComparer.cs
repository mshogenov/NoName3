using System;
using Autodesk.Revit.DB;

namespace NoNameAPI.Filters;


    public class PipeIEqualityComparer : IEqualityComparer<Element>
    {
        public bool Equals(Element firstElement, Element secondElement)
        {
            if (firstElement == null || secondElement == null)
                return false;


            if (firstElement.Location is not LocationCurve FirstElementLocation || secondElement.Location is not LocationCurve secondElementLocation)
                return false;

            var firstStartPoint = FirstElementLocation.Curve.GetEndPoint(0);
            var secondStartPoint = secondElementLocation.Curve.GetEndPoint(0);

            return Math.Round(firstStartPoint.X) == Math.Round(secondStartPoint.X)
                   && Math.Round(firstStartPoint.Y) == Math.Round(secondStartPoint.Y);
        }

    public int GetHashCode(Element obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        if (obj.Location is not LocationCurve locationCurve)
            throw new ArgumentException("Object does not have a valid location curve", nameof(obj));

        var startPoint = locationCurve.Curve.GetEndPoint(0);

        // Use small integers to avoid potential overflow
        int xHash = Math.Round(startPoint.X).GetHashCode();
        int yHash = Math.Round(startPoint.Y).GetHashCode();

        unchecked // Allow overflow, which is fine in this scenario
        {
            int hash = 17;
            hash = hash * 31 + xHash;
            hash = hash * 31 + yHash;
            return hash;
        }
    }
}









