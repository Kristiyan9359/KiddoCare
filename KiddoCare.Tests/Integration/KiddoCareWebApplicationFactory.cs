using KiddoCare.Data;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Tests.Integration;

public class KiddoCareWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            var inMemoryServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName);
                options.UseInternalServiceProvider(inMemoryServiceProvider);
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultForbidScheme = TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme,
                    options => { });
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        context.Roles.AddRange(
            new IdentityRole
            {
                Id = "admin-role-id",
                Name = Admin,
                NormalizedName = Admin.ToUpperInvariant()
            },
            new IdentityRole
            {
                Id = "teacher-role-id",
                Name = Teacher,
                NormalizedName = Teacher.ToUpperInvariant()
            },
            new IdentityRole
            {
                Id = "parent-role-id",
                Name = Parent,
                NormalizedName = Parent.ToUpperInvariant()
            });

        context.Users.AddRange(
            new IdentityUser
            {
                Id = "admin-user-id",
                UserName = "admin@kiddocare.com",
                NormalizedUserName = "ADMIN@KIDDOCARE.COM",
                Email = "admin@kiddocare.com",
                NormalizedEmail = "ADMIN@KIDDOCARE.COM",
                EmailConfirmed = true
            },
            new IdentityUser
            {
                Id = "teacher-user-id",
                UserName = "teacher@kiddocare.com",
                NormalizedUserName = "TEACHER@KIDDOCARE.COM",
                Email = "teacher@kiddocare.com",
                NormalizedEmail = "TEACHER@KIDDOCARE.COM",
                EmailConfirmed = true
            },
            new IdentityUser
            {
                Id = "other-teacher-user-id",
                UserName = "other-teacher@kiddocare.com",
                NormalizedUserName = "OTHER-TEACHER@KIDDOCARE.COM",
                Email = "other-teacher@kiddocare.com",
                NormalizedEmail = "OTHER-TEACHER@KIDDOCARE.COM",
                EmailConfirmed = true
            },
            new IdentityUser
            {
                Id = "parent-user-id",
                UserName = "parent@kiddocare.com",
                NormalizedUserName = "PARENT@KIDDOCARE.COM",
                Email = "parent@kiddocare.com",
                NormalizedEmail = "PARENT@KIDDOCARE.COM",
                EmailConfirmed = true
            },
            new IdentityUser
            {
                Id = "other-parent-user-id",
                UserName = "other-parent@kiddocare.com",
                NormalizedUserName = "OTHER-PARENT@KIDDOCARE.COM",
                Email = "other-parent@kiddocare.com",
                NormalizedEmail = "OTHER-PARENT@KIDDOCARE.COM",
                EmailConfirmed = true
            });

        context.KindergartenGroups.AddRange(
            new KindergartenGroup
            {
                Id = 1,
                Name = "Sunshine"
            },
            new KindergartenGroup
            {
                Id = 2,
                Name = "Moonlight"
            });

        context.TeacherProfiles.AddRange(
            new TeacherProfile
            {
                Id = 1,
                UserId = "teacher-user-id",
                FullName = "Teacher One",
                GroupId = 1
            },
            new TeacherProfile
            {
                Id = 2,
                UserId = "other-teacher-user-id",
                FullName = "Teacher Two",
                GroupId = 2
            });

        context.ParentProfiles.AddRange(
            new ParentProfile
            {
                Id = 1,
                UserId = "parent-user-id",
                FullName = "Parent One"
            },
            new ParentProfile
            {
                Id = 2,
                UserId = "other-parent-user-id",
                FullName = "Parent Two"
            });

        context.Children.AddRange(
            new Child
            {
                Id = 1,
                FirstName = "Ivan",
                LastName = "Ivanov",
                Gender = Gender.Male,
                DateOfBirth = DateTime.Today.AddYears(-4),
                GroupId = 1,
                ParentId = 1
            },
            new Child
            {
                Id = 2,
                FirstName = "Maria",
                LastName = "Petrova",
                Gender = Gender.Female,
                DateOfBirth = DateTime.Today.AddYears(-3),
                GroupId = 2,
                ParentId = 2
            });

        context.Events.AddRange(
            new Event
            {
                Id = 1,
                Title = "Own group event",
                StartDateTime = DateTime.Now.AddDays(1),
                Type = EventType.General,
                GroupId = 1,
                IsPublic = true
            },
            new Event
            {
                Id = 2,
                Title = "Other group event",
                StartDateTime = DateTime.Now.AddDays(2),
                Type = EventType.General,
                GroupId = 2,
                IsPublic = true
            });

        context.DailyReports.AddRange(
            new DailyReport
            {
                Id = 1,
                ChildId = 1,
                ReportDate = DateTime.Today,
                Mood = ChildMood.Happy,
                CreatedByUserId = "teacher-user-id"
            },
            new DailyReport
            {
                Id = 2,
                ChildId = 2,
                ReportDate = DateTime.Today,
                Mood = ChildMood.Calm,
                CreatedByUserId = "other-teacher-user-id"
            });

        context.MedicalRecords.AddRange(
            new MedicalRecord
            {
                Id = 1,
                ChildId = 1,
                Allergies = "Peanuts"
            },
            new MedicalRecord
            {
                Id = 2,
                ChildId = 2,
                Allergies = "None"
            });

        context.ChildDocuments.AddRange(
            new ChildDocument
            {
                Id = 1,
                ChildId = 1,
                Type = ChildDocumentType.MedicalNote,
                Title = "Own group document",
                FileUrl = "/App_Data/uploads/child-documents/own.pdf",
                Status = RequestStatus.Approved,
                UploadedByUserId = "parent-user-id"
            },
            new ChildDocument
            {
                Id = 2,
                ChildId = 2,
                Type = ChildDocumentType.MedicalNote,
                Title = "Other group document",
                FileUrl = "/App_Data/uploads/child-documents/other.pdf",
                Status = RequestStatus.Approved,
                UploadedByUserId = "other-parent-user-id"
            });

        await context.SaveChangesAsync();
    }
}
