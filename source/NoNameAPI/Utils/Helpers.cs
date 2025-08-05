using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions;
using Nice3point.Revit.Toolkit;
using NoNameAPI.Filters;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace NoNameApi.Utils;

public static class Helpers
{
   private const string _groupName = "RevitAddIn";
    /// <summary>
    /// Получает все стояки в документе
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<IGrouping<Element, Pipe>> GetAllRisers(Document doc, int risersCount,
        double pipeLength)
    {
        // Создаем коллекционер для поиска всех труб в документе
        FilteredElementCollector collector = new(doc);
        collector.OfClass(typeof(Pipe));
        // Отделение от всех труб стояков
        var standpipesWithoutSlope = collector
            .WhereElementIsNotElementType()
            .Cast<Pipe>()
            .Where(p => p.LookupParameter("Уклон").AsValueString() == null);
        var slopedRisers = collector
            .WhereElementIsNotElementType()
            .Cast<Pipe>()
            .Where(p => p.LookupParameter("Уклон").AsDouble() > 360);

        //Список всех стояков
        var verticalPipes = standpipesWithoutSlope.Concat(slopedRisers).ToList()
            .GroupBy(p => p, new PipeIEqualityComparer());
        //Отделение групп вертикальных труб по сегментам и длине труб
        var riserGroups = verticalPipes.Where(x => x.Count() > risersCount);
        return riserGroups.Where(p =>
            p.Count(y => y.FindParameter("Длина")?.AsDouble().ToMillimeters() > pipeLength) > risersCount);
    }

    /// <summary>Возвращает ближайший к точке коннектор</summary>
    /// <param name="mepCurve"><see cref="T:Autodesk.Revit.DB.MEPCurve" /></param>
    /// <param name="point">Точка</param>
    public static Connector GetNearestConnector(this MEPCurve mepCurve, XYZ point)
    {
        Connector nearestConnector = null;
        double d = double.NaN;
        foreach (Connector connector in mepCurve.ConnectorManager.Connectors.OfType<Connector>())
        {
            double num = point.DistanceTo(connector.Origin);
            if (double.IsNaN(d) || num < d)
            {
                d = num;
                nearestConnector = connector;
            }
        }

        return nearestConnector;
    }

    /// <summary>
    /// Создаёт общий параметр и привязывает его к указанным категориям в Revit.
    /// </summary>
    /// <param name="doc">Документ Revit.</param>
    /// <param name="parameterName">Имя создаваемого параметра.</param>
    /// <param name="parameterTypeId">Тип параметра, задается через SpecTypeId</param>
    /// <param name="parameterGroup">Группировка параметра, задается через GroupTypeId</param>
    /// <param name="isInstance">True для экземплярного параметра, False для типового.</param>
    /// <param name="categories">Список категорий, к которым привязывается параметр.</param>
    /// <returns>True, если параметр успешно создан или уже существует; False в случае ошибки.</returns>
    public static bool CreateSharedParameter(Document doc, string parameterName, ForgeTypeId parameterTypeId,
        ForgeTypeId parameterGroup, bool isInstance,
        List<BuiltInCategory> categories)
    {
       
        // Проверка входных данных
        if (doc == null || string.IsNullOrEmpty(parameterName))
            return false;
        // Загрузка файла общих параметров
        DefinitionFile defFile;
        try
        {
            defFile = GetOrCreateSharedParameterFile(doc);
            if (defFile == null)
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Не удалось открыть файл общих параметров.", ex);
        }

        // Получение или создание группы параметров
        DefinitionGroup group = defFile.Groups.get_Item(_groupName) ?? defFile.Groups.Create(_groupName);

        // Проверка, существует ли параметр
        Definition definition = group.Definitions.get_Item(parameterName);
        if (definition == null)
        {
            // Создание нового определения параметра
            ExternalDefinitionCreationOptions options =
                new ExternalDefinitionCreationOptions(parameterName, parameterTypeId);

            definition = group.Definitions.Create(options);
        }

        // Создание набора категорий
        CategorySet categorySet = new CategorySet();

        if (CheckParameterExists(doc, parameterName))
        {
            var unboundCategories = GetUnboundCategories(doc, parameterName, categories);
            if (unboundCategories.Count <= 0) return true;
            foreach (var unboundCategory in unboundCategories)
            {
                Category category = doc.Settings.Categories.get_Item(unboundCategory);
                if (category != null)
                    categorySet.Insert(category);
            }

            return true;
        }

        foreach (var cat in categories)
        {
            Category category = doc.Settings.Categories.get_Item(cat);
            if (category != null)
                categorySet.Insert(category);
        }

        // Создание привязки (экземплярная или типовая)
        Binding binding = isInstance ? new InstanceBinding(categorySet) : new TypeBinding(categorySet);

        try
        {
            doc.ParameterBindings.Insert(definition, binding, parameterGroup);
        }
        catch (Exception ex)
        {
            throw new Exception("Не удалось привязать параметр.", ex);
        }

        return true;
    }


