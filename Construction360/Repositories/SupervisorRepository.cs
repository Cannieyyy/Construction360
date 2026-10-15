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

        // ============================================================
        // ===== DASHBOARD DATA =====
        // ============================================================

        public async Task<SupervisorDashboardViewModel> GetDashboardDataAsync(int supervisorId)
        {
            var vm = new SupervisorDashboardViewModel();

            try
            {
                // Team members count
                var teamMembers = await GetTeamMembersAsync(supervisorId);
                vm.TeamSize = teamMembers?.Count ?? 0;

                // Today's attendance stats
                var stats = await GetAttendanceStatsAsync(supervisorId, DateTime.Today);
                vm.PresentToday = stats.GetValueOrDefault("Present", 0);
                vm.AbsentToday = stats.GetValueOrDefault("Absent", 0);
                vm.LateToday = stats.GetValueOrDefault("Late", 0);
                vm.OnLeaveToday = stats.GetValueOrDefault("OnLeave", 0);

                // Pending leave requests
                var pendingLeaves = await GetTeamLeaveRequestsAsync(supervisorId, "Pending");
                vm.PendingApprovals = pendingLeaves?.Count ?? 0;
                vm.RecentLeaveRequests = pendingLeaves?.Take(5).ToList() ?? new List<LeaveRequest>();

                // Weekly attendance chart data
                vm.WeeklyAttendance = await GetWeeklyAttendanceDataAsync(supervisorId);

                // Productivity data
                var productivity = await GetTeamProductivityAsync(supervisorId);
                vm.ProductivityTrend = productivity ?? new List<ProductivityRecord>();

                // Calculate team productivity average
                if (vm.ProductivityTrend.Any() && vm.ProductivityTrend.Any(p => p.Actual > 0))
                {
                    vm.TeamProductivity = Math.Round(
                        vm.ProductivityTrend.Where(p => p.Actual > 0).Average(p => p.Actual), 1);

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

                // Recent attendance records
                vm.RecentAttendance = await GetRecentAttendanceAsync(supervisorId, 5);

                // Footer stats
                vm.AvgEfficiency = vm.TeamProductivity;
                vm.TotalTasksCompleted = await GetTotalTasksCompletedAsync(supervisorId);
                vm.TotalHoursWorked = await GetTotalHoursWorkedAsync(supervisorId);
                vm.TopPerformer = await GetTopPerformerAsync(supervisorId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetDashboardDataAsync error: {ex.Message}");
            }

            return vm;
        }

        // ============================================================
        // ===== TEAM MEMBERS =====
        // ============================================================

        public async Task<List<Employee>> GetTeamMembersAsync(int supervisorId)
        {
            var employees = new List<Employee>();

            try
            {
                // Get the supervisor's department first
                string deptSql = "SELECT Department FROM Users WHERE User_ID = @SupervisorId";
                var deptParams = new[] { new SqlParameter("@SupervisorId", supervisorId) };

                string? department = null;
                using (var deptReader = _databaseService.ExecuteReader(deptSql, deptParams))
                {
                    if (await deptReader.ReadAsync())
                    {
                        department = deptReader["Department"]?.ToString();
                    }
                }

                Console.WriteLine($"🔍 Supervisor department: {department ?? "(none)"}");

                // Get employees in the same department
                string sql;
                SqlParameter[] parameters;

                if (!string.IsNullOrEmpty(department))
                {
                    sql = @"
                        SELECT 
                            Employee_ID,
                            First_Name + ' ' + Last_Name AS FullName,
                            Position,
                            Department,
                            CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS Status
                        FROM Employees
                        WHERE Department = @Department AND IsActive = 1
                        ORDER BY First_Name, Last_Name";

                    parameters = new[] { new SqlParameter("@Department", department) };
                }
                else
                {
                    // No department — return all active employees
                    sql = @"
                        SELECT 
                            Employee_ID,
                            First_Name + ' ' + Last_Name AS FullName,
                            Position,
                            Department,
                            CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END AS Status
                        FROM Employees
                        WHERE IsActive = 1
                        ORDER BY First_Name, Last_Name";

                    parameters = Array.Empty<SqlParameter>();
                }

                using var reader = _databaseService.ExecuteReader(sql, parameters);
                while (await reader.ReadAsync())
                {
                    employees.Add(new Employee
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Employee_ID")),
                        FullName = reader["FullName"].ToString() ?? "",
                        Position = reader["Position"]?.ToString() ?? "",
                        Department = reader["Department"]?.ToString() ?? "",
                        Status = reader["Status"].ToString() ?? "Active"
                    });
                }

                Console.WriteLine($"✅ Found {employees.Count} team members");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTeamMembersAsync error: {ex.Message}");
            }

            return employees;
        }

        // ============================================================
        // ===== ATTENDANCE =====
        // ============================================================

        public async Task<List<AttendanceRecord>> GetTeamAttendanceAsync(int supervisorId, DateTime? date = null)
        {
            var records = new List<AttendanceRecord>();
            var targetDate = date ?? DateTime.Today;

            try
            {
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
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId
                      AND a.Date = @Date
                    ORDER BY a.Status, e.First_Name";

                var parameters = new[]
                {
                    new SqlParameter("@Date", targetDate),
                    new SqlParameter("@SupervisorId", supervisorId)
                };

                using var reader = _databaseService.ExecuteReader(sql, parameters);
                while (await reader.ReadAsync())
                {
                    records.Add(MapAttendanceRecord(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTeamAttendanceAsync error: {ex.Message}");
            }

            return records;
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

            try
            {
                string sql = @"
                    SELECT 
                        a.Status,
                        COUNT(*) AS Count
                    FROM Attendance a
                    INNER JOIN Employees e ON a.Employee_ID = e.Employee_ID
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId
                      AND a.Date = @Date
                    GROUP BY a.Status";

                var parameters = new[]
                {
                    new SqlParameter("@Date", date),
                    new SqlParameter("@SupervisorId", supervisorId)
                };

                using var reader = _databaseService.ExecuteReader(sql, parameters);
                while (await reader.ReadAsync())
                {
                    var status = reader["Status"].ToString() ?? "";
                    var count = Convert.ToInt32(reader["Count"]);

                    if (stats.ContainsKey(status))
                    {
                        stats[status] = count;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetAttendanceStatsAsync error: {ex.Message}");
            }

            return stats;
        }

        private async Task<List<WeeklyAttendance>> GetWeeklyAttendanceDataAsync(int supervisorId)
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
            var records = new List<AttendanceRecord>();

            try
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
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId
                    ORDER BY a.Date DESC, a.CheckInTime DESC";

                var parameters = new[]
                {
                    new SqlParameter("@Count", count),
                    new SqlParameter("@SupervisorId", supervisorId)
                };

                using var reader = _databaseService.ExecuteReader(sql, parameters);
                while (await reader.ReadAsync())
                {
                    records.Add(MapAttendanceRecord(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetRecentAttendanceAsync error: {ex.Message}");
            }

            return records;
        }

        // ============================================================
        // ===== LEAVE REQUESTS =====
        // ============================================================

        public async Task<List<LeaveRequest>> GetTeamLeaveRequestsAsync(int supervisorId, string? status = null)
        {
            var requests = new List<LeaveRequest>();

            try
            {
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
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId";

                if (!string.IsNullOrEmpty(status))
                {
                    sql += " AND l.Status = @Status";
                }

                sql += " ORDER BY l.SubmittedDate DESC";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@SupervisorId", supervisorId)
                };

                if (!string.IsNullOrEmpty(status))
                {
                    parameters.Add(new SqlParameter("@Status", status));
                }

                using var reader = _databaseService.ExecuteReader(sql, parameters.ToArray());
                while (await reader.ReadAsync())
                {
                    requests.Add(MapLeaveRequest(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTeamLeaveRequestsAsync error: {ex.Message}");
            }

            return requests;
        }

        public async Task<bool> ApproveLeaveAsync(int leaveId)
        {
            try
            {
                string sql = "UPDATE LeaveRequests SET Status = 'Approved' WHERE Leave_ID = @LeaveId";
                var parameters = new[] { new SqlParameter("@LeaveId", leaveId) };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ApproveLeaveAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RejectLeaveAsync(int leaveId)
        {
            try
            {
                string sql = "UPDATE LeaveRequests SET Status = 'Rejected' WHERE Leave_ID = @LeaveId";
                var parameters = new[] { new SqlParameter("@LeaveId", leaveId) };

                var result = await Task.Run(() => _databaseService.ExecuteNonQuery(sql, parameters));
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ RejectLeaveAsync error: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // ===== PRODUCTIVITY =====
        // ============================================================

        public async Task<List<ProductivityRecord>> GetTeamProductivityAsync(int supervisorId)
        {
            var records = new List<ProductivityRecord>();

            try
            {
                string sql = @"
                    SELECT TOP 4
                        DATEPART(week, p.Date) AS WeekNumber,
                        AVG(CAST(p.ToiletsProduced AS FLOAT) / NULLIF(p.DailyTarget, 0) * 100) AS AvgProductivity,
                        MIN(p.Date) AS WeekStart
                    FROM Productivity p
                    INNER JOIN Employees e ON p.Employee_ID = e.Employee_ID
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId
                      AND p.DailyTarget > 0
                      AND p.Date >= DATEADD(week, -4, GETDATE())
                    GROUP BY DATEPART(week, p.Date), DATEPART(year, p.Date)
                    ORDER BY WeekStart ASC";

                var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };

                var tempRecords = new List<(int WeekNumber, double Actual, DateTime WeekStart)>();

                using var reader = _databaseService.ExecuteReader(sql, parameters);
                while (await reader.ReadAsync())
                {
                    tempRecords.Add((
                        Convert.ToInt32(reader["WeekNumber"]),
                        reader["AvgProductivity"] != DBNull.Value ? Convert.ToDouble(reader["AvgProductivity"]) : 0,
                        reader["WeekStart"] != DBNull.Value ? Convert.ToDateTime(reader["WeekStart"]) : DateTime.Today
                    ));
                }

                // Build 4-week display (pad with zeros)
                for (int i = 0; i < 4; i++)
                {
                    if (i < tempRecords.Count)
                    {
                        records.Add(new ProductivityRecord
                        {
                            Week = $"Week {i + 1}",
                            Target = 90,
                            Actual = Math.Round(tempRecords[i].Actual, 1)
                        });
                    }
                    else
                    {
                        records.Add(new ProductivityRecord
                        {
                            Week = $"Week {i + 1}",
                            Target = 90,
                            Actual = 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTeamProductivityAsync error: {ex.Message}");

                // Return 4 zero-weeks
                for (int i = 1; i <= 4; i++)
                {
                    records.Add(new ProductivityRecord
                    {
                        Week = $"Week {i}",
                        Target = 90,
                        Actual = 0
                    });
                }
            }

            return records;
        }

        public async Task<int> GetTotalTasksCompletedAsync(int supervisorId)
        {
            try
            {
                string sql = @"
                    SELECT ISNULL(SUM(p.ToiletsProduced), 0)
                    FROM Productivity p
                    INNER JOIN Employees e ON p.Employee_ID = e.Employee_ID
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId
                      AND p.Date >= DATEADD(month, -1, GETDATE())";

                var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };
                var result = await Task.Run(() => _databaseService.ExecuteScalar(sql, parameters));
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTotalTasksCompletedAsync error: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> GetTotalHoursWorkedAsync(int supervisorId)
        {
            try
            {
                string sql = @"
                    SELECT ISNULL(SUM(DATEDIFF(HOUR, a.CheckInTime, a.CheckOutTime)), 0)
                    FROM Attendance a
                    INNER JOIN Employees e ON a.Employee_ID = e.Employee_ID
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId
                      AND a.Date >= DATEADD(week, -1, GETDATE())
                      AND a.CheckInTime IS NOT NULL
                      AND a.CheckOutTime IS NOT NULL";

                var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };
                var result = await Task.Run(() => _databaseService.ExecuteScalar(sql, parameters));
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTotalHoursWorkedAsync error: {ex.Message}");
                return 0;
            }
        }

        public async Task<string> GetTopPerformerAsync(int supervisorId)
        {
            try
            {
                string sql = @"
                    SELECT TOP 1
                        e.First_Name + ' ' + e.Last_Name AS FullName,
                        AVG(CAST(p.ToiletsProduced AS FLOAT) / NULLIF(p.DailyTarget, 0) * 100) AS Productivity
                    FROM Productivity p
                    INNER JOIN Employees e ON p.Employee_ID = e.Employee_ID
                    INNER JOIN Users u ON u.Department = e.Department
                    WHERE u.User_ID = @SupervisorId
                      AND p.DailyTarget > 0
                      AND p.Date >= DATEADD(month, -1, GETDATE())
                    GROUP BY e.First_Name, e.Last_Name
                    ORDER BY Productivity DESC";

                var parameters = new[] { new SqlParameter("@SupervisorId", supervisorId) };
                using var reader = _databaseService.ExecuteReader(sql, parameters);

                if (await reader.ReadAsync())
                {
                    var name = reader["FullName"]?.ToString();
                    if (!string.IsNullOrEmpty(name)) return name;
                }

                return "N/A";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetTopPerformerAsync error: {ex.Message}");
                return "N/A";
            }
        }

        // ============================================================
        // ===== MAPPING HELPERS =====
        // ============================================================

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
                    ? reader.GetDateTime(reader.GetOrdinal("CheckInTime")).TimeOfDay
                    : null,
                CheckOut = reader["CheckOutTime"] != DBNull.Value
                    ? reader.GetDateTime(reader.GetOrdinal("CheckOutTime")).TimeOfDay
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
                Initials = string.Concat(
                    (reader["EmployeeName"].ToString() ?? "")
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(w => w.Length > 0 ? w[0].ToString() : "")
                ).ToUpper(),
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