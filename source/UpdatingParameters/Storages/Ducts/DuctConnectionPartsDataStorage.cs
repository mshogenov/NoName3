using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Ducts;

public class DuctConnectionPartsDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                    Significance = element?.FindParameter(BuiltInParameter.ALL_MODEL_TYPE_COMMENTS)?.AsValueString() ??
                                   "Отвод прямоугольного воздуховода"
                },
                new Formula
                {
                    Prefix = " ",
                    ParameterName = "ADSK_Размер_УголПоворота",
                    Significance = element?.FindParameter("ADSK_Размер_УголПоворота")?.AsValueString() ??
                                   "90°"
                },
                new Formula
                {
                    Prefix = " ",
                    ParameterName = "Размер",
                    Significance = string.Join("-",
                        element?.FindParameter(BuiltInParameter.RBS_CALCULATED_SIZE)?.AsValueString()?.Split('-')
                            .Distinct() ??
                        ["300х200"])
                },
                new Formula
                {
                    ParameterName = "ADSK_Толщина стенки",
                    Prefix = ", b=",
                    Significance = element?.FindParameter("ADSK_Толщина стенки")?.AsValueString() ??
                                   "0.9"
                },
                new Formula
                {
                    ParameterName = "Класс герметичности",
                    Prefix = ", ",
                    Significance = element?.FindParameter("Класс герметичности")?.AsValueString() ??
                                   "B"
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