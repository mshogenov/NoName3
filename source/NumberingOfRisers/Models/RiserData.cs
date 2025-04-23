namespace NumberingOfRisers.Models;

/// <summary>
/// Класс для сериализации данных стояка
/// </summary>
[Serializable]
public class RiserData
{
   public int Number { get; set; }
    public List<long> ElementIds { get; set; } = [];
    public bool Ignored { get; set; }
}