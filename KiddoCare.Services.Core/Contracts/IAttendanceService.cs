using KiddoCare.ViewModels.Attendance;

namespace KiddoCare.Services.Core.Contracts;

public interface IAttendanceService
{
    Task<AttendanceDailyViewModel> GetDailyAttendanceAsync(DateTime date, int? groupId);

    Task SaveDailyAttendanceAsync(AttendanceDailyViewModel model);
}