using Autodesk.Revit.UI;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using Autodesk.Revit.DB.Mechanical;
using NoNameApi.Extensions;
using NoNameApi.Utils;
using UpdatingParameters.Models;
using UpdatingParameters.Storages;


namespace UpdatingParameters.Services;

public class UpdaterParametersService
{
    private static readonly List<BuiltInCategory> MepCategories =
    [
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_FlexPipeCurves,
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_PipeAccessory,
        BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_PipeInsulations,
        BuiltInCategory.OST_Sprinklers,
        BuiltInCategory.OST_PlumbingEquipment,
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_FlexDuctCurves,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_DuctTerminal,
        BuiltInCategory.OST_DuctInsulations,
        BuiltInCategory.OST_DuctLinings
    ];

    public static List<Parameter> GetAllParameters(Element element)
    {
        List<Parameter> parameters = [];
        if (element == null) return parameters;
        parameters.AddRange(element.Parameters.Cast<Parameter>());

        ElementId typeId = element.GetTypeId();
        if (typeId == ElementId.InvalidElementId) return parameters;
        Element type = element.Document.GetElement(typeId);

        parameters.AddRange(type.Parameters.Cast<Parameter>());

        return parameters;
    }

    public static void UpdateParameters(Element element, DataStorageFormulas dataStorage)
    {
        SetNameValue(element, dataStorage);
        SetNoteValue(element, dataStorage);
        SetQuantityValue(element, dataStorage);
    }

