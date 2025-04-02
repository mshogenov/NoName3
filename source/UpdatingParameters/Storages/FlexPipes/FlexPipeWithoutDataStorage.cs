using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.FlexPipes;

public class FlexPipeWithoutDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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