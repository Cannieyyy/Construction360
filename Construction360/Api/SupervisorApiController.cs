using Construction360.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction360.Controllers.Api
{
    /// <summary>
    /// Supervisor API endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize(Roles = "Supervisor,Admin")]
    public class SupervisorApiController : ControllerBase
    {
        private readonly ISupervisorRepository _supervisorRepository;

        public SupervisorApiController(ISupervisorRepository supervisorRepository)
        {
            _supervisorRepository = supervisorRepository;
        }

        /// <summary>
        /// Get dashboard data for supervisor
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
                return Unauthorized(new { message = "Invalid user session" });

            var data = await _supervisorRepository.GetDashboardDataAsync(supervisorId);
            return Ok(data);
        }

        /// <summary>
        /// Get team attendance for today
        /// </summary>
        [HttpGet("attendance")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAttendance()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
                return Unauthorized(new { message = "Invalid user session" });

            var attendance = await _supervisorRepository.GetTeamAttendanceAsync(supervisorId);
            return Ok(attendance);
        }

        /// <summary>
        /// Get pending leave requests
        /// </summary>
        [HttpGet("leave-requests")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLeaveRequests([FromQuery] string status = "Pending")
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
                return Unauthorized(new { message = "Invalid user session" });

            var leaves = await _supervisorRepository.GetTeamLeaveRequestsAsync(supervisorId, status);
            return Ok(leaves);
        }

        /// <summary>
        /// Approve a leave request
        /// </summary>
        /// <param name="id">Leave request ID</param>
        [HttpPost("leave-requests/{id}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var result = await _supervisorRepository.ApproveLeaveAsync(id);
            if (!result)
                return NotFound(new { message = "Leave request not found" });

            return Ok(new { message = "Leave approved successfully" });
        }

        /// <summary>
        /// Reject a leave request
        /// </summary>
        /// <param name="id">Leave request ID</param>
        [HttpPost("leave-requests/{id}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RejectLeave(int id)
        {
            var result = await _supervisorRepository.RejectLeaveAsync(id);
            if (!result)
                return NotFound(new { message = "Leave request not found" });

            return Ok(new { message = "Leave rejected successfully" });
        }

        /// <summary>
        /// Get team productivity data
        /// </summary>
        [HttpGet("productivity")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductivity()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int supervisorId))
                return Unauthorized(new { message = "Invalid user session" });

            var productivity = await _supervisorRepository.GetTeamProductivityAsync(supervisorId);
            return Ok(productivity);
        }
    }
}