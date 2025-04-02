namespace NumberingOfRisers.Models;

/// <summary>
/// Класс для сериализации данных системы стояков
/// </summary>
[Serializable]
public class RiserSystemTypeData
{
    public string MepSystemTypeName { get; set; }
    public List<RiserData> Risers { get; set; } = new List<RiserData>();
}