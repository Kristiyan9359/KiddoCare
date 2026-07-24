using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Data.Models;

public class Child
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(ChildFirstNameMaxLength)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(ChildLastNameMaxLength)]
    public string LastName { get; set; } = null!;

    [Required]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(ChildAllergiesMaxLength)]
    public string? Allergies { get; set; }

    [MaxLength(ChildAdditionalNotesMaxLength)]
    public string? AdditionalNotes { get; set; }

    [Required]
    [ForeignKey(nameof(Group))]
    public int GroupId { get; set; }

    public KindergartenGroup Group { get; set; } = null!;

    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
}