     /// <summary>
    /// Возвращает DefinitionFile.  Если файл отсутствует, спрашивает пользователя, можно ли его создать.
    /// При отказе возвращает null.
    /// </summary>
    public static DefinitionFile GetOrCreateSharedParameterFile(Document doc)
    {
        Application app = doc.Application;
        string spPath = app.SharedParametersFilename;

        bool needCreate = string.IsNullOrEmpty(spPath) || !File.Exists(spPath);

        // ─── 1. Файл отсутствует: спрашиваем пользователя ──────────────────────────
        if (needCreate)
        {
            TaskDialog dlg = new TaskDialog("Файл общих параметров");
            dlg.MainInstruction = "Файл общих параметров не найден.";
            dlg.MainContent     = "Создать новый файл общих параметров?";
            dlg.CommonButtons   = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
            dlg.DefaultButton   = TaskDialogResult.Yes;

            if (dlg.Show() != TaskDialogResult.Yes)
                return null;                        // пользователь отказался
        }

        // ─── 2. Создаём файл при необходимости ──────────────────────────────────────
        if (needCreate)
        {
            spPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RevitAddIn2.txt");

            Directory.CreateDirectory(Path.GetDirectoryName(spPath)!);

            if (!File.Exists(spPath))
            {
                string[] header =
                {
                    "# This is a Revit shared parameter file.",
                    "# Do not edit manually.",
                    "*META\tVERSION\tMINVERSION",
                    "META\t2\t1",
                    "*GROUP\tID\tNAME",
                    "*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE"
                };
                File.WriteAllLines(spPath, header, new UTF8Encoding(true)); // UTF-8 + BOM
            }

            app.SharedParametersFilename = spPath;
        }

        // ─── 3. Открываем файл ───────────────────────────────────────────────────────
        DefinitionFile defFile = app.OpenSharedParameterFile();
        if (defFile == null)
            throw new InvalidOperationException(
                $"Не удалось открыть файл общих параметров: {spPath}");

        return defFile;
    }


    /// <summary>
    /// Получает список категорий, к которым параметр не привязан
    /// </summary>
    private static List<BuiltInCategory> GetUnboundCategories(Document doc, string parameterName,
        List<BuiltInCategory> mepCategories)
    {
        List<BuiltInCategory> unboundCategories = new List<BuiltInCategory>();

        foreach (BuiltInCategory builtInCategory in mepCategories)
        {
            // Проверяем, НЕ привязан ли параметр к категории
            if (!IsParameterBoundToCategory(doc, parameterName, builtInCategory))
            {
                unboundCategories.Add(builtInCategory);
            }
        }

        return unboundCategories;
    }

    /// <summary>
    /// Проверяет, привязан ли параметр к категории
    /// </summary>
    private static bool IsParameterBoundToCategory(Document doc, string parameterName, BuiltInCategory builtInCategory)
    {
        // Преобразуем BuiltInCategory в ElementId
        ElementId categoryId = new ElementId((long)builtInCategory);

        // Получаем категорию по ID
        Category category = Category.GetCategory(doc, categoryId);
        if (category is not { AllowsBoundParameters: true })
            return false;

        // Получаем карту привязок параметров
        BindingMap bindingMap = doc.ParameterBindings;

        // Итерируем по привязкам
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
        while (iterator.MoveNext())
        {
            Definition definition = iterator.Key;
            if (definition == null || definition.Name != parameterName) continue;
            // Получаем привязку параметра
            if (iterator.Current is Binding binding)
            {
                switch (binding)
                {
                    // Проверяем тип привязки (InstanceBinding или TypeBinding)
                    case InstanceBinding instanceBinding:
                        return instanceBinding.Categories.Contains(category);
                    case TypeBinding typeBinding:
                        return typeBinding.Categories.Contains(category);
                }
            }
        }

        // Если параметр не найден
        return false;
    }

