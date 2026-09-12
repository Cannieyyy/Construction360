using System.Data.SqlClient;
using Construction360.Models;
using Construction360.Enums;
using Construction360.ViewModels;
using Construction360.Services;

namespace Construction360.Repositories
{
    public class SupervisorRepository : ISupervisorRepository
    {
        private readonly DatabaseService _databaseService;

        public SupervisorRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<SupervisorDashboardViewModel> GetDashboardDataAsync(int supervisorId)
        {
            var vm = new SupervisorDashboardViewModel();

            try
            {
                // Get team members
                var teamMembers = await GetTeamMembersAsync(supervisorId);
                vm.TeamSize = teamMembers?.Count ?? 0;

                // Get today's attendance
                var today = DateTime.Today;
                var stats = await GetAttendanceStatsAsync(supervisorId, today);

                vm.PresentToday = stats?.GetValueOrDefault("Present", 0) ?? 0;
                vm.AbsentToday = stats?.GetValueOrDefault("Absent", 0) ?? 0;
                vm.LateToday = stats?.GetValueOrDefault("Late", 0) ?? 0;
                vm.OnLeaveToday = stats?.GetValueOrDefault("OnLeave", 0) ?? 0;

                // Get pending leave requests
                var pendingLeaves = await GetTeamLeaveRequestsAsync(supervisorId, "Pending");
                vm.PendingApprovals = pendingLeaves?.Count ?? 0;
                vm.RecentLeaveRequests = pendingLeaves?.Take(5).ToList() ?? new List<LeaveRequest>();

                // Get weekly attendance
                vm.WeeklyAttendance = await GetWeeklyAttendanceAsync(supervisorId) ?? new List<WeeklyAttendance>();

                // Get productivity data from database
                var productivity = await GetTeamProductivityAsync(supervisorId);
                vm.ProductivityTrend = productivity ?? new List<ProductivityRecord>();

                // Calculate average productivity from actual data (only if data exists)
                if (vm.ProductivityTrend.Any() && vm.ProductivityTrend.Any(p => p.Actual > 0))
                {
                    vm.TeamProductivity = Math.Round(vm.ProductivityTrend.Average(p => p.Actual), 1);

                    if (vm.ProductivityTrend.Count >= 2)
                    {
                        var current = vm.ProductivityTrend.Last().Actual;
                        var previous = vm.ProductivityTrend[vm.ProductivityTrend.Count - 2].Actual;
                        vm.ProductivityChange = previous > 0
                            ? Math.Round(((current - previous) / previous) * 100, 1)
                            : 0;
                    }
                }
                else
                {
                    vm.TeamProductivity = 0;
                    vm.ProductivityChange = 0;
                }

                // Get recent attendance
                vm.RecentAttendance = await GetRecentAttendanceAsync(supervisorId, 5) ?? new List<AttendanceRecord>();

                // Get real stats from database - NO RANDOM DATA
                vm.AvgEfficiency = vm.TeamProductivity;
                vm.TotalTasksCompleted = await GetTotalTasksCompletedAsync(supervisorId);
                vm.TotalHoursWorked = await GetTotalHoursWorkedAsync(supervisorId);
                vm.TopPerformer = await GetTopPerformerAsync(supervisorId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard data: {ex.Message}");

                // Ensure all collections have default values (all zeros/empty)
                vm.WeeklyAttendance ??= new List<WeeklyAttendance>();
                vm.ProductivityTrend ??= new List<ProductivityRecord>();
                vm.RecentLeaveRequests ??= new List<LeaveRequest>();
                vm.RecentAttendance ??= new List<AttendanceRecord>();
                vm.TeamProductivity = 0;
                vm.ProductivityChange = 0;
                vm.AvgEfficiency = 0;
                vm.TotalTasksCompleted = 0;
                vm.TotalHoursWorked = 0;
                vm.TopPerformer = "N/A";
            }

            return vm;
        }

        public async Task<List<AttendanceRecord>> GetTeamAttendanceAsync(int supervisorId, DateTime? date = null)
        {
            var records = new List<AttendanceRecord>();
            var targetDate = date ?? DateTime.Today;

            string sql = @"
                SELECT 
                    a.Attendance_ID,
                    a.Employee_ID,
                    a.Date,
                    a.CheckInTime,
                    a.CheckOutTime,
                    a.Status,
                    e.First_Name + ' ' + e.Last_Name AS EmployeeName
                FROM Attendance a
                INNER JOIN Employees e ON a.Employee_ID = e.Employee_ID
                WHERE a.Date = @Date
                ORDER BY a.Status, e.First_Name";

            var parameters = new[] { new SqlParameter("@Date", targetDate) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            while (await reader.ReadAsync())
            {
                records.Add(MapAttendanceRecord(reader));
            }

            return records;
        }

        public async Task<List<LeaveRequest>> GetTeamLeaveRequestsAsync(int supervisorId, string? status = null)
        {
            var requests = new List<LeaveRequest>();

            string sql = @"
                SELECT 
                    l.Leave_ID,
                    l.Employee_ID,
                    l.Type,
                    l.Status,
                    l.StartDate,
                    l.EndDate,
                    l.Reason,
                    l.SubmittedDate,
                    e.First_Name + ' ' + e.Last_Name AS EmployeeName
                FROM LeaveRequests l
                INNER JOIN Employees e ON l.Employee_ID = e.Employee_ID
                WHERE 1=1";

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND l.Status = @Status";
            }

            sql += " ORDER BY l.SubmittedDate DESC";

            var parameters = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(status))
            {
                parameters.Add(new SqlParameter("@Status", status));
            }

            using var reader = _databaseService.ExecuteReader(sql, parameters.ToArray());
            while (await reader.ReadAsync())
            {
                requests.Add(MapLeaveRequest(reader));
            }

            return requests;
        }

