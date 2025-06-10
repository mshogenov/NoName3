using System.ComponentModel.DataAnnotations;

namespace UpdatingParameters.Models;

public enum Condition
{
    [Display(Name = "равно")] Equally,
    [Display(Name = "не равно")] NotEqually,
    [Display(Name = "содержит")] Contains,
    [Display(Name = "не содержит")] NotContains,
    [Display(Name = "больше")] More,
    [Display(Name = "больше или равно")] MoreOrEqually,
    [Display(Name = "меньше")] Less,
    [Display(Name = "меньше или равно")] LessOrEqually,
    [Display(Name = "начинается с")] StartsWith,
    [Display(Name = "не начинается с")] NotStartsWith,
    [Display(Name = "заканчивается на")] EndsIn,
    [Display(Name = "не заканчивается на")]
    NotEndsIn,
    [Display(Name = "имеет значение")] Matters,
    [Display(Name = "не имеет значение")] NotMatters,
}