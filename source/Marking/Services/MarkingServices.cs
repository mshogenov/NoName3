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
        private readonly Document _doc = Context.ActiveDocument;
        private readonly UIDocument _uiDoc = Context.ActiveUiDocument;
        private const string paramNameLevelMark = "msh_Отметка уровня";
        private const string paramNameFloor = "ADSK_Этаж";


        public (List<Element> selectedElements, List<DisplacementElement> displacedElements) SelectElements()
        {
            try
            {
                var selectionConfiguration = new SelectionConfiguration().Allow.Element(e =>
                    (e.Category?.BuiltInCategory == BuiltInCategory.OST_PipeFitting &&
                     (e.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() == "Полимерная труба")) ||
                    (e.Category?.BuiltInCategory == BuiltInCategory.OST_DisplacementElements));
                var elements = _uiDoc.Selection
                    .PickObjects(ObjectType.Element, selectionConfiguration.Filter, "Выберите элементы")
                    .Select(x => _doc.GetElement(x)).ToList();
                var displacedElements = elements
                    .Where(e => e.Category?.BuiltInCategory == BuiltInCategory.OST_DisplacementElements)
                    .Cast<DisplacementElement>()
                    .ToList();

                var selectedElements = elements
                    .Where(e => e.Category?.BuiltInCategory != BuiltInCategory.OST_DisplacementElements)
                    .ToList();

                return (selectedElements, displacedElements);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return ([], []);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", ex.Message);
                return ([], []);
            }
        }

        public void PlaceStamps(
            (List<Element> selectedElements, List<DisplacementElement> displacedElements) selectedElements,
            Element mark, bool isChecked)
        {
            List<IndependentTag> newTags = [];
            if (selectedElements.selectedElements != null)
            {
                foreach (var element in selectedElements.selectedElements)
                {
                    HeightSetting(element, isChecked);
                    var newTag = StampSetting(element, mark);
                    if (newTag != null)
                    {
                        newTags.Add(newTag);
                    }
                }
            }

            if (selectedElements.displacedElements == null) return;
            {
                foreach (var displacementElement in selectedElements.displacedElements)
                {
                    XYZ pointDisplaced = displacementElement.GetRelativeDisplacement();
                    var selectedElementsDisplaced = GetExemplarsFromOffset(displacementElement);
                    foreach (var elemDisplaced in selectedElementsDisplaced)
                    {
                        HeightSetting(elemDisplaced, isChecked);
                        var newTag = StampSetting(elemDisplaced, mark);
                        if (newTag == null) continue;
                        newTag.TagHeadPosition += pointDisplaced;
                        newTags.Add(newTag);
                    }
                }
            }
            if (newTags.Count <= 0) return;
            var validElementIds = newTags
                .Where(tag => tag is { IsValidObject: true })
                .Select(tag => tag.Id)
                .ToList();

            if (validElementIds.Count <= 0) return;
            _uiDoc.Selection.SetElementIds(validElementIds);
        }

        private List<Element> GetExemplarsFromOffset(DisplacementElement displacementElement)
        {
            List<Element> selectedElementsDisplaced = [];
            var displacedElementIds = displacementElement.GetDisplacedElementIds();
            foreach (var elementId in displacedElementIds)
            {
                var displacedElementFamily = _doc.GetElement(elementId);
                if (displacedElementFamily.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ==
                    "Полимерная труба" && displacedElementFamily is FamilyInstance)
                {
                    selectedElementsDisplaced.Add(displacedElementFamily);
                }
            }

            return selectedElementsDisplaced;
        }

        private IndependentTag StampSettingDisplaced(XYZ pointDisplaced, Element elemDisplaced, Element mark)
        {
            if (elemDisplaced.Location is not LocationPoint location) return null;
            XYZ point = location.Point;
            XYZ newPoint = new(point.X + pointDisplaced.X + 1, point.Y + pointDisplaced.Y, point.Z + pointDisplaced.Z);
            IndependentTag newTag = IndependentTag.Create(_doc, mark.Id,
                _doc.ActiveView.Id, new Reference(elemDisplaced), false, TagOrientation.Horizontal,
                newPoint);
            if (newTag != null)
            {
                newTag.TagHeadPosition = newPoint;
            }

            return newTag;
        }

        private IndependentTag StampSetting(Element element, Element mark)
        {
            if (element.Location is not LocationPoint locPoint) return null;
            XYZ point = locPoint.Point;
            XYZ newPoint = new XYZ(point.X + 1, point.Y, point.Z);
            IndependentTag newTag = IndependentTag.Create(Context.ActiveDocument, mark.Id, _doc.ActiveView.Id,
                new Reference(element), false, TagOrientation.Horizontal, newPoint);
            if (newTag != null)
            {
                newTag.TagHeadPosition = newPoint;
            }

            return newTag;
        }

        public static void HeightSetting(Element element, bool isChecked)
        {
            if (element.Location is not LocationPoint location) return;

            double elevationInFeet = location.Point.Z;
            double elevationInMeters = elevationInFeet.ToMeters();
            double roundedElevation = Math.Round(elevationInMeters, 3) + 0.0;

            // Форматируем число с нужной точностью и добавляем знак плюс
            string formattedNumber = roundedElevation >= 0
                ? roundedElevation.ToString("+0.000", CultureInfo.InvariantCulture)
                : roundedElevation.ToString("0.000", CultureInfo.InvariantCulture);

            // Установка значения в пользовательский параметр
            Parameter paramLevelMark = element.FindParameter(paramNameLevelMark);
            paramLevelMark?.Set(formattedNumber);

            Parameter paramFloor = element.FindParameter(paramNameFloor);
            if (isChecked)
            {
                string levelName = element.FindParameter(BuiltInParameter.FAMILY_LEVEL_PARAM)?.AsValueString();
                paramFloor?.Set(levelName);
            }
            else if (paramFloor?.AsValueString() != null)
            {
                paramFloor.Set((string)null);
            }
        }

        public static void DownloadFamily(Document doc, string familyName)
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
                stream?.CopyTo(fileStream);
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