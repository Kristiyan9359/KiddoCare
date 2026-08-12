using System.ComponentModel.DataAnnotations;
using KiddoCare.Common.ValidationAttributes;
using KiddoCare.Data.Models.Enums;
using KiddoCare.ViewModels.ChildDocuments;
using KiddoCare.ViewModels.Children;
using KiddoCare.ViewModels.ConsentRequests;
using KiddoCare.ViewModels.DailyReports;
using KiddoCare.ViewModels.Parents;
using KiddoCare.ViewModels.Teachers;
using static KiddoCare.Common.ValidationConstants;

namespace KiddoCare.Tests;

public class ViewModelValidationTests
{
    [Fact]
    public void ChildBirthDateAttribute_ShouldAcceptDateWithinAllowedRange()
    {
        var attribute = new ChildBirthDateAttribute();

        var result = attribute.IsValid(DateTime.Today.AddYears(-4));

        Assert.True(result);
    }

    [Fact]
    public void ChildBirthDateAttribute_ShouldRejectFutureDate()
    {
        var attribute = new ChildBirthDateAttribute();

        var result = attribute.IsValid(DateTime.Today.AddDays(1));

        Assert.False(result);
    }

    [Fact]
    public void ChildBirthDateAttribute_ShouldRejectDateOlderThanAllowedRange()
    {
        var attribute = new ChildBirthDateAttribute();

        var result = attribute.IsValid(DateTime.Today.AddYears(-8));

        Assert.False(result);
    }

    [Fact]
    public void ChildCreateViewModel_ShouldRequireNamesGenderDateAndGroup()
    {
        var model = new ChildCreateViewModel
        {
            FirstName = null!,
            LastName = null!,
            Gender = Gender.Male,
            DateOfBirth = DateTime.Today.AddYears(-4),
            GroupId = 0
        };

        var errors = Validate(model);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ChildCreateViewModel.FirstName)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ChildCreateViewModel.LastName)));
    }

    [Fact]
    public void ChildCreateViewModel_ShouldValidateBirthDate()
    {
        var model = new ChildCreateViewModel
        {
            FirstName = "Ivan",
            LastName = "Ivanov",
            Gender = Gender.Male,
            DateOfBirth = DateTime.Today.AddYears(-8),
            GroupId = 1
        };

        var errors = Validate(model);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ChildCreateViewModel.DateOfBirth)));
    }

    [Fact]
    public void ParentCreateViewModel_ShouldValidateEmailFormat()
    {
        var model = new ParentCreateViewModel
        {
            Email = "not-an-email",
            FullName = "Parent One"
        };

        var errors = Validate(model);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ParentCreateViewModel.Email)));
    }

    [Fact]
    public void TeacherCreateViewModel_ShouldValidateMaxLength()
    {
        var model = new TeacherCreateViewModel
        {
            Email = "teacher@kiddocare.com",
            FullName = new string('a', TeacherFullNameMaxLength + 1),
            GroupId = 1
        };

        var errors = Validate(model);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(TeacherCreateViewModel.FullName)));
    }

    [Fact]
    public void DailyReportCreateViewModel_ShouldRejectUnknownMood()
    {
        var model = new DailyReportCreateViewModel
        {
            ChildId = 1,
            ReportDate = DateTime.Today,
            Mood = ChildMood.Unknown,
            MealRating = 4,
            SleepRating = 3,
            ActivityRating = 5
        };

        var errors = Validate(model);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(DailyReportCreateViewModel.Mood)));
    }

    [Fact]
    public void ConsentRequestCreateViewModel_ShouldRequireChildAndTitle()
    {
        var model = new ConsentRequestCreateViewModel
        {
            ChildId = null,
            Title = null!,
            Type = ConsentRequestType.PhotoPermission
        };

        var errors = Validate(model);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ConsentRequestCreateViewModel.ChildId)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ConsentRequestCreateViewModel.Title)));
    }

    [Fact]
    public void ChildDocumentCreateViewModel_ShouldRequireFile()
    {
        var model = new ChildDocumentCreateViewModel
        {
            ChildId = 1,
            Type = ChildDocumentType.MedicalNote,
            Title = "Medical note",
            File = null!
        };

        var errors = Validate(model);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(ChildDocumentCreateViewModel.File)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        return results;
    }
}