        public async Task<List<ProductivityRecord>> GetTeamProductivityAsync(int supervisorId)
        {
            var records = new List<ProductivityRecord>();

            try
            {
                // Get productivity data from database
                string sql = @"
                    SELECT 
                        DATEPART(week, Date) AS WeekNumber,
                        AVG(CAST(ToiletsProduced AS FLOAT) / NULLIF(DailyTarget, 0) * 100) AS AvgProductivity,
                        MIN(Date) AS WeekStart
                    FROM Productivity
                    WHERE DailyTarget > 0
                        AND Date >= DATEADD(week, -4, GETDATE())
                    GROUP BY DATEPART(week, Date), DATEPART(year, Date)
                    ORDER BY WeekStart DESC";

                using var reader = _databaseService.ExecuteReader(sql);

                var tempRecords = new List<(int WeekNumber, double Actual, DateTime WeekStart)>();

                while (await reader.ReadAsync())
                {
                    var weekNumber = reader["WeekNumber"] != DBNull.Value
                        ? Convert.ToInt32(reader["WeekNumber"])
                        : 0;

                    var actual = reader["AvgProductivity"] != DBNull.Value
                        ? Convert.ToDouble(reader["AvgProductivity"])
                        : 0;

                    var weekStart = reader["WeekStart"] != DBNull.Value
                        ? Convert.ToDateTime(reader["WeekStart"])
                        : DateTime.Today;

                    tempRecords.Add((weekNumber, actual, weekStart));
                }
                reader.Close();

                // If we have data, process it - NO RANDOM DATA
                if (tempRecords.Any())
                {
                    var sorted = tempRecords.OrderBy(r => r.WeekStart).ToList();

                    for (int i = 0; i < sorted.Count && i < 4; i++)
                    {
                        records.Add(new ProductivityRecord
                        {
                            Week = $"Week {i + 1}",
                            Target = 90,
                            Actual = Math.Round(sorted[i].Actual, 1)
                        });
                    }

                    // Pad with zeros if less than 4 weeks - ONLY ZEROS
                    while (records.Count < 4)
                    {
                        records.Insert(0, new ProductivityRecord
                        {
                            Week = $"Week {records.Count + 1}",
                            Target = 90,
                            Actual = 0
                        });
                    }
                }
                else
                {
                    // No data - return ALL ZEROS
                    for (int i = 0; i < 4; i++)
                    {
                        records.Add(new ProductivityRecord
                        {
                            Week = $"Week {i + 1}",
                            Target = 90,
                            Actual = 0
                        });
                    }
                }

                return records;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTeamProductivityAsync error: {ex.Message}");
                // Return ALL ZEROS on error
                for (int i = 0; i < 4; i++)
                {
                    records.Add(new ProductivityRecord
                    {
                        Week = $"Week {i + 1}",
                        Target = 90,
                        Actual = 0
                    });
                }
                return records;
            }
        }

        public async Task<List<Employee>> GetTeamMembersAsync(int supervisorId)
        {
            var employees = new List<Employee>();

            string sql = @"
                SELECT 
                    Employee_ID,
                    First_Name + ' ' + Last_Name AS FullName,
                    Position,
                    Department,
                    'Active' AS Status
                FROM Employees
                WHERE Department IN (
                    SELECT Department FROM Users WHERE User_ID = @SupervisorId
                )
                ORDER BY First_Name, Last_Name";

            var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            while (await reader.ReadAsync())
            {
                employees.Add(new Employee
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Employee_ID")),
                    FullName = reader["FullName"].ToString() ?? "",
                    Position = reader["Position"].ToString() ?? "",
                    Department = reader["Department"].ToString() ?? "",
                    Status = reader["Status"].ToString() ?? "Active"
                });
            }