    /// <summary>
    /// Устанавливает значение количества для данного элемента на основе предоставленных формул хранения данных.
    /// </summary>
    /// <param name="element">Элемент Revit для обновления.</param>
    /// <param name="dataStorage">Хранилище данных, содержащее формулы количества и соответствующие настройки.</param>
    private static void SetQuantityValue(Element element, DataStorageFormulas dataStorage)
    {
        // Проверка, включена ли проверка количества
        if (!dataStorage.QuantityIsChecked || dataStorage.QuantityFormulas == null ||
            !dataStorage.QuantityFormulas.Any())
        {
            return;
        }

        double quantityValue = 0;

        foreach (var formula in dataStorage.QuantityFormulas)
        {
            if (formula == null || string.IsNullOrEmpty(formula.ParameterName))
            {
                continue;
            }

            try
            {
                // Парсим значение "накопления" (stockpile)
                bool parsedStockpile = double.TryParse(
                    formula.Stockpile.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out double stockpile);

                // Обрабатываем формулу в зависимости от типа параметра
                switch (formula.ParameterName)
                {
                    case "Длина":
                        quantityValue += HandleLengthParameter(element, formula, parsedStockpile, stockpile);
                        break;

                    case "Число":
                        quantityValue += HandleNumberParameter(formula, parsedStockpile, stockpile);
                        break;

                    case "Площадь":
                        quantityValue += HandleAreaParameter(element, formula, parsedStockpile, stockpile);
                        break;

                    case "Объем":
                        quantityValue += HandleVolumeParameter(element, formula, parsedStockpile, stockpile);
                        break;

                    default:
                        // Если параметр неизвестен, пропускаем
                        continue;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка при обработке формулы '{formula.ParameterName}': {ex.Message}");
            }
        }

        // Устанавливаем значение параметра "ADSK_Количество"
        Parameter adskQuantity = element.FetchParameter("ADSK_Количество");
        var currentValue = adskQuantity?.AsDouble();
        if (adskQuantity is { IsReadOnly: false } && Math.Abs(currentValue.Value - quantityValue) > 0.001)
        {
            adskQuantity.Set(quantityValue);
        }
    }

    public static void UpdateAllMarginParameters(Document doc, SetMarginDataStorage setMarginDataStorage)
    {
        foreach (var marginCategory in setMarginDataStorage.MarginCategories)
        {
            if (!marginCategory.IsChecked) continue;

            // Получаем все элементы данной категории
            var categoryFilter = new ElementCategoryFilter(marginCategory.Category.Id);
            var elements = new FilteredElementCollector(doc)
                .WherePasses(categoryFilter)
                .WhereElementIsNotElementType()
                .ToList();

            foreach (Element element in elements)
            {
                // Получаем параметры у конкретного элемента
                var fromParam = element.FindParameter(marginCategory.FromParameter.Definition.Name);
                if (fromParam == null) continue;
                var fromValue = fromParam.AsDouble();
                double newValue = (fromValue / 100) * marginCategory.Margin + fromValue;
                ;
                if (marginCategory.InParameter != null)
                {
                    var inParam = element.FindParameter(marginCategory.InParameter.Definition.Name);
                    if (inParam == null ||
                        fromParam.StorageType != StorageType.Double ||
                        inParam.IsReadOnly) continue;
                    inParam.Set(newValue);
                }
                else
                {
                    if (fromParam.StorageType != StorageType.Double) continue;
                    fromParam.Set(newValue);
                }
            }
        }
    }


    private static double HandleLengthParameter(Element element, Formula formula, bool parsedStockpile,
        double stockpile)
    {
        var parameter = element.FetchParameter(formula.ParameterName);
        if (parameter == null) return 0;

        double significance = formula.MeasurementUnit switch
        {
            MeasurementUnit.Millimeter => parameter.AsDouble().ToMillimeters(),
            _ => parameter.AsDouble().ToMeters()
        };

        return parsedStockpile ? significance * stockpile : significance;
    }

    private static double HandleNumberParameter(Formula formula, bool parsedStockpile, double stockpile)
    {
        // Извлекаем числовое значение из строки "significance"
        if (!double.TryParse(
                Regex.Replace(formula.Significance, @"[^0-9.,]", "").Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double significance))
        {
            return 0;
        }

        return parsedStockpile ? significance * stockpile : significance;
    }

    private static double HandleAreaParameter(Element element, Formula formula, bool parsedStockpile, double stockpile)
    {
        var parameter = element.FetchParameter(formula.ParameterName);
        if (parameter == null) return 0;

        double significance = parameter.AsDouble().ToUnit(UnitTypeId.SquareMeters);
        return parsedStockpile ? significance * stockpile : significance;
    }

    private static double HandleVolumeParameter(Element element, Formula formula, bool parsedStockpile,
        double stockpile)
    {
        var parameter = element.FetchParameter(formula.ParameterName);
        if (parameter == null) return 0;

        double significance = parameter.AsDouble().ToUnit(UnitTypeId.CubicMeters);
        return parsedStockpile ? significance * stockpile : significance;
    }

    private static void SetNoteValue(Element element, DataStorageFormulas dataStorage)
    {
        if (!dataStorage.NoteIsChecked) return;
        string noteValue;
        if (element.Category.BuiltInCategory == BuiltInCategory.OST_DuctFitting &&
            dataStorage.NameFormulas.Any(x => x.ParameterName == "Размер"))
        {
            noteValue = string.Concat(dataStorage.NoteFormulas.Select(f =>
                $"{f.Prefix}{string.Join("-", element.FetchParameter(f.ParameterName)?.AsValueString()?.Split('-').Distinct()!)}{f.Suffix}"));
        }
        else
        {
            noteValue = string.Concat(dataStorage.NoteFormulas.Select(f =>
                $"{f.Prefix}{element.FetchParameter(f.ParameterName)?.AsValueString()}{f.Suffix}"));
        }

        Parameter adskNote = element.FetchParameter("ADSK_Примечание");
        string currentValue = adskNote?.AsString();
        if (adskNote is { IsReadOnly: false } && currentValue != noteValue)
        {
            adskNote.Set(noteValue);
        }
    }

    private static void SetNameValue(Element element, DataStorageFormulas dataStorage)
    {
        // Проверка, включена ли проверка имени
        if (!dataStorage.NameIsChecked) return;

        // Проверка наличия формул для имени
        if (dataStorage.NameFormulas == null || !dataStorage.NameFormulas.Any())
        {
            TaskDialog.Show("Ошибка", "Формулы имени отсутствуют");
            return;
        }

        // Поиск параметра "ADSK_Наименование"
        Parameter adskName = element.FetchParameter("ADSK_Наименование");
        if (adskName == null || adskName.IsReadOnly)
        {
            return;
        }

        var nameComponents = new List<string>();

        foreach (var formula in dataStorage.NameFormulas)
        {
            if (formula == null || string.IsNullOrEmpty(formula.ParameterName)) continue;

            try
            {
                switch (formula.ParameterName)
                {
                    case "Размер":
                        HandleSizeParameter(element, formula, nameComponents);
                        break;

                    default:
                        HandleDefaultParameter(element, formula, nameComponents);
                        break;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Ошибка при обработке формулы '{formula.ParameterName}': {ex.Message}");
            }
        }

        // Формирование итогового значения имени
        var nameValue = string.Concat(nameComponents);
        string currentValue = adskName?.AsString();
        if (!string.IsNullOrEmpty(nameValue) && !adskName.IsReadOnly && currentValue != nameValue)
        {
            adskName.Set(nameValue);
        }
    }

    private static void HandleSizeParameter(Element element, Formula formula, List<string> nameComponents)
    {
        var param = element.FetchParameter(formula.ParameterName);
        if (param == null) return;

        if (element.Category?.BuiltInCategory == BuiltInCategory.OST_DuctFitting)
        {
            var valueString = param.AsValueString()?.Split('-').ToList();
            if (valueString == null) return;
            if (valueString.Count >= 3)
            {
                var radiusParam = element.FetchParameter("Радиус воздуховода");
                if (radiusParam == null || radiusParam.AsDouble() == 0)
                {
                    var distinctValue = valueString.Distinct().ToList();
                    if (distinctValue.Count == 1)
                    {
                        var modelType = element.FetchParameter(BuiltInParameter.ALL_MODEL_MODEL)?.AsValueString();
                        if (modelType is "Тройник" or "Крестовина")
                        {
                            var prefix = modelType == "Тройник" ? "равнопроходной " : "равнопроходная ";
                            nameComponents.Add($"{formula.Prefix}{prefix}{distinctValue.First()}{formula.Suffix}");
                            return;
                        }
                    }
                }
            }

            if (element.FetchParameter(BuiltInParameter.ALL_MODEL_MODEL)?.AsValueString() == "Отвод")
            {
                nameComponents.Add($"{formula.Prefix}{string.Join("-", valueString.Distinct())}{formula.Suffix}");
                return;
            }

            var distinctValues = string.Join("-", valueString);
            nameComponents.Add($"{formula.Prefix}{distinctValues}{formula.Suffix}");
        }
        else if (element is Duct duct && duct.DuctType.Shape == ConnectorProfileType.Rectangular)
        {
            var width = duct.FetchParameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.AsDouble().ToMillimeters();
            var height = duct.FetchParameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.AsDouble().ToMillimeters();

            if (!width.HasValue || !height.HasValue) return;
            if (width < height)
            {
                (width, height) = (height, width);
            }

            var concatValue = $"{width}x{height}";
            nameComponents.Add($"{formula.Prefix}{concatValue}{formula.Suffix}");
        }
    }


    private static void HandleDefaultParameter(Element element, Formula formula, List<string> nameComponents)
    {
        var parameter = element.FetchParameter(formula.ParameterName);
        var value = parameter?.AsValueString();

        if (!string.IsNullOrWhiteSpace(value))
        {
            nameComponents.Add($"{formula.Prefix}{value}{formula.Suffix}");
        }
    }

    public static void CopyParameter(Document doc, Element element, string getParamName, string setParamName)
    {
        if (getParamName == null || setParamName == null || element == null) return;
        try
        {
            Parameter getParam = element.FetchParameter(getParamName);
            Parameter setParam = element.FetchParameter(setParamName);
            if (element.Category.BuiltInCategory == BuiltInCategory.OST_MechanicalEquipment)
            {
                if (getParamName == "Сокращение для системы")
                {
                    var connectors = (element as FamilyInstance)?.MEPModel?.ConnectorManager?.Connectors;
                    string parameterValue = string.Empty;
                    setParam = element.FindParameter(setParamName);
                    if (connectors != null)
                    {
                        foreach (Connector connector in connectors)
                        {
                            if (connector.MEPSystem == null) continue;
                            ElementId systemTypeId = connector.MEPSystem.GetTypeId();
                            Element systemType = doc.GetElement(systemTypeId);
                            Parameter getParamAbbreviation =
                                systemType?.FindParameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
                            if (getParamAbbreviation != null &&
                                !parameterValue.Contains(getParamAbbreviation.AsValueString()))
                            {
                                parameterValue += getParamAbbreviation.AsValueString() + ", ";
                            }
                        }

                        string sortedParameter = SortParameter(parameterValue);
                        if (!string.IsNullOrEmpty(sortedParameter))
                        {
                            // Устанавливаем значение для родительского элемента
                            SetParameter(sortedParameter, setParam);
                            // Рекурсивно обрабатываем все вложенные семейства
                            ProcessNestedElements(doc, element, sortedParameter, setParamName);
                            return;
                        }
                    }
                }
            }

            if (getParam == null || setParam == null) return;
            {
                // Получаем отсортированное значение аббревиатуры из родительского элемента
                string sortedParameter = SortParameter(getParam.AsValueString());
                if (string.IsNullOrEmpty(sortedParameter)) return;
                // Устанавливаем значение для родительского элемента
                SetParameter(sortedParameter, setParam);
                // Рекурсивно обрабатываем все вложенные семейства
                ProcessNestedElements(doc, element, sortedParameter, setParamName);
            }
        }
        catch (Exception ex)
        {
            var elementType = doc.GetElement(element.GetTypeId()) as ElementType;
            var familyName = elementType?.FamilyName;
            TaskDialog.Show("Ошибка",
                $"{ex.Message}\nСемейство: {familyName}\nЭлемент: {element.Name}\nID: {element.Id}");
        }
    }

// Метод для установки значения параметра
    private static void SetParameter(string sortedParameter, Parameter setParam)
    {
        string currentValue = setParam?.AsString();
        if (setParam is { IsReadOnly: false } && currentValue != sortedParameter)
        {
            setParam.Set(sortedParameter);
        }
    }

// Метод для сортировки аббревиатуры
    private static string SortParameter(string paramValue)
    {
        if (string.IsNullOrWhiteSpace(paramValue))
            return string.Empty;
        return string.Join(", ", paramValue
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x)) // Убираем пустые строки после Trim
            .OrderBy(x => x));
    }

// Рекурсивный метод для обработки всех вложенных семейств
    private static void ProcessNestedElements(Document doc, Element parentElement, string sortedParameter,
        string setParamName)
    {
        List<FamilyInstance> nestedFamilyInstances = GetNestedFamilyInstances(parentElement);
        foreach (var instance in nestedFamilyInstances)
        {
            try
            {
                Parameter setParam = instance.FetchParameter(setParamName);
                // Устанавливаем значение параметра для текущего вложенного элемента
                SetParameter(sortedParameter, setParam);
                // Рекурсивно обрабатываем вложенные экземпляры этого элемента
                ProcessNestedElements(doc, instance, sortedParameter, setParamName);
            }
            catch (Exception ex)
            {
                var elementType = doc.GetElement(instance.GetTypeId()) as ElementType;
                var familyName = elementType?.FamilyName;
                TaskDialog.Show("Ошибка",
                    $"{ex.Message}\nСемейство: {familyName}\nЭлемент: {instance.Name}\nID: {instance.Id}");
            }
        }
    }

