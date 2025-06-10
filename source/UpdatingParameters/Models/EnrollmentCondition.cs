using System.ComponentModel.DataAnnotations;

namespace UpdatingParameters.Models;

public enum EnrollmentCondition 
{
    [Display(Name = "И")]
    And,
    [Display(Name = "ИЛИ")]
    Or
}