            return employees;
        }

        public async Task<Dictionary<string, int>> GetAttendanceStatsAsync(int supervisorId, DateTime date)
        {
            var stats = new Dictionary<string, int>
            {
                { "Present", 0 },
                { "Absent", 0 },
                { "Late", 0 },
                { "OnLeave", 0 }
            };

            string sql = @"
                SELECT 
                    Status,
                    COUNT(*) AS Count
                FROM Attendance a
                INNER JOIN Employees e ON a.Employee_ID = e.Employee_ID
                WHERE a.Date = @Date
                GROUP BY Status";

            var parameters = new[] { new SqlParameter("@Date", date) };

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            while (await reader.ReadAsync())
            {
                var status = reader["Status"].ToString() ?? "";
                var count = reader.GetInt32(reader.GetOrdinal("Count"));

                if (stats.ContainsKey(status))
                {
                    stats[status] = count;
                }
            }

            return stats;
        }

        public async Task<bool> ApproveLeaveAsync(int leaveId)
        {
            string sql = "UPDATE LeaveRequests SET Status = 'Approved' WHERE Leave_ID = @LeaveId";
            var parameters = new[] { new SqlParameter("@LeaveId", leaveId) };

            var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
            return result > 0;
        }

        public async Task<bool> RejectLeaveAsync(int leaveId)
        {
            string sql = "UPDATE LeaveRequests SET Status = 'Rejected' WHERE Leave_ID = @LeaveId";
            var parameters = new[] { new SqlParameter("@LeaveId", leaveId) };

            var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
            return result > 0;
        }

        // ===== DATABASE-ONLY METHODS - NO RANDOM DATA =====