    private static List<FamilyInstance> GetNestedFamilyInstances(Element element)
    {
        List<FamilyInstance> nestedFamilyInstances = [];
        if (element is FamilyInstance familyInstance)
        {
            foreach (ElementId subElementId in familyInstance.GetSubComponentIds())
            {
                Element nestedElement = element.Document.GetElement(subElementId) as FamilyInstance;
                if (nestedElement != null)
                {
                    nestedFamilyInstances.Add((FamilyInstance)nestedElement);
                }
            }
        }

        return nestedFamilyInstances;
    }


    public static void SetWallThickness(Element element, List<DuctParameters> ductParameters)
    {
        // Проверка, является ли элемент воздуховодом
        if (element is Duct duct)
        {
            HandleDuctWallThickness(duct, ductParameters);
        }
        else if (element.Category?.BuiltInCategory == BuiltInCategory.OST_DuctFitting)
        {
            HandleDuctFittingWallThickness(element as FamilyInstance, ductParameters);
        }
    }

    private static void HandleDuctWallThickness(Duct duct, List<DuctParameters> ductParameters)
    {
        var parameters = GetDuctParameters(duct);
        if (!parameters.IsValid)
        {
            return;
        }

        var filteredDucts = FilterDuctParameters(ductParameters, parameters);
        if (!filteredDucts.Any())
        {
            return;
        }

        double targetSize = parameters.Size ?? 0;
        var validSizes = filteredDucts
            .Select(x => x.Size)
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .ToList();

        if (!validSizes.Any())
        {
            return;
        }

        var closestSize = FindClosestNumbersEfficient(validSizes, targetSize);
        var thickness = filteredDucts.FirstOrDefault(x => x.Size == closestSize)?.Thickness ?? 0;

        SetWallThicknessParameter(duct, thickness.FromMillimeters());
    }

