using UpdatingParameters.Models;

namespace UpdatingParameters.Storages.FlexPipes
{
    public class FlexPipesCorrugationsDataStorage(IDataLoader dataLoader) : DataStorageFormulas(dataLoader)
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
                                       "Патрубок гофрированный для унитаза, L=350 мм"
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
}