using KiddoCare.Data.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KiddoCare.ViewModels.Attendance;

public class AttendanceFilterViewModel
{
    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int? GroupId { get; set; }

    public AttendanceStatus? Status { get; set; }

    public string? SearchTerm { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 15;

    public int TotalRecords { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public IEnumerable<SelectListItem> Groups { get; set; } = new List<SelectListItem>();

    public IEnumerable<AttendanceRecordViewModel> Records { get; set; } = new List<AttendanceRecordViewModel>();
}