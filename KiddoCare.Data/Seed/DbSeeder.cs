using KiddoCare.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