    private static void HandleDuctFittingWallThickness(FamilyInstance fitting, List<DuctParameters> ductParameters)
    {
        var nearestDuct = FindNearestLargerDuct(fitting, []);
        if (nearestDuct == null)
        {
            return;
        }

        Parameter adskWallThickness = fitting.FetchParameter("ADSK_Толщина стенки");
        if (adskWallThickness == null)
        {
            return;
        }

        var wallThickness = nearestDuct.FetchParameter("ADSK_Толщина стенки")?.AsDouble();
        var currentValue = adskWallThickness?.AsDouble();
        if (wallThickness > 0 && Math.Abs(currentValue.Value - wallThickness.Value) > 0.001)
        {
            adskWallThickness.Set(wallThickness.Value);
            return;
        }

        var parameters = GetDuctParameters(nearestDuct);
        if (!parameters.IsValid)
        {
            return;
        }

        var filteredDucts = FilterDuctParameters(ductParameters, parameters);
        if (!filteredDucts.Any())
        {
            return;
        }

        double targetSize = parameters.Size ?? 0;
        var validSizes = filteredDucts
            .Select(x => x.Size)
            .Where(x => x.HasValue)
            .Select(x => x.Value)
            .ToList();

        if (!validSizes.Any())
        {
            return;
        }

        var closestSize = FindClosestNumbersEfficient(validSizes, targetSize);
        var thickness = filteredDucts.FirstOrDefault(x => x.Size == closestSize)?.Thickness ?? 0;

        SetWallThicknessParameter(fitting, thickness.FromMillimeters());
    }

