using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Pipes;

public class PipesOuterDiameterDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                                   "Труба чугунная SML"
                },
                new Formula
                {
                    ParameterName = "Внешний диаметр",
                    Prefix = " ø",
                    Significance = element?.FindParameter("Внешний диаметр")?.AsValueString() ?? "100"
                },
                new Formula
                {
                    ParameterName = "Толщина стенки",
                    Prefix = "x",
                    Significance = element?.FindParameter("Толщина стенки")?.AsValueString() ?? "3,5"
                }
            ],
            AdskNoteFormulas = [],
            AdskQuantityFormulas =
            [
                new Formula
                {
                    MeasurementUnit = MeasurementUnit.Meter,
                    ParameterName = "Длина",
                    Significance =
                        element?.FindParameter("Длина")?.AsDouble().ToMeters().ToString(CultureInfo.InvariantCulture) ??
                        "1200 мм",
                    Stockpile = "Нет значения"
                }
            ]
        };

        DataLoader.SaveData(defaultFormulas);
    }
}