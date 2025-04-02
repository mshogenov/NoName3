using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Ducts;

public class DuctPlasticDataStorage(IDataLoader dataLoader):DataStorageFormulas(dataLoader)
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
                                   "Воздуховод пластиковый для плоских каналов"
                },
                new Formula
                {
                    ParameterName = "Размер",
                    Significance = element?.FindParameter(BuiltInParameter.RBS_CALCULATED_SIZE)?.AsValueString() ??
                                   "200x300"
                },
            ],
            AdskNoteFormulas = [],
            AdskQuantityFormulas =
            [
                new Formula
                {
                    MeasurementUnit = MeasurementUnit.Meter,
                    ParameterName = "Длина",
                    Significance =
                        element?.FindParameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsDouble()
                            .ToUnit(UnitTypeId.Meters).ToString(CultureInfo.InvariantCulture) ??
                        "1 м",
                    Stockpile = "Нет значения"
                }
            ]
        };
        DataLoader.SaveData(defaultFormulas); 
    }
}