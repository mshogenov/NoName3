namespace UpdatingParameters.Models
{
    [AttributeUsage(AttributeTargets.Field)]
    sealed class CategoryAttribute : Attribute
    {
        public MeasurementCategory Category { get; }

        public CategoryAttribute(MeasurementCategory category)
        {
            Category = category;
        }
    }
}
