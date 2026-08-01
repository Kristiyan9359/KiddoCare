using System.Security.Claims;
using KiddoCare.Services.Core.Contracts;
using KiddoCare.ViewModels.MedicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static KiddoCare.Common.RoleConstants;

namespace KiddoCare.Web.Controllers;

[Authorize]
public class MedicalRecordsController : Controller
{
    private readonly IMedicalRecordService medicalRecordService;

    public MedicalRecordsController(IMedicalRecordService medicalRecordService)
    {
        this.medicalRecordService = medicalRecordService;
    }

    [HttpGet]
    public async Task<IActionResult> Details(int childId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await medicalRecordService.GetDetailsAsync(childId, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Create(int? childId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await medicalRecordService.GetCreateModelAsync(userId, isAdmin, isTeacher);

        if (childId.HasValue)
        {
            model.ChildId = childId.Value;
        }

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(MedicalRecordCreateViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            var createModel = await medicalRecordService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;

            return View(model);
        }

        try
        {
            await medicalRecordService.CreateAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var createModel = await medicalRecordService.GetCreateModelAsync(userId, isAdmin, isTeacher);
            model.Children = createModel.Children;

            return View(model);
        }

        return RedirectToAction("Index", "Children");
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await medicalRecordService.GetForEditAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    public async Task<IActionResult> Edit(MedicalRecordEditViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await medicalRecordService.EditAsync(model, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction("Details", "MedicalRecords", new { childId = model.Id });
    }

    [Authorize(Roles = Admin)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        var model = await medicalRecordService.GetForDeleteAsync(id, userId, isAdmin, isTeacher);

        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [Authorize(Roles = Admin)]
    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole(Admin);
        var isTeacher = User.IsInRole(Teacher);

        try
        {
            await medicalRecordService.DeleteAsync(id, userId, isAdmin, isTeacher);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        return RedirectToAction("Index", "Children");
    }
}