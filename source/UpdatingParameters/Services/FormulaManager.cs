using System.Collections.ObjectModel;
using System.Globalization;
using UpdatingParameters.Models;

namespace UpdatingParameters.Services;

public class FormulaManager
{
    private ObservableCollection<Formula> Formulas { get; set; }
    private ObservableCollection<Parameter> AvailableParameters { get; set; }
    private ObservableCollection<string> AvailableParametersQuantity { get; set; }

    private Action SaveFormulas { get; set; }

    public FormulaManager(ObservableCollection<Formula> formulas, ObservableCollection<Parameter> availableParameters,
        Action saveFormulas)
    {
        Formulas = formulas;
        AvailableParameters = availableParameters;

        SaveFormulas = saveFormulas;
    }

    public FormulaManager(ObservableCollection<Formula> formulas,
        ObservableCollection<string> availableParametersQuantity, Action saveFormulas)
    {
        Formulas = formulas;
        AvailableParametersQuantity = availableParametersQuantity;

        SaveFormulas = saveFormulas;
    }

    public void AddParameter(Parameter selectParameter)
    {
        if (selectParameter == null || Formulas.Any(f => f.ParameterName == selectParameter.Definition.Name)) return;
        Formulas.Add(new Formula
        {
            ParameterName = selectParameter.Definition.Name,
            Significance = selectParameter.AsValueString()
        });

        AvailableParameters.Remove(selectParameter);
        SaveFormulas();
    }

    public void AddParameterQuantity(string selectParameter, Element element)
    {
        if (selectParameter == null || Formulas.Any(f => f.ParameterName == selectParameter)) return;
        Parameter parameter = element.FindParameter(selectParameter);
        MeasurementUnit unit = selectParameter switch
        {
            "Объем" => MeasurementUnit.CubicMeter,
            "Площадь" => MeasurementUnit.SquareMeters,
            "Число" => MeasurementUnit.Piece,
            _ => MeasurementUnit.Millimeter,
        };
        string value = unit switch
        {
            MeasurementUnit.Meter => parameter != null
                ? Math.Round(parameter.AsDouble().ToMeters(), 2).ToString(CultureInfo.InvariantCulture)
                : "1200",
            MeasurementUnit.Millimeter => parameter != null
                ? parameter.AsDouble().ToMillimeters().ToString("F1", CultureInfo.InvariantCulture)
                : "1200",

            MeasurementUnit.CubicMeter => parameter != null
                ? parameter.AsDouble().ToUnit(UnitTypeId.CubicMeters).ToString("F3", CultureInfo.InvariantCulture)
                : "1200",
            MeasurementUnit.SquareMeters => parameter != null
                ? parameter.AsDouble().ToUnit(UnitTypeId.SquareMeters).ToString(CultureInfo.InvariantCulture)
                : "1200",
            MeasurementUnit.Piece => "1",
            _ => ""
        };
        Formulas.Add(new Formula
        {
            ParameterName = selectParameter,
            MeasurementUnit = unit,
            Significance = value,
            Stockpile = "Нет значения"
        });

        AvailableParametersQuantity.Remove(selectParameter);
        SaveFormulas();
    }

    public void RemoveParameter(Formula selectFormula, Element element)
    {
        if (selectFormula == null) return;
        Formulas.Remove(selectFormula);

        AvailableParameters.Add(element.FindParameter(selectFormula.ParameterName));
        SaveFormulas();
    }

    public void RemoveParameterQuantity(Formula selectFormula)
    {
        if (selectFormula == null) return;
        Formulas.Remove(selectFormula);


        AvailableParametersQuantity.Add(selectFormula.ParameterName);


        SaveFormulas();
    }

    public void MoveUp(Formula selectFormula, Action onMoveSuccess)
    {
        if (selectFormula == null) return;
        int currentIndex = Formulas.IndexOf(selectFormula);
        if (currentIndex > 0)
        {
            Formulas.Move(currentIndex, currentIndex - 1);

            onMoveSuccess?.Invoke();
        }

        SaveFormulas();
    }

    public void MoveDown(Formula selectFormula, Action onMoveSuccess)
    {
        if (selectFormula == null) return;
        int currentIndex = Formulas.IndexOf(selectFormula);
        if (currentIndex < Formulas.Count - 1)
        {
            Formulas.Move(currentIndex, currentIndex + 1);

            onMoveSuccess?.Invoke();
        }

        SaveFormulas();
    }
}