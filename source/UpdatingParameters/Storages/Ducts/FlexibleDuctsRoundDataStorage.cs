using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Ducts;

public class FlexibleDuctsRoundDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                                   "Воздуховод гибкий гофрированный"
                },
                new Formula
                {
                    
                    ParameterName = "Диаметр",
                    Prefix = " ø",
                    Significance = $"{element?.FindParameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM)?.AsValueString()}"
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