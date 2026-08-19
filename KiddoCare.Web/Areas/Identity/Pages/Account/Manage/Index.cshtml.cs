namespace KiddoCare.Web.Areas.Identity.Pages.Account.Manage;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using KiddoCare.Common;
using KiddoCare.Data;
using KiddoCare.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using static KiddoCare.Common.ValidationConstants;

[Authorize]
public class IndexModel : PageModel
{
    private const string FullNameClaimType = "FullName";

    private readonly ApplicationDbContext context;
    private readonly UserManager<IdentityUser> userManager;
    private readonly SignInManager<IdentityUser> signInManager;
    private readonly IStringLocalizer<SharedResource> localizer;

    public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IStringLocalizer<SharedResource> localizer)
    {
        this.context = context;
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.localizer = localizer;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string ProfileMetaLabel { get; set; } = string.Empty;

    public string ProfileMetaValue { get; set; } = string.Empty;

    public class InputModel
    {
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "New email address")]
        public string? NewEmail { get; set; }

        [Required(ErrorMessage = "Please enter your full name.")]
        [StringLength(ParentFullNameMaxLength, ErrorMessage = "Full name cannot be longer than {1} characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(ParentPhoneNumberMaxLength, ErrorMessage = "Phone number cannot be longer than {1} characters.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm email change with password")]
        public string? EmailChangePassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, ErrorMessage = "Password cannot be longer than {1} characters.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm new password")]
        public string? ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound();
        }

        await LoadProfileAsync(user);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);

        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await LoadPageMetaAsync(user);

            return Page();
        }

        var newEmail = Input.NewEmail?.Trim();
        bool shouldChangeEmail = !string.IsNullOrWhiteSpace(newEmail) && !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase);
        bool shouldChangePassword =
            !string.IsNullOrWhiteSpace(Input.CurrentPassword) ||
            !string.IsNullOrWhiteSpace(Input.NewPassword) ||
            !string.IsNullOrWhiteSpace(Input.ConfirmPassword);

        if (shouldChangeEmail && !ValidateEmailChangeInput())
        {
            await LoadPageMetaAsync(user);

            return Page();
        }

        if (shouldChangePassword && !ValidatePasswordChangeInput())
        {
            await LoadPageMetaAsync(user);

            return Page();
        }

        if (!string.IsNullOrWhiteSpace(newEmail))
        {
            var existingUser = await userManager.FindByEmailAsync(newEmail);

            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError("Input.NewEmail", localizer["This email address is already used by another account."]);
                await LoadPageMetaAsync(user);

                return Page();
            }
        }

        if (shouldChangeEmail)
        {
            bool isCurrentPasswordValid = await userManager.CheckPasswordAsync(user, Input.EmailChangePassword!);

            if (!isCurrentPasswordValid)
            {
                ModelState.AddModelError("Input.EmailChangePassword", localizer["Incorrect password."]);
                await LoadPageMetaAsync(user);

                return Page();
            }
        }

        if (shouldChangePassword)
        {
            var passwordResult = await userManager.ChangePasswordAsync(user, Input.CurrentPassword!, Input.NewPassword!);

            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, localizer[error.Description]);
                }

                await LoadPageMetaAsync(user);

                return Page();
            }
        }

        if (User.IsInRole(RoleConstants.Parent))
        {
            var parent = await context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id && !p.IsDeleted);

            if (parent == null)
            {
                return NotFound();
            }

            parent.FullName = Input.FullName;
            parent.PhoneNumber = Input.PhoneNumber;
        }
        else if (User.IsInRole(RoleConstants.Teacher))
        {
            var teacher = await context.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == user.Id && !t.IsDeleted);

            if (teacher == null)
            {
                return NotFound();
            }

            teacher.FullName = Input.FullName;
            teacher.PhoneNumber = Input.PhoneNumber;
        }
        else
        {
            await SetFullNameClaimAsync(user, Input.FullName);
        }

        if (shouldChangeEmail)
        {
            user.Email = newEmail!;
            user.UserName = newEmail!;
            user.NormalizedEmail = userManager.NormalizeEmail(newEmail!);
            user.NormalizedUserName = userManager.NormalizeName(newEmail!);
            user.EmailConfirmed = true;
        }

        user.PhoneNumber = Input.PhoneNumber;

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadPageMetaAsync(user);

            return Page();
        }

        await context.SaveChangesAsync();
        await signInManager.RefreshSignInAsync(user);

        TempData[ControllerNotificationExtensions.SuccessMessageKey] = "Profile updated successfully.";

        return RedirectToPage();
    }

    private bool ValidateEmailChangeInput()
    {
        if (string.IsNullOrWhiteSpace(Input.EmailChangePassword))
        {
            ModelState.AddModelError("Input.EmailChangePassword", localizer["Please enter your password to confirm the email change."]);
        }

        return ModelState.IsValid;
    }

    private bool ValidatePasswordChangeInput()
    {
        if (string.IsNullOrWhiteSpace(Input.CurrentPassword))
        {
            ModelState.AddModelError("Input.CurrentPassword", localizer["Please enter your current password."]);
        }

        if (string.IsNullOrWhiteSpace(Input.NewPassword))
        {
            ModelState.AddModelError("Input.NewPassword", localizer["Please enter a new password."]);
        }

        if (string.IsNullOrWhiteSpace(Input.ConfirmPassword))
        {
            ModelState.AddModelError("Input.ConfirmPassword", localizer["Please confirm your new password."]);
        }

        return ModelState.IsValid;
    }

    private async Task LoadProfileAsync(IdentityUser user)
    {
        await LoadPageMetaAsync(user);

        if (User.IsInRole(RoleConstants.Parent))
        {
            var parent = await context.ParentProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id && !p.IsDeleted);

            if (parent != null)
            {
                Input.FullName = parent.FullName;
                Input.PhoneNumber = parent.PhoneNumber;
            }
        }
        else if (User.IsInRole(RoleConstants.Teacher))
        {
            var teacher = await context.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == user.Id && !t.IsDeleted);

            if (teacher != null)
            {
                Input.FullName = teacher.FullName;
                Input.PhoneNumber = teacher.PhoneNumber;
            }
        }
        else
        {
            var fullNameClaim = (await userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == FullNameClaimType);

            Input.FullName = fullNameClaim?.Value ?? user.Email ?? string.Empty;
            Input.PhoneNumber = user.PhoneNumber;
        }

        Input.NewEmail = null;
        Input.EmailChangePassword = null;
        Input.CurrentPassword = null;
        Input.NewPassword = null;
        Input.ConfirmPassword = null;
    }

    private async Task LoadPageMetaAsync(IdentityUser user)
    {
        Email = user.Email ?? string.Empty;
        RoleName = await GetRoleNameAsync(user);
        await LoadProfileMetaAsync(user);
    }

    private async Task<string> GetRoleNameAsync(IdentityUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        return roles.FirstOrDefault() ?? "User";
    }

    private async Task LoadProfileMetaAsync(IdentityUser user)
    {
        if (User.IsInRole(RoleConstants.Parent))
        {
            int childrenCount = await context.Children.CountAsync(c => c.Parent != null && c.Parent.UserId == user.Id && !c.IsDeleted);

            ProfileMetaLabel = "Profile linked children";
            ProfileMetaValue = childrenCount.ToString();

            return;
        }

        if (User.IsInRole(RoleConstants.Teacher))
        {
            var groupName = await context.TeacherProfiles
                .Where(t => t.UserId == user.Id && !t.IsDeleted)
                .Select(t => t.Group.Name)
                .FirstOrDefaultAsync();

            ProfileMetaLabel = "Profile assigned group";
            ProfileMetaValue = string.IsNullOrWhiteSpace(groupName) ? "No group assigned" : groupName;

            return;
        }

        ProfileMetaLabel = "Access level";
        ProfileMetaValue = "System administrator";
    }

    private async Task SetFullNameClaimAsync(IdentityUser user, string fullName)
    {
        var claims = await userManager.GetClaimsAsync(user);
        var existingClaim = claims.FirstOrDefault(c => c.Type == FullNameClaimType);

        if (existingClaim != null)
        {
            await userManager.ReplaceClaimAsync(user, existingClaim, new Claim(FullNameClaimType, fullName));

            return;
        }

        await userManager.AddClaimAsync(user, new Claim(FullNameClaimType, fullName));
    }

}