    /// <summary>
    /// Подключает параметр к категориям
    /// </summary>
    /// 
    public static bool BindParameter(Document doc, string parameterName, List<BuiltInCategory> mepCategories,
        SubTransaction sT)
    {
        try
        {
            // Получаем только НЕпривязанные категории
            var unboundCategories = GetUnboundCategories(doc, parameterName, mepCategories);
            if (unboundCategories.Count <= 0) return true; // Все категории уже привязаны

            // Создаем набор категорий для привязки
            CategorySet categorySet = new CategorySet();

            // Заполняем набор только неподключенными категориями
            foreach (var unboundCategory in unboundCategories)
            {
                Category category = Category.GetCategory(doc, new ElementId((int)unboundCategory));
                if (category != null && category.AllowsBoundParameters)
                    categorySet.Insert(category);
            }

            // Проверяем, что набор категорий не пустой
            if (categorySet.IsEmpty)
            {
                return true; // Все категории, которые могут быть привязаны, уже привязаны
            }

            // Находим существующий параметр
            BindingMap bindingMap = doc.ParameterBindings;
            Definition paramDef = null;
            Binding existingBinding = null;

            // Ищем определение параметра
            DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
            while (iterator.MoveNext())
            {
                Definition def = iterator.Key;
                if (def != null && def.Name == parameterName)
                {
                    paramDef = def;
                    existingBinding = (Binding)iterator.Current;
                    break;
                }
            }

            sT.Start();

            // Если параметр существует, добавляем новые категории к существующей привязке
            if (paramDef != null && existingBinding != null)
            {
                // Получаем текущие привязанные категории
                CategorySet existingCategories;
                if (existingBinding is InstanceBinding instanceBinding)
                {
                    existingCategories = instanceBinding.Categories;
                }
                else if (existingBinding is TypeBinding typeBinding)
                {
                    existingCategories = typeBinding.Categories;
                }
                else
                {
                    sT.RollBack();
                    return false;
                }

                // Добавляем новые категории к существующим
                foreach (Category category in categorySet)
                {
                    if (!existingCategories.Contains(category))
                    {
                        existingCategories.Insert(category);
                    }
                }

                // Создаем новую привязку с обновленным набором категорий
                Binding newBinding;
                if (existingBinding is InstanceBinding)
                {
                    newBinding = new InstanceBinding(existingCategories);
                }
                else
                {
                    newBinding = new TypeBinding(existingCategories);
                }

                // Обновляем привязку
                bool result = bindingMap.ReInsert(paramDef, newBinding);
                sT.Commit();
                return result;
            }
            else
            {
                // Если параметр не существует, нужно его создать
                // [Код для создания нового параметра]

                sT.RollBack();
                TaskDialog.Show("Ошибка", "Параметр не найден в документе. Необходимо сначала создать общий параметр.");
                return false;
            }
        }
        catch (Exception ex)
        {
            if (sT.HasStarted())
                sT.RollBack();
            TaskDialog.Show("Ошибка", $"Не удалось привязать параметр: {ex.Message}");
            return false;
        }
    }

    public static bool CheckParameterExists(Document doc, string parameterName)
    {
        if (doc == null || string.IsNullOrEmpty(parameterName))
            return false;

        BindingMap bindingMap = doc.ParameterBindings;
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();

        while (iterator.MoveNext())
        {
            Definition definition = iterator.Key;
            if (definition?.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Получает определение параметра
    /// </summary>
    private static Definition GetParameterDefinition(Document doc, string parameterName)
    {
        BindingMap bindingMap = doc.ParameterBindings;
        DefinitionBindingMapIterator iterator = bindingMap.ForwardIterator();
        iterator.Reset();
        while (iterator.MoveNext())
        {
            Definition definition = iterator.Key;
            if (definition.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

// Вспомогательный метод для проверки файла общих параметров

    public static bool FindParameterInFile(DefinitionFile defFile, string parameterName)
    {
        if (defFile == null) return false;
        foreach (DefinitionGroup group in defFile.Groups)
        {
            if (IsParameterInGroup(group, parameterName))
                return true;
        }

        return false;
    }

    private static bool IsParameterInGroup(DefinitionGroup group, string parameterName)
    {
        if (group == null || string.IsNullOrEmpty(parameterName))
            return false;

        Definition definition = group.Definitions.get_Item(parameterName);
        return definition != null;
    }

    // Вспомогательный метод для проверки существования группы

    private static (DefinitionGroup group, bool wasCreated) GetOrCreateParameterGroupDetailed(Application app,
        string groupName)
    {
        DefinitionFile defFile = app.OpenSharedParameterFile();
        if (defFile == null)
        {
            throw new Exception("Shared parameter file not found");
        }

        bool created = false;
        DefinitionGroup group = defFile.Groups.get_Item(groupName);

        if (group == null)
        {
            group = defFile.Groups.Create(groupName);
            created = true;
        }

        return (group, created);
    }

    /// <summary>
    /// Получает список выделенных элементов в активном документе
    /// </summary>
    /// <returns>Список выделенных элементов</returns>
    public static List<Element> GetSelectedElements(UIDocument uiDoc)
    {
        Document doc = uiDoc.Document;
        // Получаем IDs выделенных элементов
        ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

        // Преобразуем IDs в элементы
        List<Element> selectedElements = [];
        foreach (ElementId id in selectedIds)
        {
            Element elem = doc.GetElement(id);
            if (elem != null)
            {
                selectedElements.Add(elem);
            }
        }

        return selectedElements;
    }
}