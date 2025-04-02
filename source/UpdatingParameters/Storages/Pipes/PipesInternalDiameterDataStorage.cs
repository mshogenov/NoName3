using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.Pipes;

public class PipesInternalDiameterDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                    ParameterName="Комментарии к типоразмеру",
                    Significance=element ?.FindParameter("Комментарии к типоразмеру")?.AsValueString() ?? "Труба полипропиленовая PN10"
                },
                new Formula
                {
                    ParameterName="Диаметр",
                    Prefix=" ø",
                    Significance=element ?.FindParameter("Диаметр")?.AsValueString() ?? "110"
                },
                new Formula
                {
                    ParameterName="Толщина стенки",
                    Prefix="x",
                    Significance=element ?.FindParameter("Толщина стенки")?.AsValueString() ?? "10"
                }

            ],
            AdskNoteFormulas = new List<Formula>(),
            AdskQuantityFormulas = new List<Formula>
            {
                new()
                {
                    MeasurementUnit = MeasurementUnit.Meter,
                    ParameterName="Длина",
                    Significance=element ?.FindParameter("Длина")?.AsDouble().ToMeters().ToString(CultureInfo.InvariantCulture) ?? "100",
                    Stockpile="Нет значения"

                }

            }
        };

        DataLoader.SaveData(defaultFormulas);
    }
}