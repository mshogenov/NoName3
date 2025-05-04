namespace CopyAnnotations.Models;

public class TextNoteModel
{
    public ElementId Id { get; set; }
    public TextNote TextNote { get; set; }
    public int LiederCount { get; set; }
    public List<LeaderModel> LeaderModels { get; set; } = [];
    public TextNoteModel(TextNote textNote )
    {
        if (textNote == null) return;
        TextNote = textNote;
        Id = textNote.Id;
        LiederCount = textNote.LeaderCount;
        foreach (var leader in textNote.GetLeaders())
        {
            LeaderModels.Add(new LeaderModel(leader));
        }
    }
}