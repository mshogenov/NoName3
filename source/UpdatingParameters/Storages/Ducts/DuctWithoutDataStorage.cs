using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Ducts;

public class DuctWithoutDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
{
    public override void InitializeDefault()
    {
        var defaultFormulas = new CategoryFormulas
        {
            AdskNameFormulas = [],
            AdskNoteFormulas = [],
            AdskQuantityFormulas = []
        };

        DataLoader.SaveData(defaultFormulas);
    }
}