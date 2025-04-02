using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.FlexPipes;

public class FlexPipesConnectionsDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
{
    public override void InitializeDefault()
    {
        Element element = GetElement();
        var defaultFormulas = new CategoryFormulas
        {
            NameIsChecked = true,
            NoteIsChecked = false,
            QuantityIsChecked = true,
            AdskNameFormulas =
            [
                new Formula
                {
                    ParameterName = "Комментарии к типоразмеру",
                    Significance = element?.FindParameter("Комментарии к типоразмеру")?.AsValueString() ??
                                   "Гибкая подводка для воды, тип гайка-гайка "
                },
                new Formula
                {
                    ParameterName = "Диаметр",
                    Prefix = "ø",
                    Significance = $"{element?.FindParameter("Диаметр")?.AsValueString()}"
                },
            ],
            AdskNoteFormulas = [],
            AdskQuantityFormulas =
            [
                new Formula
                {
                    MeasurementUnit = MeasurementUnit.Piece,
                    ParameterName = "Число",
                    Significance = "1",
                    Stockpile = "Нет значения"
                }
            ]
        };

        DataLoader.SaveData(defaultFormulas);
    }
}