using Autodesk.Revit.UI;

namespace RevitAddIn.Services;

internal class CommandAvailability : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
    {
        try
        {
            // Проверяем, что есть активный UIDocument
            if (applicationData.ActiveUIDocument == null)
            {
                return false;
            }

            // Проверяем, что документ основной, а не связанный
            Document activeDocument = applicationData.ActiveUIDocument.Document;
            if (activeDocument == null)
            {
                return false;
            }

            // Получаем активный вид
            View activeView = activeDocument.ActiveView;
            if (activeView == null)
            {
                return false;
            }

            // Проверяем тип активного вида
            var viewType = activeView.ViewType;

            // Делаем команду недоступной для спецификаций
            if (viewType == ViewType.Schedule)
            {
                return false;
            }

            // Во всех остальных случаях команда доступна
            return true;
        }
        catch (Exception)
        {
            // В случае любой ошибки делаем команду недоступной
            return false;
        }
    }
}