        public async Task<int> GetTotalTasksCompletedAsync(int supervisorId)
        {
            try
            {
                // Count total tasks completed from Productivity table
                string sql = @"
                    SELECT ISNULL(SUM(ToiletsProduced), 0) 
                    FROM Productivity p
                    INNER JOIN Employees e ON p.Employee_ID = e.Employee_ID
                    WHERE e.Department IN (
                        SELECT Department FROM Users WHERE User_ID = @SupervisorId
                    )
                    AND p.Date >= DATEADD(month, -1, GETDATE())";

                var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };
                var result = _databaseService.ExecuteScalar(sql, parameters);

                // Return 0 if no data (not random)
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTotalTasksCompletedAsync error: {ex.Message}");
                return 0; // Always return 0 on error, not random
            }
        }

        public async Task<int> GetTotalHoursWorkedAsync(int supervisorId)
        {
            try
            {
                // Calculate total hours worked from Attendance table
                string sql = @"
                    SELECT ISNULL(SUM(DATEDIFF(HOUR, CheckInTime, CheckOutTime)), 0) AS TotalHours
                    FROM Attendance a
                    INNER JOIN Employees e ON a.Employee_ID = e.Employee_ID
                    WHERE e.Department IN (
                        SELECT Department FROM Users WHERE User_ID = @SupervisorId
                    )
                    AND a.Date >= DATEADD(week, -1, GETDATE())
                    AND a.CheckInTime IS NOT NULL
                    AND a.CheckOutTime IS NOT NULL";

                var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };
                var result = _databaseService.ExecuteScalar(sql, parameters);

                // Return 0 if no data (not random)
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTotalHoursWorkedAsync error: {ex.Message}");
                return 0; // Always return 0 on error, not random
            }
        }

        public async Task<string> GetTopPerformerAsync(int supervisorId)
        {
            try
            {
                // Find the employee with highest productivity
                string sql = @"
                    SELECT TOP 1
                        e.First_Name + ' ' + e.Last_Name AS FullName,
                        AVG(CAST(p.ToiletsProduced AS FLOAT) / NULLIF(p.DailyTarget, 0) * 100) AS Productivity
                    FROM Productivity p
                    INNER JOIN Employees e ON p.Employee_ID = e.Employee_ID
                    WHERE e.Department IN (
                        SELECT Department FROM Users WHERE User_ID = @SupervisorId
                    )
                    AND p.DailyTarget > 0
                    AND p.Date >= DATEADD(month, -1, GETDATE())
                    GROUP BY e.First_Name, e.Last_Name, e.Employee_ID
                    HAVING AVG(CAST(p.ToiletsProduced AS FLOAT) / NULLIF(p.DailyTarget, 0) * 100) > 0
                    ORDER BY Productivity DESC";

                var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };
                using var reader = _databaseService.ExecuteReader(sql, parameters);

                if (await reader.ReadAsync())
                {
                    var name = reader["FullName"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
                reader.Close();

                // If no productivity data, check attendance for most present employee
                string attendanceSql = @"
                    SELECT TOP 1
                        e.First_Name + ' ' + e.Last_Name AS FullName,
                        COUNT(*) AS DaysPresent
                    FROM Attendance a
                    INNER JOIN Employees e ON a.Employee_ID = e.Employee_ID
                    WHERE e.Department IN (
                        SELECT Department FROM Users WHERE User_ID = @SupervisorId
                    )
                    AND a.Status = 'Present'
                    AND a.Date >= DATEADD(month, -1, GETDATE())
                    GROUP BY e.First_Name, e.Last_Name
                    ORDER BY DaysPresent DESC";

                using var reader2 = _databaseService.ExecuteReader(attendanceSql, parameters);
                if (await reader2.ReadAsync())
                {
                    var name = reader2["FullName"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }

                // No data found - return "N/A"
                return "N/A";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTopPerformerAsync error: {ex.Message}");
                return "N/A"; // Always return N/A on error
            }
        }

        // ===== PRIVATE HELPER METHODS =====

        private async Task<List<WeeklyAttendance>> GetWeeklyAttendanceAsync(int supervisorId)
        {
            var weekly = new List<WeeklyAttendance>();
            var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);

            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                var stats = await GetAttendanceStatsAsync(supervisorId, date);

                weekly.Add(new WeeklyAttendance
                {
                    Day = days[i],
                    Present = stats.GetValueOrDefault("Present", 0),
                    Absent = stats.GetValueOrDefault("Absent", 0),
                    Late = stats.GetValueOrDefault("Late", 0)
                });
            }

            return weekly;
        }

        private async Task<List<AttendanceRecord>> GetRecentAttendanceAsync(int supervisorId, int count)
        {
            string sql = @"
                SELECT TOP (@Count)
                    a.Attendance_ID,
                    a.Employee_ID,
                    a.Date,
                    a.CheckInTime,
                    a.CheckOutTime,
                    a.Status,
                    e.First_Name + ' ' + e.Last_Name AS EmployeeName
                FROM Attendance a
                INNER JOIN Employees e ON a.Employee_ID = e.Employee_ID
                WHERE e.Department IN (
                    SELECT Department FROM Users WHERE User_ID = @SupervisorId
                )
                ORDER BY a.Date DESC, a.CheckInTime DESC";

            var parameters = new[]
            {
                new SqlParameter("@Count", count),
                new SqlParameter("@SupervisorId", supervisorId)
            };
            var records = new List<AttendanceRecord>();

            using var reader = _databaseService.ExecuteReader(sql, parameters);
            while (await reader.ReadAsync())
            {
                records.Add(MapAttendanceRecord(reader));
            }

            return records;
        }

        private AttendanceRecord MapAttendanceRecord(SqlDataReader reader)
        {
            var status = reader["Status"].ToString() ?? "Absent";
            Enum.TryParse<AttendanceStatus>(status, true, out var attendanceStatus);

            return new AttendanceRecord
            {
                Id = reader.GetInt32(reader.GetOrdinal("Attendance_ID")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("Employee_ID")),
                EmployeeName = reader["EmployeeName"].ToString() ?? "",
                Date = reader.GetDateTime(reader.GetOrdinal("Date")),
                CheckIn = reader["CheckInTime"] != DBNull.Value
                    ? reader.GetDateTime(reader.GetOrdinal("CheckInTime")).TimeOfDay // Fixed the issue by using TimeOfDay property
                    : null,
                CheckOut = reader["CheckOutTime"] != DBNull.Value
                    ? reader.GetDateTime(reader.GetOrdinal("CheckOutTime")).TimeOfDay // Fixed the issue by using TimeOfDay property
                    : null,
                Status = attendanceStatus
            };
        }

        private LeaveRequest MapLeaveRequest(SqlDataReader reader)
        {
            var type = reader["Type"].ToString() ?? "Annual";
            var status = reader["Status"].ToString() ?? "Pending";
            Enum.TryParse<LeaveType>(type, true, out var leaveType);
            Enum.TryParse<LeaveStatus>(status, true, out var leaveStatus);

            return new LeaveRequest
            {
                Id = reader.GetInt32(reader.GetOrdinal("Leave_ID")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("Employee_ID")),
                EmployeeName = reader["EmployeeName"].ToString() ?? "",
                Type = leaveType,
                Status = leaveStatus,
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                Reason = reader["Reason"]?.ToString() ?? "",
                SubmittedDate = reader.GetDateTime(reader.GetOrdinal("SubmittedDate"))
            };
        }
    }
}