    private static List<DuctParameters> FilterDuctParameters(List<DuctParameters> ductParameters,
        DuctParametersInfo parameters)
    {
        return ductParameters
            .Where(p => MatchesDuctParameters(p, parameters))
            .ToList();
    }

    private static void SetWallThicknessParameter(Element element, double thickness)
    {
        var adskWallThickness = element.FetchParameter("ADSK_Толщина стенки");
        var currentValue = adskWallThickness?.AsDouble();
        if (adskWallThickness is { IsReadOnly: false } && Math.Abs(currentValue.Value - thickness) > 0.001)
        {
            adskWallThickness?.Set(thickness);
        }
    }

    private static Element FindNearestLargerDuct(FamilyInstance ductFitting,
        HashSet<ElementId> processedElements = null)
    {
        // Инициализация множества обработанных элементов
        processedElements ??= new HashSet<ElementId>();

        // Если элемент уже был обработан, пропускаем его
        if (!processedElements.Add(ductFitting.Id))
            return null;

        // Проверка на null для MEPModel и ConnectorManager
        if (ductFitting?.MEPModel?.ConnectorManager?.Connectors == null)
            return null;

        // Получение всех подключенных коннекторов
        var connectedConnectors = ductFitting.MEPModel.ConnectorManager.Connectors
            .Cast<Connector>()
            .Where(c => c.IsConnected)
            .ToList();

        if (!connectedConnectors.Any())
            return null;

        // Сбор всех подключенных элементов
        var connectedElements = connectedConnectors
            .SelectMany(connector => connector.AllRefs.Cast<Connector>().Select(c => c.Owner))
            .Where(e => e != null)
            .Distinct()
            .ToList();

        if (!connectedElements.Any())
            return null;
        // Фильтрация: оставляем только воздуховоды (игнорируем фитинги)
        var connectedDucts = connectedElements
            .OfType<Duct>() // Берем только элементы типа Duct
            .ToList();

        if (connectedDucts.Any())
        {
            // Если есть подключенные воздуховоды, ищем самый большой из них
            return FindElementWithLargeSide(connectedDucts);
        }

        // Если нет подключенных воздуховодов, рекурсивно ищем в подключенных фитингах
        foreach (var element in connectedElements)
        {
            if (element is not FamilyInstance fitting) continue;
            var result = FindNearestLargerDuct(fitting, processedElements);
            if (result != null)
                return result;
        }

        // Если ничего не найдено, возвращаем null
        return null;
    }

