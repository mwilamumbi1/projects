using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Core.HumanResourceManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly HRDataContext _context;

        public UsersController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetUserDetails")]
        public async Task<IActionResult> GetUserDetails()
        {
            var result = await _context.Set<UserDetailsDto>()
                .FromSqlRaw("EXEC core.GetAllUserDetails")
                .ToListAsync();

            if (result == null || result.Count == 0)
            {
                return NotFound(new { success = false, message = "No user details found." });
            }

            return Ok(result);
        }
        [HttpPost("UpdateUserRole")]
    public async Task<IActionResult> UpdateUserRole([FromBody] UpdateUserRoleDto dto)
    {
        try
        {
            var userIdParam = new SqlParameter("@UserID", dto.UserID);
            var roleIdParam = new SqlParameter("@NewRoleID", dto.NewRoleID);
            var modifiedByParam = new SqlParameter("@ModifiedBy", dto.ModifiedBy);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC [core].[UpdateUserRole] @UserID, @NewRoleID, @ModifiedBy",
                userIdParam, roleIdParam, modifiedByParam
            );

            return Ok(new { success = true, message = "User role updated successfully." });
        }
        catch (SqlException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
        [HttpGet("active-inactive")]
        public async Task<ActionResult<IEnumerable<EmployeeStatusDto>>> GetActiveInactiveEmployees()
        {
            var result = await _context.EmployeeStatusDtos
                .FromSqlRaw("EXEC HR.GetActiveInactiveEmployees")
                .ToListAsync();

            return Ok(result);
        }
        [HttpPut("toggle-status/{employeeId}")]
        public async Task<IActionResult> ToggleEmployeeStatus(int employeeId)
        {
            var param = new SqlParameter("@EmployeeID", employeeId);

            try
            {
                await _context.Database.ExecuteSqlRawAsync("EXEC HR.ToggleEmployeeStatus @EmployeeID", param);
                return Ok(new { message = "Employee status toggled successfully." });
            }
            catch (SqlException ex)
            {
                // Handle errors raised by RAISERROR in the proc
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}


public class UpdateUserRoleDto
{
    public int UserID { get; set; }
    public int NewRoleID { get; set; }
    public int ModifiedBy { get; set; }
}

public class UserDetailsDto
    {   
        public int UserID { get; set; }
        public string UserFullName { get; set; }
        public string Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string RoleName { get; set; }
        public string StatusName { get; set; }
        public string EmployeeName { get; set; }
    }
public class EmployeeStatusDto

{
    public int EmployeeID { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string Name { get; set; } = null!;        // Status name (Active/Inactive)
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}
public class ToggleEmployeeStatusDto
{
    public int EmployeeID { get; set; }
}
