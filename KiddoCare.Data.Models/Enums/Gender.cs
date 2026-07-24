using System.ComponentModel.DataAnnotations;

namespace KiddoCare.Data.Models.Enums;

public enum Gender
{
    [Display(Name = "Male")]
    Male = 1,

    [Display(Name = "Female")]
    Female = 2
}