    private static Element FindElementWithLargeSide(IEnumerable<Element> elements)
    {
        return elements
            .OrderByDescending(FindLargerSide) // Сортировка по наибольшей стороне
            .FirstOrDefault(); // Выбор первого элемента
    }

    private static double FindClosestNumbersEfficient(List<double> numbers, double target)
    {
        if (numbers == null || numbers.Count == 0)
            throw new ArgumentException("List is empty or null");

        // Сортируем список чисел
        numbers.Sort();

        // Если target меньше всех чисел, возвращаем наименьшее число
        if (target < numbers[0])
            return numbers[0];

        // Ищем точное совпадение или ближайшее большее число
        for (int i = 0; i < numbers.Count; i++)
        {
            if (Math.Abs(numbers[i] - target) < double.Epsilon)
                return numbers[i]; // Точное совпадение
            if (numbers[i] > target)
                return numbers[i]; // Первое число, большее target
        }

        // Если target больше всех чисел, возвращаем последнее число
        return numbers[numbers.Count - 1];
    }

    private static bool MatchesDuctParameters(DuctParameters ductParam, DuctParametersInfo parameters)
    {
        // Проверка соответствия параметров
        return string.Equals(ductParam.Material, parameters.Material, StringComparison.OrdinalIgnoreCase) &&
               ductParam.Shape == parameters.Shape &&
               ductParam.ExternalInsulation == parameters.ExternalInsulation &&
               ductParam.InternalInsulation == parameters.InternalInsulation;
    }

    private static DuctParametersInfo GetDuctParameters(Element element)
    {
        if (element == null)
            return new DuctParametersInfo();

        // Получение внешней изоляции
        var externalInsulationValue =
            element.FetchParameter(BuiltInParameter.RBS_REFERENCE_INSULATION_TYPE)?.AsValueString();
        var externalInsulation = externalInsulationValue == "Огнезащита" ? externalInsulationValue : string.Empty;

        return new DuctParametersInfo
        {
            Material = element.FetchParameter("ADSK_Материал обозначение")?.AsValueString() ?? string.Empty,
            Shape = element.FetchParameter(BuiltInParameter.WINDOW_TYPE_ID)?.AsValueString() ?? string.Empty,
            ExternalInsulation = externalInsulation,
            InternalInsulation = element.FetchParameter(BuiltInParameter.RBS_REFERENCE_LINING_TYPE)?.AsValueString() ??
                                 string.Empty,
            Size = FindLargerSide(element)?.ToMillimeters()
        };
    }

    private static double? FindLargerSide(Element element)
    {
        return element switch
        {
            FamilyInstance familyInstance => GetLargerSideForFamilyInstance(familyInstance),
            Duct duct => GetLargerSideForDuct(duct),
            _ => null
        };
    }

    public static void UpdateParamSystemAbbreviation(Document doc, List<Element> elements)
    {
        const string setParamAbbreviation = "ADSK_Система_Сокращение";
        const string getParamAbbreviation = "Сокращение для системы";
        SubTransaction subTransaction = new SubTransaction(doc);
        var flag = Helpers.BindParameter(doc, setParamAbbreviation, MepCategories, subTransaction);
        if (!flag) return;
        foreach (var element in elements)
        {
            CopyParameter(Context.ActiveDocument, element, getParamAbbreviation, setParamAbbreviation);
        }
    }

