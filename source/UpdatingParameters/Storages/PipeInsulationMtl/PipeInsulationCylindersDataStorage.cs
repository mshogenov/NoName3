using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.PipeInsulationMtl;

public class PipeInsulationCylindersDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                                   "Цилиндры минераловатные некашированные"
                },
                new Formula
                {
                    ParameterName = "Толщина изоляции",
                    Prefix = " толщиной ",
                    Significance = element?.FindParameter("Толщина изоляции")?.AsValueString() ?? "16"
                },
                new Formula
                {
                    ParameterName = "Размер трубы",
                    Prefix = " для ",
                    Significance = element?.FindParameter("Размер трубы")?.AsValueString() ?? "ø100"
                }
            ],
            AdskNoteFormulas = [],
            AdskQuantityFormulas =
            [
                new Formula
                {
                    MeasurementUnit = MeasurementUnit.CubicMeter,
                    ParameterName = "Объем",

                    Significance = element?.FindParameter("Объем")?.AsDouble().ToUnit(UnitTypeId.CubicMeters)
                        .ToString(CultureInfo.InvariantCulture) ?? "3 м³",

                    Stockpile = "Нет значения"
                }
            ]
        };
        DataLoader.SaveData(defaultFormulas);
    }
}