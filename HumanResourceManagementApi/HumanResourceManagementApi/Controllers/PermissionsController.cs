using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly HRDataContext _context;

        public PermissionsController(HRDataContext context)
        {
            _context = context;
        }

        // GET api/permissions/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<PermissionDto>>> GetPermissionsByUserId(int userId)
        {
            var permissions = await _context.Permissions
                .FromSqlRaw("EXEC [core].[GetPermissionsByUserId] @UserID = {0}", userId)
                .ToListAsync();

            if (permissions == null || permissions.Count == 0)
            {
                return NotFound("No permissions found for this user.");
            }

            return permissions;
        }

        [HttpPost("AddPermissions")]
        public async Task<ActionResult<ResponseDto>> AddRolePermission([FromBody] RolePermissionDto dto)
        {
            try
            {
                var roleIdParam = new SqlParameter("@RoleID", dto.RoleID);
                var permissionIdParam = new SqlParameter("@PermissionID", dto.PermissionID);
                var grantedByParam = new SqlParameter("@GrantedBy", dto.GrantedBy);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [core].[AddRolePermission] @RoleID, @PermissionID, @GrantedBy",
                    roleIdParam, permissionIdParam, grantedByParam);

                return Ok(new ResponseDto
                {
                    Success = true,
                    Message = "RolePermission added successfully."
                });
            }
            catch (SqlException ex) when (ex.Number == 50000) // Custom THROW error
            {
                return BadRequest(new ResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch
            {
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred."
                });
            }
        }

        [HttpPut("UpdateRolePermission")]
        public async Task<ActionResult<ResponseDto>> UpdateRolePermission([FromBody] UpdateRolePermissionDto dto)
        {
            try
            {
                var rolePermissionIdParam = new SqlParameter("@RolePermissionID", dto.RolePermissionID);
                var roleIdParam = new SqlParameter("@RoleID", dto.RoleID);
                var permissionIdParam = new SqlParameter("@PermissionID", dto.PermissionID);
                var grantedByParam = new SqlParameter("@GrantedBy", dto.GrantedBy);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [core].[UpdateRolePermission] @RolePermissionID, @RoleID, @PermissionID, @GrantedBy",
                    rolePermissionIdParam, roleIdParam, permissionIdParam, grantedByParam);

                return Ok(new ResponseDto
                {
                    Success = true,
                    Message = "RolePermission updated successfully."
                });
            }
            catch (SqlException ex) when (ex.Number == 50000) // Custom THROW message
            {
                return BadRequest(new ResponseDto
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred.",
                    
                });
            }
        }


        [HttpGet("GetPermissions")]
        public async Task<ActionResult<List<getPermissionDto>>> GetAllPermissions()
        {
            try
            {
                var Permit = await _context.Permit
                    .FromSqlRaw("EXEC [core].[GetAllPermissions]")
                    .ToListAsync();

                if (Permit == null || Permit.Count == 0)
                    return NotFound();

                return Permit;
            }
            catch
            {
                return StatusCode(500, "An error occurred while retrieving permissions.");
            }
        }


        [HttpGet("GetRolePermissionsWithDetails")]
        public async Task<ActionResult<IEnumerable<RolePermissionDetailDto>>> GetRolePermissionsWithDetails()
        {
            try
            {
                var connection = _context.Database.GetDbConnection();

                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "core.GetRolePermissionsWithDetails";
                command.CommandType = CommandType.StoredProcedure;

                var result = new List<RolePermissionDetailDto>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new RolePermissionDetailDto
                    {
                        RolePermissionID = reader.GetInt32(0),
                        RoleName = reader.GetString(1),
                        PermissionName = reader.GetString(2),
                        GrantedAt = reader.GetDateTime(3),
                        GrantedByUserName = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to retrieve role-permission details.",
                    Error = ex.Message
                });
            }
        }

        [HttpDelete("DeleteRolePermission")]
        public async Task<IActionResult> DeleteRolePermission([FromBody] DeleteRolePermissionDto dto)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = "core.DeleteRolePermission";
                command.CommandType = CommandType.StoredProcedure;

                var param = command.CreateParameter();
                param.ParameterName = "@RolePermissionID";
                param.Value = dto.RolePermissionID;
                command.Parameters.Add(param);

                using var reader = await command.ExecuteReaderAsync();

                // Read the first row returned by the SP
                if (await reader.ReadAsync())
                {
                    // Check if Success column exists and its value
                    var successOrdinal = reader.GetOrdinal("Success");
                    var success = reader.GetInt32(successOrdinal);

                    if (success == 1)
                    {
                        var message = reader["Message"].ToString();
                        return Ok(new
                        {
                            Success = true,
                            Message = message
                        });
                    }
                    else
                    {
                        var errorMessage = reader["ErrorMessage"].ToString();
                        return NotFound(new
                        {
                            Success = false,
                            Message = errorMessage
                        });
                    }
                }

                // If no rows returned (should not happen)
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Unexpected error: No response from database."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Failed to delete role permission.",
                    Error = ex.Message
                });
            }
        }


    }


}

public class UpdateRolePermissionDto
{
    public int RolePermissionID { get; set; }
    public int RoleID { get; set; }
    public int PermissionID { get; set; }
    public int GrantedBy { get; set; }
}



public class getPermissionDto
{
    public int PermissionID { get; set; }
    public string? PermissionName { get; set; }
    public string? Description { get; set; }
    public string? Module { get; set; }
}

public class PermissionDto
{
    public int UserID { get; set; }
    public string? PermissionNames { get; set; }
 
}

public class RolePermissionDto
{
    public int RoleID { get; set; }
    public int PermissionID { get; set; }
    public int GrantedBy { get; set; }
}

public class RolePermissionDetailDto
{
    public int RolePermissionID { get; set; }
    public string RoleName { get; set; }
    public string PermissionName { get; set; }
    public DateTime GrantedAt { get; set; }
    public string GrantedByUserName { get; set; }
}

public class ResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class DeleteRolePermissionDto
{
    public int RolePermissionID { get; set; }
}
