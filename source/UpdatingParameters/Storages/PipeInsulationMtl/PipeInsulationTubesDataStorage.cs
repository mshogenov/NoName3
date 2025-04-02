using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.PipeInsulationMtl
{
    public class PipeInsulationTubesDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                   Significance=element ?.FindParameter("Комментарии к типоразмеру")?.AsValueString() ?? "Трубки теплоизоляционные"
               },
            new Formula
            {
                ParameterName="Толщина изоляции",
                Prefix=" толщиной ",
                Significance=element ?.FindParameter("Толщина изоляции")?.AsValueString() ?? "16"
            },
            new Formula
            {
                ParameterName="Размер трубы",
                Prefix=" для ",
                Significance=element ?.FindParameter("Размер трубы")?.AsValueString() ?? "ø100" }
            ],
                AdskNoteFormulas = [],
                AdskQuantityFormulas =
                [
                     new Formula
                     {
                          MeasurementUnit = MeasurementUnit.Meter,
                        ParameterName="Длина",
                         Significance=element ?.FindParameter("Длина")?.AsDouble().ToMeters().ToString(CultureInfo.InvariantCulture) ??"1200 мм",
                          Stockpile="Нет значения"
                 }
                ]
            };
            DataLoader.SaveData(defaultFormulas);
        }


    }
}