    public static void UpdateParamSystemName(Document doc, List<Element> elements)
    {
        const string setParamName = "ADSK_Система_Имя";
        const string getParamName = "Имя системы";

        SubTransaction subTransaction = new SubTransaction(doc);
        var flag = Helpers.BindParameter(doc, setParamName, MepCategories, subTransaction);
        if (!flag) return;
        foreach (var element in elements)
        {
            CopyParameter(Context.ActiveDocument, element, getParamName, setParamName);
        }
    }

    public static void UpdateParamHermeticСlass(Document doc, List<Element> elements)
    {
        const string setParamName = "Класс герметичности";

        SubTransaction subTransaction = new SubTransaction(doc);
        var flag = Helpers.BindParameter(doc, setParamName, MepCategories, subTransaction);
        if (!flag) return;
        foreach (var element in elements)
        {
            SetHermeticСlass(element);
        }
    }

    public static void UpdateParamWallThickness(Document doc, List<Element> elements,
        List<DuctParameters> ductParameters)
    {
        const string setParamName = "ADSK_Толщина стенки";

        SubTransaction subTransaction = new SubTransaction(doc);
        var flag = Helpers.BindParameter(doc, setParamName, MepCategories, subTransaction);
        if (!flag) return;
        foreach (var element in elements)
        {
            SetWallThickness(element, ductParameters);
        }
    }

    private static double? GetLargerSideForFamilyInstance(FamilyInstance familyInstance)
    {
        var connector = familyInstance.MEPModel?.ConnectorManager?.Connectors
            .Cast<Connector>()
            .FirstOrDefault();

        if (connector == null) return null;

        if (connector.Shape == ConnectorProfileType.Round)
        {
            return connector.Radius;
        }

        var dimensions = new[]
        {
            familyInstance.FetchParameter("Высота воздуховода 1")?.AsDouble(),
            familyInstance.FetchParameter("Высота воздуховода 2")?.AsDouble(),
            familyInstance.FetchParameter("Ширина воздуховода 1")?.AsDouble(),
            familyInstance.FetchParameter("Ширина воздуховода 2")?.AsDouble()
        };

        return dimensions.Where(d => d.HasValue).Max() ?? 0;
    }

    private static double? GetLargerSideForDuct(Duct duct)
    {
        var connector = duct.ConnectorManager?.Connectors
            .Cast<Connector>()
            .FirstOrDefault();
        if (connector == null) return null;

        if (connector.Shape == ConnectorProfileType.Round)
        {
            return connector.Radius * 2;
        }

        var width = duct.FetchParameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM)?.AsDouble();
        var height = duct.FetchParameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM)?.AsDouble();
        return new[] { width ?? 0, height ?? 0 }.Max();
    }

    public static void SetHermeticСlass(Element element)
    {
        if (element is not Duct && element.Category.BuiltInCategory != BuiltInCategory.OST_DuctFitting) return;

        string internalInsulationValue =
            element?.FetchParameter(BuiltInParameter.RBS_REFERENCE_LINING_TYPE)?.AsValueString();
        Parameter hermeticСlass = element.FetchParameter("Класс герметичности");
        string currentValue = hermeticСlass?.AsString();

        if (internalInsulationValue == null)
        {
            const string newValue = "класс герметичности A";

            if (currentValue != newValue)
            {
                hermeticСlass?.Set(newValue);
            }

            return;
        }

        string formattedValue = char.ToLower(internalInsulationValue[0]) + internalInsulationValue.Substring(1);

        if (currentValue != formattedValue)
        {
            hermeticСlass?.Set(formattedValue);
        }
    }

    public static void ReturnWindowState(Window window)
    {
        if (window == null || window.WindowState == WindowState.Minimized) return;
        window.Activate(); // Делаем окно активным
        window.Topmost = true; // Устанавливаем окно поверх всех остальных
        window.Topmost = false; // Сбрасываем флаг, чтобы окно могло быть отправлено назад при необходимости
    }
}