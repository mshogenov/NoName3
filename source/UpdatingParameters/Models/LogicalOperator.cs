using System.ComponentModel.DataAnnotations;

namespace UpdatingParameters.Models;

public enum LogicalOperator 
{
    [Display(Name = "И")]
    And,
    [Display(Name = "ИЛИ")]
    Or
}