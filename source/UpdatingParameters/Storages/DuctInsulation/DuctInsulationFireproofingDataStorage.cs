using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.DuctInsulation;

public class DuctInsulationFireproofingDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                                   "Комбинированное покрытие на основе базальтового рулонного материала, кашированного алюминиевой фольгой, и клеевого состава, EI30, b=5 мм"
                }
            ],
            AdskNoteFormulas = [],
            AdskQuantityFormulas =
            [
                new Formula
                {
                    MeasurementUnit = MeasurementUnit.SquareMeters,
                    ParameterName = "Площадь",
                    Significance =
                        element?.FindParameter(BuiltInParameter.RBS_CURVE_SURFACE_AREA)?.AsDouble()
                            .ToUnit(UnitTypeId.SquareMeters).ToString(CultureInfo.InvariantCulture) ??
                        "1 м²",
                    Stockpile = "Нет значения"
                }
            ]
        };
        DataLoader.SaveData(defaultFormulas);
    }
}