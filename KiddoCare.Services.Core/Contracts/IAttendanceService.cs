using KiddoCare.ViewModels.Attendance;

namespace KiddoCare.Services.Core.Contracts;

public interface IAttendanceService
{
    Task<AttendanceDailyViewModel> GetDailyAttendanceAsync(
        DateTime date,
        int? groupId,
        string userId,
        bool isAdmin,
        bool isTeacher);

    Task SaveDailyAttendanceAsync(
        AttendanceDailyViewModel model,
        string userId,
        bool isAdmin,
        bool isTeacher);

    Task<AttendanceFilterViewModel> GetHistoryAsync(
        AttendanceFilterViewModel filter,
        string userId,
        bool isAdmin,
        bool isTeacher);
}