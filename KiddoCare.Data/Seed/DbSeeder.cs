using KiddoCare.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KiddoCare.Data.Models;
using KiddoCare.Data.Models.Enums;

namespace KiddoCare.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles =
        [
            RoleConstants.Admin,
            RoleConstants.Teacher,
            RoleConstants.Parent
        ];

        foreach (var role in roles)
        {
            bool roleExists = await roleManager.RoleExistsAsync(role);

            if (!roleExists)
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        const string adminEmail = "admin@kiddocare.com";
        var adminPassword = configuration[UserPasswordConfigurationKeys.AdminPassword];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                $"Admin password is not configured. Set '{UserPasswordConfigurationKeys.AdminPassword}' in user secrets.");
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Admin seed failed: {errors}");
            }
        }

        bool isAdmin = await userManager.IsInRoleAsync(adminUser, RoleConstants.Admin);

        if (!isAdmin)
        {
            await userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
        }
    }

    public static async Task SeedDemoDataAsync(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        if (!configuration.GetValue<bool>("SeedData:SeedDemoData"))
        {
            return;
        }

        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var defaultParentPassword = configuration[UserPasswordConfigurationKeys.DefaultParentPassword];
        var defaultTeacherPassword = configuration[UserPasswordConfigurationKeys.DefaultTeacherPassword];

        if (string.IsNullOrWhiteSpace(defaultParentPassword))
        {
            throw new InvalidOperationException(
                $"Default parent password is not configured. Set '{UserPasswordConfigurationKeys.DefaultParentPassword}' in user secrets.");
        }

        if (string.IsNullOrWhiteSpace(defaultTeacherPassword))
        {
            throw new InvalidOperationException(
                $"Default teacher password is not configured. Set '{UserPasswordConfigurationKeys.DefaultTeacherPassword}' in user secrets.");
        }

        var teacherSunshineUser = await EnsureUserAsync(userManager, "teacher.sunshine@kiddocare.com", defaultTeacherPassword, RoleConstants.Teacher);
        var teacherMoonlightUser = await EnsureUserAsync(userManager, "teacher.moonlight@kiddocare.com", defaultTeacherPassword, RoleConstants.Teacher);
        var teacherStarsUser = await EnsureUserAsync(userManager, "teacher.stars@kiddocare.com", defaultTeacherPassword, RoleConstants.Teacher);

        var parentIvanoviUser = await EnsureUserAsync(userManager, "parent.ivanovi@kiddocare.com", defaultParentPassword, RoleConstants.Parent);
        var parentGeorgieviUser = await EnsureUserAsync(userManager, "parent.georgievi@kiddocare.com", defaultParentPassword, RoleConstants.Parent);
        var parentPetroviUser = await EnsureUserAsync(userManager, "parent.petrovi@kiddocare.com", defaultParentPassword, RoleConstants.Parent);
        var parentDimitroviUser = await EnsureUserAsync(userManager, "parent.dimitrovi@kiddocare.com", defaultParentPassword, RoleConstants.Parent);
        var parentStoyanoviUser = await EnsureUserAsync(userManager, "parent.stoyanovi@kiddocare.com", defaultParentPassword, RoleConstants.Parent);
        var parentNikoloviUser = await EnsureUserAsync(userManager, "parent.nikolovi@kiddocare.com", defaultParentPassword, RoleConstants.Parent);
        var parentAngeloviUser = await EnsureUserAsync(userManager, "parent.angelovi@kiddocare.com", defaultParentPassword, RoleConstants.Parent);

        var sunshineGroup = await EnsureGroupAsync(context, "Sunshine", "Preschool group focused on social skills, early literacy and outdoor play.");
        var moonlightGroup = await EnsureGroupAsync(context, "Moonlight", "Calm kindergarten group with creative activities, music and sensory play.");
        var starsGroup = await EnsureGroupAsync(context, "Little Stars", "Young children group with gentle routines and daily care tracking.");

        var teacherSunshine = await EnsureTeacherProfileAsync(context, teacherSunshineUser.Id, "Emma Petrova", "+359 888 100 101", sunshineGroup.Id);
        var teacherMoonlight = await EnsureTeacherProfileAsync(context, teacherMoonlightUser.Id, "Nikolay Dimitrov", "+359 888 100 102", moonlightGroup.Id);
        var teacherStars = await EnsureTeacherProfileAsync(context, teacherStarsUser.Id, "Gergana Stoyanova", "+359 888 100 103", starsGroup.Id);

        var parentIvanovi = await EnsureParentProfileAsync(context, parentIvanoviUser.Id, "Ivan Ivanov", "+359 888 200 201");
        var parentGeorgievi = await EnsureParentProfileAsync(context, parentGeorgieviUser.Id, "Maria Georgieva", "+359 888 200 202");
        var parentPetrovi = await EnsureParentProfileAsync(context, parentPetroviUser.Id, "Petar Petrov", "+359 888 200 203");
        var parentDimitrovi = await EnsureParentProfileAsync(context, parentDimitroviUser.Id, "Daniela Dimitrova", "+359 888 200 204");
        var parentStoyanovi = await EnsureParentProfileAsync(context, parentStoyanoviUser.Id, "Stoyan Stoyanov", "+359 888 200 205");
        var parentNikolovi = await EnsureParentProfileAsync(context, parentNikoloviUser.Id, "Elena Nikolova", "+359 888 200 206");
        var parentAngelovi = await EnsureParentProfileAsync(context, parentAngeloviUser.Id, "Angel Angelov", "+359 888 200 207");

        var seedDate = new DateTime(2026, 8, 19);
        var childMila = await EnsureChildAsync(context, "Mila", "Ivanova", Gender.Female, seedDate.AddYears(-4).AddMonths(-2), sunshineGroup.Id, parentIvanovi.Id, DemoAvatarUrl("Mila Ivanova"));
        var childBoris = await EnsureChildAsync(context, "Boris", "Ivanov", Gender.Male, seedDate.AddYears(-5).AddMonths(1), sunshineGroup.Id, parentIvanovi.Id, DemoAvatarUrl("Boris Ivanov"));
        var childElena = await EnsureChildAsync(context, "Elena", "Petrova", Gender.Female, seedDate.AddYears(-4).AddMonths(-8), sunshineGroup.Id, parentPetrovi.Id, DemoAvatarUrl("Elena Petrova"));
        var childViktor = await EnsureChildAsync(context, "Viktor", "Petrov", Gender.Male, seedDate.AddYears(-3).AddMonths(-10), sunshineGroup.Id, parentPetrovi.Id, DemoAvatarUrl("Viktor Petrov"));
        var childTeodor = await EnsureChildAsync(context, "Teodor", "Angelov", Gender.Male, seedDate.AddYears(-5).AddMonths(2), sunshineGroup.Id, parentAngelovi.Id, DemoAvatarUrl("Teodor Angelov"));

        var childSofia = await EnsureChildAsync(context, "Sofia", "Georgieva", Gender.Female, seedDate.AddYears(-3).AddMonths(-8), moonlightGroup.Id, parentGeorgievi.Id, DemoAvatarUrl("Sofia Georgieva"));
        var childMartin = await EnsureChildAsync(context, "Martin", "Georgiev", Gender.Male, seedDate.AddYears(-4).AddMonths(-5), moonlightGroup.Id, parentGeorgievi.Id, DemoAvatarUrl("Martin Georgiev"));
        var childNia = await EnsureChildAsync(context, "Nia", "Dimitrova", Gender.Female, seedDate.AddYears(-4).AddMonths(-1), moonlightGroup.Id, parentDimitrovi.Id, DemoAvatarUrl("Nia Dimitrova"));
        var childKaloyan = await EnsureChildAsync(context, "Kaloyan", "Dimitrov", Gender.Male, seedDate.AddYears(-5).AddMonths(3), moonlightGroup.Id, parentDimitrovi.Id, DemoAvatarUrl("Kaloyan Dimitrov"));
        var childEmma = await EnsureChildAsync(context, "Emma", "Angelova", Gender.Female, seedDate.AddYears(-3).AddMonths(-6), moonlightGroup.Id, parentAngelovi.Id, DemoAvatarUrl("Emma Angelova"));

        var childAlex = await EnsureChildAsync(context, "Alex", "Stoyanov", Gender.Male, seedDate.AddYears(-4).AddMonths(-3), starsGroup.Id, parentStoyanovi.Id, DemoAvatarUrl("Alex Stoyanov"));
        var childRaya = await EnsureChildAsync(context, "Raya", "Stoyanova", Gender.Female, seedDate.AddYears(-3).AddMonths(-11), starsGroup.Id, parentStoyanovi.Id, DemoAvatarUrl("Raya Stoyanova"));
        var childYoana = await EnsureChildAsync(context, "Yoana", "Nikolova", Gender.Female, seedDate.AddYears(-5).AddMonths(4), starsGroup.Id, parentNikolovi.Id, DemoAvatarUrl("Yoana Nikolova"));
        var childDaniel = await EnsureChildAsync(context, "Daniel", "Nikolov", Gender.Male, seedDate.AddYears(-4).AddMonths(-7), starsGroup.Id, parentNikolovi.Id, DemoAvatarUrl("Daniel Nikolov"));
        var childNikola = await EnsureChildAsync(context, "Nikola", "Angelov", Gender.Male, seedDate.AddYears(-3).AddMonths(-9), starsGroup.Id, parentAngelovi.Id, DemoAvatarUrl("Nikola Angelov"));

        await EnsureMedicalRecordAsync(context, childMila.Id, "Strawberries", null, "Dr. Elena Markova", "+359 888 300 301", "Ivan Ivanov", "+359 888 200 201", "Use the allergy plan if a reaction appears.");
        await EnsureMedicalRecordAsync(context, childSofia.Id, null, "Mild asthma", "Dr. Petar Kolev", "+359 888 300 302", "Maria Georgieva", "+359 888 200 202", "Keep inhaler instructions available during trips.");
        await EnsureMedicalRecordAsync(context, childTeodor.Id, "Peanuts", null, "Dr. Elena Markova", "+359 888 300 301", "Angel Angelov", "+359 888 200 207", "Avoid snacks with nuts.");
        await EnsureMedicalRecordAsync(context, childRaya.Id, null, "Seasonal allergies", "Dr. Ivaylo Marinov", "+359 888 300 303", "Stoyan Stoyanov", "+359 888 200 205", "Watch for symptoms during spring outdoor play.");
        await EnsureMedicalRecordAsync(context, childDaniel.Id, "Milk protein", null, "Dr. Petar Kolev", "+359 888 300 302", "Elena Nikolova", "+359 888 200 206", "Use dairy-free meal options.");

        await EnsureAttendanceRecordAsync(context, childMila.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childBoris.Id, seedDate, AttendanceStatus.Late, "Arrived after morning circle.");
        await EnsureAttendanceRecordAsync(context, childElena.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childViktor.Id, seedDate, AttendanceStatus.Absent, "Parent called in the morning.");
        await EnsureAttendanceRecordAsync(context, childTeodor.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childSofia.Id, seedDate, AttendanceStatus.Sick, "Parent reported fever.");
        await EnsureAttendanceRecordAsync(context, childMartin.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childNia.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childKaloyan.Id, seedDate, AttendanceStatus.Late, "Traffic delay.");
        await EnsureAttendanceRecordAsync(context, childEmma.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childAlex.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childRaya.Id, seedDate, AttendanceStatus.Vacation, "Family trip.");
        await EnsureAttendanceRecordAsync(context, childYoana.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childDaniel.Id, seedDate, AttendanceStatus.Sick, "Doctor appointment.");
        await EnsureAttendanceRecordAsync(context, childNikola.Id, seedDate, AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childMila.Id, seedDate.AddDays(-1), AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childBoris.Id, seedDate.AddDays(-1), AttendanceStatus.Present, null);
        await EnsureAttendanceRecordAsync(context, childSofia.Id, seedDate.AddDays(-1), AttendanceStatus.Vacation, "Family trip.");
        await EnsureAttendanceRecordAsync(context, childAlex.Id, seedDate.AddDays(-1), AttendanceStatus.Late, "Arrived during breakfast.");
        await EnsureAttendanceRecordAsync(context, childYoana.Id, seedDate.AddDays(-1), AttendanceStatus.Present, null);

        await EnsureDailyReportAsync(context, childMila.Id, teacherSunshine.UserId, seedDate, ChildMood.Happy, 5, 4, 5, "Mila enjoyed painting and helped clean up after lunch.");
        await EnsureDailyReportAsync(context, childBoris.Id, teacherSunshine.UserId, seedDate, ChildMood.Calm, 4, 5, 4, "Boris had a calm day and slept well.");
        await EnsureDailyReportAsync(context, childElena.Id, teacherSunshine.UserId, seedDate, ChildMood.Happy, 5, 5, 4, "Elena joined the group story time with confidence.");
        await EnsureDailyReportAsync(context, childViktor.Id, teacherSunshine.UserId, seedDate.AddDays(-1), ChildMood.Tired, 3, 4, 3, "Viktor preferred quiet blocks and puzzles.");
        await EnsureDailyReportAsync(context, childTeodor.Id, teacherSunshine.UserId, seedDate, ChildMood.Calm, 4, 4, 5, "Teodor had a focused day and loved the outdoor games.");
        await EnsureDailyReportAsync(context, childSofia.Id, teacherMoonlight.UserId, seedDate.AddDays(-1), ChildMood.Tired, 3, 3, 4, "Sofia preferred quiet activities yesterday.");
        await EnsureDailyReportAsync(context, childMartin.Id, teacherMoonlight.UserId, seedDate, ChildMood.Happy, 4, 5, 5, "Martin was very social during music time.");
        await EnsureDailyReportAsync(context, childNia.Id, teacherMoonlight.UserId, seedDate, ChildMood.Calm, 5, 4, 4, "Nia enjoyed sensory play and ate well.");
        await EnsureDailyReportAsync(context, childKaloyan.Id, teacherMoonlight.UserId, seedDate, ChildMood.Happy, 4, 4, 5, "Kaloyan was active and cheerful.");
        await EnsureDailyReportAsync(context, childEmma.Id, teacherMoonlight.UserId, seedDate.AddDays(-1), ChildMood.Sad, 3, 3, 3, "Emma missed home in the morning but settled after lunch.");
        await EnsureDailyReportAsync(context, childAlex.Id, teacherStars.UserId, seedDate, ChildMood.Happy, 5, 4, 5, "Alex built a large train track with friends.");
        await EnsureDailyReportAsync(context, childRaya.Id, teacherStars.UserId, seedDate.AddDays(-1), ChildMood.Calm, 4, 5, 4, "Raya had a peaceful day and slept well.");
        await EnsureDailyReportAsync(context, childYoana.Id, teacherStars.UserId, seedDate, ChildMood.Happy, 5, 5, 5, "Yoana participated in every activity today.");
        await EnsureDailyReportAsync(context, childDaniel.Id, teacherStars.UserId, seedDate.AddDays(-1), ChildMood.Tired, 3, 4, 3, "Daniel was a bit tired but enjoyed drawing.");
        await EnsureDailyReportAsync(context, childNikola.Id, teacherStars.UserId, seedDate, ChildMood.Calm, 4, 4, 4, "Nikola followed the routine very well.");

        await EnsureAbsenceRequestAsync(context, childSofia.Id, parentGeorgievi.UserId, teacherMoonlight.UserId, seedDate, seedDate.AddDays(1), AbsenceReason.Sick, "Sofia will stay home until she feels better.", RequestStatus.Approved, "Confirmed by teacher.");
        await EnsureAbsenceRequestAsync(context, childMartin.Id, parentGeorgievi.UserId, null, seedDate.AddDays(3), seedDate.AddDays(5), AbsenceReason.FamilyReason, "Family travel planned.", RequestStatus.Pending, null);
        await EnsureAbsenceRequestAsync(context, childRaya.Id, parentStoyanovi.UserId, teacherStars.UserId, seedDate, seedDate.AddDays(2), AbsenceReason.Vacation, "Short family vacation.", RequestStatus.Approved, "Confirmed by teacher.");
        await EnsureAbsenceRequestAsync(context, childDaniel.Id, parentNikolovi.UserId, null, seedDate.AddDays(1), seedDate.AddDays(1), AbsenceReason.Sick, "Medical appointment.", RequestStatus.Pending, null);

        await EnsureConsentRequestAsync(context, childMila.Id, teacherSunshine.UserId, parentIvanovi.UserId, "Photo permission for kindergarten gallery", ConsentRequestType.PhotoPermission, "May we include Mila in photos from group activities?", RequestStatus.Approved, "Approved by parent.");
        await EnsureConsentRequestAsync(context, childBoris.Id, teacherSunshine.UserId, null, "Field trip permission", ConsentRequestType.FieldTrip, "Permission needed for the upcoming park visit.", RequestStatus.Pending, null);
        await EnsureConsentRequestAsync(context, childSofia.Id, teacherMoonlight.UserId, parentGeorgievi.UserId, "Special activity participation", ConsentRequestType.EventParticipation, "Parent response for the music workshop.", RequestStatus.Rejected, "Not this time.");
        await EnsureConsentRequestAsync(context, childTeodor.Id, teacherSunshine.UserId, null, "Cooking workshop permission", ConsentRequestType.EventParticipation, "Permission needed for a supervised cooking workshop.", RequestStatus.Pending, null);
        await EnsureConsentRequestAsync(context, childYoana.Id, teacherStars.UserId, parentNikolovi.UserId, "Photo permission for class album", ConsentRequestType.PhotoPermission, "May we include Yoana in the class album?", RequestStatus.Approved, "Approved by parent.");
        await EnsureConsentRequestAsync(context, childEmma.Id, teacherMoonlight.UserId, parentAngelovi.UserId, "Medical assistance permission", ConsentRequestType.MedicalAssistance, "Permission for basic first aid if needed.", RequestStatus.Approved, "Approved by parent.");

        await EnsureAnnouncementAsync(context, "Welcome to the new kindergarten week", "Please bring indoor shoes and a labeled water bottle.", null, true, seedDate.AddDays(-2));
        await EnsureAnnouncementAsync(context, "Sunshine group picnic reminder", "The Sunshine group picnic starts at 10:00. Please pack a light snack.", sunshineGroup.Id, false, seedDate.AddDays(-1));
        await EnsureAnnouncementAsync(context, "Moonlight music day", "Children can bring a small safe instrument for music day.", moonlightGroup.Id, false, seedDate);

        await EnsureEventAsync(context, "Parent meeting", "Monthly parent meeting with teachers.", seedDate.AddDays(4).AddHours(17), seedDate.AddDays(4).AddHours(18), EventType.ParentMeeting, "Main classroom", null, true);
        await EnsureEventAsync(context, "Sunshine park trip", "Outdoor trip to the nearby park.", seedDate.AddDays(7).AddHours(9), seedDate.AddDays(7).AddHours(12), EventType.Trip, "City park", sunshineGroup.Id, false);
        await EnsureEventAsync(context, "Birthday celebration", "Group birthday celebration with songs and games.", seedDate.AddDays(10).AddHours(10), null, EventType.Birthday, "Moonlight room", moonlightGroup.Id, false);

        await SeedConversationsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task<IdentityUser> EnsureUserAsync(UserManager<IdentityUser> userManager, string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Demo user seed failed for '{email}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task<KindergartenGroup> EnsureGroupAsync(ApplicationDbContext context, string name, string description)
    {
        var group = await context.KindergartenGroups.FirstOrDefaultAsync(g => g.Name == name);

        if (group != null)
        {
            return group;
        }

        group = new KindergartenGroup
        {
            Name = name,
            Description = description
        };

        await context.KindergartenGroups.AddAsync(group);
        await context.SaveChangesAsync();

        return group;
    }

    private static async Task<TeacherProfile> EnsureTeacherProfileAsync(ApplicationDbContext context, string userId, string fullName, string phoneNumber, int groupId)
    {
        var profile = await context.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId);

        if (profile != null)
        {
            return profile;
        }

        profile = new TeacherProfile
        {
            UserId = userId,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            GroupId = groupId
        };

        await context.TeacherProfiles.AddAsync(profile);
        await context.SaveChangesAsync();

        return profile;
    }

    private static async Task<ParentProfile> EnsureParentProfileAsync(ApplicationDbContext context, string userId, string fullName, string phoneNumber)
    {
        var profile = await context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null)
        {
            return profile;
        }

        profile = new ParentProfile
        {
            UserId = userId,
            FullName = fullName,
            PhoneNumber = phoneNumber
        };

        await context.ParentProfiles.AddAsync(profile);
        await context.SaveChangesAsync();

        return profile;
    }

    private static async Task<Child> EnsureChildAsync(ApplicationDbContext context, string firstName, string lastName, Gender gender, DateTime dateOfBirth, int groupId, int parentId, string photoUrl)
    {
        var child = await context.Children
            .FirstOrDefaultAsync(c =>
                !c.IsDeleted &&
                c.FirstName == firstName &&
                c.LastName == lastName &&
                c.GroupId == groupId &&
                c.ParentId == parentId);

        if (child != null)
        {
            child.Gender = gender;
            child.DateOfBirth = dateOfBirth;
            child.GroupId = groupId;
            child.ParentId = parentId;
            child.PhotoUrl = photoUrl;

            await context.SaveChangesAsync();

            return child;
        }

        child = new Child
        {
            FirstName = firstName,
            LastName = lastName,
            Gender = gender,
            DateOfBirth = dateOfBirth,
            GroupId = groupId,
            ParentId = parentId,
            PhotoUrl = photoUrl
        };

        await context.Children.AddAsync(child);
        await context.SaveChangesAsync();

        return child;
    }

    private static string DemoAvatarUrl(string seed)
    {
        return $"https://api.dicebear.com/9.x/adventurer/svg?seed={Uri.EscapeDataString(seed)}&backgroundColor=b6e3f4,c0aede,d1d4f9,ffd5dc,ffdfbf";
    }

    private static async Task EnsureMedicalRecordAsync(ApplicationDbContext context, int childId, string? allergies, string? chronicConditions, string doctorName, string doctorPhone, string emergencyContactName, string emergencyContactPhone, string notes)
    {
        bool exists = await context.MedicalRecords.AnyAsync(m => m.ChildId == childId && !m.IsDeleted);

        if (exists)
        {
            return;
        }

        await context.MedicalRecords.AddAsync(new MedicalRecord
        {
            ChildId = childId,
            Allergies = allergies,
            ChronicConditions = chronicConditions,
            DoctorName = doctorName,
            DoctorPhone = doctorPhone,
            EmergencyContactName = emergencyContactName,
            EmergencyContactPhone = emergencyContactPhone,
            Notes = notes
        });
    }

    private static async Task EnsureAttendanceRecordAsync(ApplicationDbContext context, int childId, DateTime date, AttendanceStatus status, string? note)
    {
        bool exists = await context.AttendanceRecords.AnyAsync(r => r.ChildId == childId && r.Date.Date == date.Date);

        if (exists)
        {
            return;
        }

        await context.AttendanceRecords.AddAsync(new AttendanceRecord
        {
            ChildId = childId,
            Date = date,
            Status = status,
            Note = note
        });
    }

    private static async Task EnsureDailyReportAsync(ApplicationDbContext context, int childId, string createdByUserId, DateTime reportDate, ChildMood mood, int mealRating, int sleepRating, int activityRating, string teacherNote)
    {
        bool exists = await context.DailyReports.AnyAsync(r => !r.IsDeleted && r.ChildId == childId && r.ReportDate.Date == reportDate.Date);

        if (exists)
        {
            return;
        }

        await context.DailyReports.AddAsync(new DailyReport
        {
            ChildId = childId,
            CreatedByUserId = createdByUserId,
            ReportDate = reportDate,
            Mood = mood,
            MealRating = mealRating,
            SleepRating = sleepRating,
            ActivityRating = activityRating,
            TeacherNote = teacherNote
        });
    }

    private static async Task EnsureAbsenceRequestAsync(ApplicationDbContext context, int childId, string requestedByUserId, string? reviewedByUserId, DateTime startDate, DateTime endDate, AbsenceReason reason, string parentNote, RequestStatus status, string? reviewNote)
    {
        bool exists = await context.AbsenceRequests.AnyAsync(r =>
            !r.IsDeleted &&
            r.ChildId == childId &&
            r.StartDate.Date == startDate.Date &&
            r.EndDate.Date == endDate.Date &&
            r.Reason == reason);

        if (exists)
        {
            return;
        }

        await context.AbsenceRequests.AddAsync(new AbsenceRequest
        {
            ChildId = childId,
            RequestedByUserId = requestedByUserId,
            ReviewedByUserId = reviewedByUserId,
            StartDate = startDate,
            EndDate = endDate,
            Reason = reason,
            ParentNote = parentNote,
            Status = status,
            ReviewedOn = reviewedByUserId == null ? null : DateTime.UtcNow,
            ReviewNote = reviewNote
        });
    }

    private static async Task EnsureConsentRequestAsync(ApplicationDbContext context, int childId, string createdByUserId, string? respondedByUserId, string title, ConsentRequestType type, string description, RequestStatus status, string? parentNote)
    {
        bool exists = await context.ConsentRequests.AnyAsync(r => !r.IsDeleted && r.ChildId == childId && r.Title == title);

        if (exists)
        {
            return;
        }

        await context.ConsentRequests.AddAsync(new ConsentRequest
        {
            ChildId = childId,
            CreatedByUserId = createdByUserId,
            RespondedByUserId = respondedByUserId,
            Title = title,
            Type = type,
            Description = description,
            Status = status,
            RespondedOn = respondedByUserId == null ? null : DateTime.UtcNow,
            ParentNote = parentNote
        });
    }

    private static async Task EnsureAnnouncementAsync(ApplicationDbContext context, string title, string content, int? groupId, bool isPublic, DateTime publishedOn)
    {
        bool exists = await context.Announcements.AnyAsync(a => !a.IsDeleted && a.Title == title);

        if (exists)
        {
            return;
        }

        await context.Announcements.AddAsync(new Announcement
        {
            Title = title,
            Content = content,
            GroupId = groupId,
            IsPublic = isPublic,
            PublishedOn = publishedOn
        });
    }

    private static async Task EnsureEventAsync(ApplicationDbContext context, string title, string description, DateTime startDateTime, DateTime? endDateTime, EventType type, string location, int? groupId, bool isPublic)
    {
        bool exists = await context.Events.AnyAsync(e => !e.IsDeleted && e.Title == title);

        if (exists)
        {
            return;
        }

        await context.Events.AddAsync(new Event
        {
            Title = title,
            Description = description,
            StartDateTime = startDateTime,
            EndDateTime = endDateTime,
            Type = type,
            Location = location,
            GroupId = groupId,
            IsPublic = isPublic
        });
    }

    private static async Task SeedConversationsAsync(ApplicationDbContext context)
    {
        var children = await context.Children
            .Include(c => c.Parent)
            .Include(c => c.Group)
                .ThenInclude(g => g!.Teachers)
            .Where(c => !c.IsDeleted &&
                        c.Parent != null &&
                        c.Group != null &&
                        c.Group.Teachers.Any())
            .Take(8)
            .ToListAsync();

        foreach (var child in children)
        {
            var parentUserId = child.Parent!.UserId;
            var teacherUserId = child.Group!.Teachers.First().UserId;

            if (string.IsNullOrWhiteSpace(parentUserId) ||
                string.IsNullOrWhiteSpace(teacherUserId))
            {
                continue;
            }

            bool exists = await context.Conversations.AnyAsync(c =>
                c.ChildId == child.Id &&
                c.ParentUserId == parentUserId &&
                c.TeacherUserId == teacherUserId);

            if (exists)
            {
                continue;
            }

            var conversation = new Conversation
            {
                ChildId = child.Id,
                ParentUserId = parentUserId,
                TeacherUserId = teacherUserId,
                CreatedOn = DateTime.UtcNow.AddDays(-3)
            };

            conversation.Messages.Add(new ChatMessage
            {
                SenderUserId = teacherUserId,
                Content = $"Hello, this is a quick update about {child.FirstName}.",
                SentOn = DateTime.UtcNow.AddHours(-7),
                ReadOn = DateTime.UtcNow.AddHours(-6)
            });

            conversation.Messages.Add(new ChatMessage
            {
                SenderUserId = parentUserId,
                Content = "Thank you for the update.",
                SentOn = DateTime.UtcNow.AddHours(-4),
                ReadOn = null
            });

            await context.Conversations.AddAsync(conversation);
        }
    }
}
