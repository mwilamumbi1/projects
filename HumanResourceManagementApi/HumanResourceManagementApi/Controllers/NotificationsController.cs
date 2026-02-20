using Core.HumanResourceManagementApi;
using Core.HumanResourceManagementApi.Controllers;
using Core.HumanResourceManagementApi.Controllers.ERP.DTOs.HR;
using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly HRDataContext _context;

        public NotificationsController(HRDataContext context)
        {
            _context = context;
        }

        // POST: api/Notifications/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@ActionType", dto.ActionType),
                    new SqlParameter("@PrimaryKeyValue", dto.PrimaryKeyValue),
                    new SqlParameter("@Message", dto.Message),
                    new SqlParameter("@ActorID", dto.ActorID),
                    new SqlParameter("@TargetEmployeeID", (object?)dto.TargetEmployeeID ?? DBNull.Value)
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.CreateNotification @ActionType, @PrimaryKeyValue, @Message, @ActorID, @TargetEmployeeID",
                    parameters
                );

                return Ok(new { success = true, message = "Notification created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // GET: api/Notifications/employee/1025?isRead=false
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByEmployee(int employeeId, [FromQuery] bool? isRead = null)
        {
            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", employeeId),
                new SqlParameter("@IsRead", (object?)isRead ?? DBNull.Value)
            };

            var results = await _context.Notifications
                .FromSqlRaw("EXEC HR.GetNotificationsByEmployee @EmployeeID, @IsRead", parameters)
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/Notifications/role/HR_Manager?isRead=false
        [HttpGet("role/{roleName}")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByRole(string roleName, [FromQuery] bool? isRead = null)
        {
            var parameters = new[]
            {
                new SqlParameter("@RoleName", roleName),
                new SqlParameter("@IsRead", (object?)isRead ?? DBNull.Value)
            };

            var results = await _context.Notifications
                .FromSqlRaw("EXEC HR.GetNotificationsByRole @RoleName, @IsRead", parameters)
                .ToListAsync();

            return Ok(results);
        }

        [HttpPost("mark-as-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkNotificationAsReadDto dto)
        {
            try
            {
                var parameters = new[]
                {
            new SqlParameter("@NotificationID", dto.NotificationId),
            new SqlParameter("@EmployeeID", dto.EmployeeId)
        };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.MarkNotificationAsRead @NotificationID, @EmployeeID",
                    parameters
                );

                return Ok(new { success = true, message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

    }
}

namespace Core.HumanResourceManagementApi.Controllers.ERP.DTOs.HR
{
    // DTO for creating notifications
    public class CreateNotificationDto
    {
        public string ActionType { get; set; } = string.Empty;
        public int PrimaryKeyValue { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ActorID { get; set; }
        public int? TargetEmployeeID { get; set; }
    }

    // DTO for notifications returned by GetNotificationsByEmployee and GetNotificationsByRole
    public class NotificationDto
    {
        public Guid notification_id { get; set; }   // matches n.notification_id
        public string type { get; set; } = string.Empty;   // matches n.type
        public string message { get; set; } = string.Empty; // matches n.message
        public int actor_id { get; set; }          // matches n.actor_id
        public DateTime created_at { get; set; }   // matches n.created_at
        public int? employee_id { get; set; }      // matches r.recipient_id AS employee_id
        public bool is_read { get; set; }          // matches r.is_read
    }

    public class MarkNotificationAsReadDto
    {
        public Guid NotificationId { get; set; }  // The notification to mark as read
        public int EmployeeId { get; set; }       // The employee marking it as read
    }
}