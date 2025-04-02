namespace UpdatingParameters.Models
{

    public class CategoryFormulas
    {
        public IEnumerable<Formula> AdskNameFormulas { get; set; } = new List<Formula>();
        public IEnumerable<Formula> AdskNoteFormulas { get; set; } = new List<Formula>();
        public IEnumerable<Formula> AdskQuantityFormulas { get; set; } = new List<Formula>();
        public bool NameIsChecked { get; set; }
        public bool NoteIsChecked { get; set; }
        public bool QuantityIsChecked { get; set; }
     

    }
}
