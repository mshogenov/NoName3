using Autodesk.Revit.DB.Plumbing;

namespace NumberingOfRisers.Models;

public partial class Riser : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private int _number;
    public int CountPipes { get; set; }
    public XYZ Location { get; set; }
    [ObservableProperty] private string _newNumberRiser;
    public MEPSystemType MepSystemType { get; set; }
    public ICollection<ElementId> ElementIds { get; set; } = [];
    public double? TotalLength { get; set; }

    public List<Pipe> Pipes { get; set; } = [];

    public Riser(IGrouping<Element, Pipe> pipesGroups)
    {
        if (!pipesGroups.Any()) return;
        Id = Guid.NewGuid();
        foreach (var pipe in pipesGroups)
        {
            Pipes.Add(pipe);
            ElementIds.Add(pipe.Id);
        }

        if (Pipes == null) return;

        Location = ((LocationCurve)pipesGroups.FirstOrDefault()?.Location)?.Curve.GetEndPoint(0);
        TotalLength =
            Pipes.Sum(x => x.FindParameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble().ToMillimeters());
        MepSystemType = GetPipingSystemType(Pipes.FirstOrDefault());
        CountPipes = pipesGroups.Count();
        Number = GetNumberRiser();
        CountPipes = Pipes.Count;
    }

// Конструктор для ручного добавления стояка
    public Riser(IEnumerable<Pipe> pipes)
    {
        if (pipes == null) return;
        Id = Guid.NewGuid();
        foreach (var pipe in pipes)
        {
            Pipes.Add(pipe);
            ElementIds.Add(pipe.Id);
        }
        if (Pipes.Count <= 0) return;
        Location = ((LocationCurve)Pipes.FirstOrDefault()?.Location)?.Curve.GetEndPoint(0);
        TotalLength = Pipes.Sum(x => x.FindParameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble().ToMillimeters());
        MepSystemType = GetPipingSystemType(Pipes.FirstOrDefault());
        CountPipes = Pipes.Count;
        Number = GetNumberRiser();
    }

    /// <summary>
    /// Получает номер стояка
    /// </summary>
    /// <returns></returns>
    public int GetNumberRiser()
    {
        if (Pipes.Count <= 0) return 0;

        // Группировка с отфильтрованными null значениями
        var groups = Pipes
            .Where(p => p.FindParameter("ADSK_Номер стояка") != null &&
                        !string.IsNullOrEmpty(p.FindParameter("ADSK_Номер стояка")?.AsValueString()))
            .GroupBy(p => p.FindParameter("ADSK_Номер стояка")?.AsValueString())
            .ToList();

        if (groups.Count == 0)
            return 0;

        // Найдем группу с максимальным числовым значением ключа
        string maxKey = groups
            .Select(g => g.Key)
            .OrderByDescending(k =>
            {
                int.TryParse(k, out int value);
                return value;
            })
            .First();

        // Обновляем свойство Number
        Number = int.TryParse(maxKey, out int result) ? result : 0;

        // Возвращаем новое значение
        return Number;
    }

    private MEPSystemType GetPipingSystemType(Pipe pipe)
    {
        // Получаем MEPSystem трубы
        MEPSystem mepSystem = pipe.MEPSystem;

        if (mepSystem != null)
        {
            // Получаем SystemType из MEPSystem
            ElementId systemTypeId = mepSystem.GetTypeId();

            if (systemTypeId != ElementId.InvalidElementId)
            {
                // Получаем MEPSystemType по его Id
                return pipe.Document.GetElement(systemTypeId) as MEPSystemType;
            }
        }

        // Если система не определена, можно попробовать получить тип напрямую из параметра трубы
        Parameter systemTypeParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
        if (systemTypeParam != null && systemTypeParam.HasValue)
        {
            ElementId typeId = systemTypeParam.AsElementId();
            if (typeId != ElementId.InvalidElementId)
            {
                return pipe.Document.GetElement(typeId) as MEPSystemType;
            }
        }

        // Если не удалось определить тип системы
        return null;
    }
}