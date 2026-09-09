using Construction360.Models;
using Construction360.ViewModels;

namespace Construction360.Repositories
{
    public interface ISupervisorRepository
    {
        Task<SupervisorDashboardViewModel> GetDashboardDataAsync(int supervisorId);
        Task<List<AttendanceRecord>> GetTeamAttendanceAsync(int supervisorId, DateTime? date = null);
        Task<List<LeaveRequest>> GetTeamLeaveRequestsAsync(int supervisorId, string? status = null);
        Task<List<ProductivityRecord>> GetTeamProductivityAsync(int supervisorId);
        Task<List<Employee>> GetTeamMembersAsync(int supervisorId);
        Task<bool> ApproveLeaveAsync(int leaveId);
        Task<bool> RejectLeaveAsync(int leaveId);
        Task<Dictionary<string, int>> GetAttendanceStatsAsync(int supervisorId, DateTime date);

        //Database methods
        Task<int> GetTotalTasksCompletedAsync(int supervisorId);
        Task<int> GetTotalHoursWorkedAsync(int supervisorId);
        Task<string> GetTopPerformerAsync(int supervisorId);
    }
}