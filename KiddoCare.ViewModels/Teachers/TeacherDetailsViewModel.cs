namespace KiddoCare.ViewModels.Teachers;

public class TeacherDetailsViewModel
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string GroupName { get; set; } = null!;
}