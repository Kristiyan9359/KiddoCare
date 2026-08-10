using KiddoCare.Data;
using KiddoCare.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KiddoCare.Services.Core;
using KiddoCare.Services.Core.Contracts;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddScoped<IGroupService, GroupService>();

builder.Services.AddScoped<IChildService, ChildService>();

builder.Services.AddScoped<IAttendanceService, AttendanceService>();

builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IParentService, ParentService>();

builder.Services.AddScoped<ITeacherService, TeacherService>();

builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

builder.Services.AddScoped<IDailyReportService, DailyReportService>();

builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();

builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();

builder.Services.AddScoped<IAbsenceRequestService, AbsenceRequestService>();

builder.Services.AddScoped<IConsentRequestService, ConsentRequestService>();

builder.Services.AddScoped<IChildDocumentService, ChildDocumentService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedRolesAsync(scope.ServiceProvider);
    await DbSeeder.SeedAdminAsync(scope.ServiceProvider);
}

app.Run();
