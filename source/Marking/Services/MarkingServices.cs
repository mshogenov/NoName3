using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.Options;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Marking.Services
{
    public class MarkingServices
    {
        public Tuple<List<Element>, List<Element>> SelectElements()
        {
            try
            {
                var selectionConfiguration = new SelectionConfiguration().Allow.Element(e => (e.Category?.BuiltInCategory == BuiltInCategory.OST_PipeFitting && (e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба")) || (e.Category?.BuiltInCategory == BuiltInCategory.OST_DisplacementElements));
                var elements = Context.ActiveUiDocument.Selection.PickObjects(ObjectType.Element, selectionConfiguration.Filter, "Выберите элементы").Select(x => Context.ActiveDocument.GetElement(x));
                List<Element> selectedElements = [];
                List<Element> displacedElements = [];
                foreach (var element in elements)
                {
                    if (element.Category.BuiltInCategory == BuiltInCategory.OST_DisplacementElements)
                    {
                        displacedElements.Add(element);
                    }
                    else selectedElements.Add(element);
                }
                return new Tuple<List<Element>, List<Element>>(selectedElements, displacedElements);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new Tuple<List<Element>, List<Element>>([], []);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
            }
            return null;
        }
        public void PlaceStamps(Tuple<List<Element>, List<Element>> selectedElements, Element mark, bool isCheked)
        {
            if (selectedElements.Item1 != null)
            {
                foreach (var el in selectedElements.Item1)
                {
                    HeightSetting(el, isCheked);
                    StampSetting(el, mark);
                }
            }
            if (selectedElements.Item2 != null)
            {
                foreach (var el in selectedElements.Item2)
                {
                    DisplacementElement displacementElement = el as DisplacementElement;
                    XYZ pointDisplaced = displacementElement.GetRelativeDisplacement();
                    List<Element> selectedElementsDisplaced = [];
                    selectedElementsDisplaced = GetExemplarsFromOffset(displacementElement);
                    foreach (var elemDisplaced in selectedElementsDisplaced)
                    {
                        HeightSetting(elemDisplaced, isCheked);
                        StampSettingDisplaced(pointDisplaced, elemDisplaced, mark);
                    }
                }
            }
        }
        private List<Element> GetExemplarsFromOffset(DisplacementElement displacementElement)
        {
            List<Element> selectedElementsDisplaced = [];
            var displacedElementIds = displacementElement.GetDisplacedElementIds();
            foreach (var elementId in displacedElementIds)
            {
                var displacedElementFamily = Context.ActiveDocument.GetElement(elementId);
                if (displacedElementFamily.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба" && displacedElementFamily is FamilyInstance)
                {
                    selectedElementsDisplaced.Add(displacedElementFamily);
                }
            }
            return selectedElementsDisplaced;
        }
        private void StampSettingDisplaced(XYZ pointDisplaced, Element elemDisplaced, Element mark)
        {
            LocationPoint location = elemDisplaced.Location as LocationPoint;
            XYZ point = location.Point;
            XYZ newPoint = new(point.X + pointDisplaced.X + 1, point.Y + pointDisplaced.Y, point.Z + pointDisplaced.Z);
            IndependentTag newTag = IndependentTag.Create(Context.ActiveDocument, mark.Id, Context.ActiveDocument.ActiveView.Id, new Reference(elemDisplaced), false, TagOrientation.Horizontal, newPoint);
            if (newTag != null)
            {
                newTag.TagHeadPosition = newPoint;
            }
        }
        private void StampSetting(Element el, Element mark)
        {
            LocationPoint locPoint = el.Location as LocationPoint;
            XYZ point = locPoint.Point;
            XYZ newPoint = new(point.X + 1, point.Y, point.Z);
            IndependentTag newTag = IndependentTag.Create(Context.ActiveDocument, mark.Id, Context.ActiveView.Id, new Reference(el), false, TagOrientation.Horizontal, newPoint);
            if (newTag != null)
            {
                newTag.TagHeadPosition = newPoint;
            }
        }
        public void HeightSetting(Element el, bool isChecked)
        {
            if (el.Location is LocationPoint location)
            {
                string paramNameLevelMark = "msh_Отметка уровня";
                string paramNameFloor = "ADSK_Этаж";
                double elevation = location.Point.Z; // Получение отметки по оси Z

                // Перевод из внутренней системы координат Revit (футы) в метры
                string elevationInMeters = Math.Round((elevation.ToMeters()), 3).ToString();
                elevationInMeters = elevationInMeters.Replace(',', '.');

                // Преобразуем строку в число с плавающей точкой
                if (double.TryParse(elevationInMeters, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                {
                    string formattedNumber = "";
                    // Форматируем число с нужной точностью и добавляем знак плюс
                    if (elevation >= 0)
                    {
                        formattedNumber = number.ToString("+0.000", CultureInfo.InvariantCulture);
                    }
                    else formattedNumber = number.ToString("0.000", CultureInfo.InvariantCulture);
                    // Установка значения в пользовательский параметр
                    Parameter paramLevelMark = el.LookupParameter(paramNameLevelMark);
                    paramLevelMark?.Set(formattedNumber);
                    Parameter paramFloor = el.LookupParameter(paramNameFloor);
                    if (isChecked)
                    {
                        string levelName = el.FindParameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsValueString();

                        paramFloor?.Set(levelName);
                    }
                    if (!isChecked && paramFloor.AsValueString() != null)
                    {
                        paramFloor.Set((string)null);
                    }
                }
            }
        }
        public void DownloadFamily(Document doc, string familyName)
        {
            // Получаем текущую сборку
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = $"Marking.Resources.{familyName}.rfa";
            // Проверяем, существует ли ресурс
            string[] resourceNames = assembly.GetManifestResourceNames();
            if (!resourceNames.Contains(resourceName))
            {
                return;
            }
            Stream stream = assembly.GetManifestResourceStream(resourceName);
            // Создаем временный файл с желаемым именем семейства
            string tempFamilyFileName = $"{familyName}.rfa";
            string tempFamilyPath = Path.Combine(Path.GetTempPath(), tempFamilyFileName);

            // Проверяем, существует ли файл, и удаляем его, если необходимо
            if (File.Exists(tempFamilyPath))
            {
                File.Delete(tempFamilyPath);
            }
            //Сохраняем поток в файл
            using (FileStream fileStream = new FileStream(tempFamilyPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
            bool loaded = doc.LoadFamily(tempFamilyPath, new FamilyLoadOptions(), out Family family);

            if (loaded && family != null)
            {
                TaskDialog.Show("Успешно", "Семейство было успешно загружено.");

            }
            else
            {
                TaskDialog.Show("Ошибка", "Не удалось загрузить семейство.");
            }
            File.Delete(tempFamilyPath);
        }
    }
}
