using Core.HumanResourceManagementApi.DTOs; // Assuming this namespace holds your DTOs
using Core.HumanResourceManagementApi.Models; // Assuming this namespace also holds models/DTOs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data; // Required for CommandType if you were using raw ADO.NET, but not strictly needed with ExecuteSqlRawAsync
using System.Linq;
using System.Threading.Tasks; // Ensure Task is available

namespace Core.HumanResourceManagementApi.Controllers
{
    // --- RolesController ---
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly HRDataContext _context; // Your context type is HRDataContext

        public RolesController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetRoles")]
        public async Task<IActionResult> GetallRoles()
        {
            try
            {
                // Ensure GetallRolesDto includes Description if core.GetRoles SP returns it
                // Based on your DTO definition, it does.
                var roles = await _context.Set<GetallRolesDto>()
                    .FromSqlRaw("EXEC core.GetRoles")
                    .ToListAsync();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetRoles: {ex.Message}");
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = $"An error occurred while retrieving roles: {ex.Message}"
                });
            }
        }

        [HttpPost("AddRoles")]
        public async Task<ActionResult<ResponseDto>> AddRole([FromBody] AddRoleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RoleName))
            {
                return BadRequest(new ResponseDto
                {
                    Success = false,
                    Message = "RoleName cannot be empty."
                });
            }

            try
            {
                var roleNameParam = new SqlParameter("@RoleName", dto.RoleName);
                var descParam = new SqlParameter("@Description", (object)dto.Description ?? DBNull.Value);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [core].[AddRole] @RoleName, @Description",
                    roleNameParam, descParam
                );

                return Ok(new ResponseDto
                {
                    Success = true,
                    Message = "Role added successfully."
                });
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50000 || ex.Number >= 50000)
                {
                    return BadRequest(new ResponseDto
                    {
                        Success = false,
                        Message = ex.Message.Contains("Error: ") ? ex.Message.Replace("Error: ", "") : ex.Message
                    });
                }
                Console.Error.WriteLine($"SqlException in AddRole: {ex.Message} (Error Number: {ex.Number})");
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = $"A database error occurred: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in AddRole: {ex.Message}");
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        [HttpPut("UpdateRoles")]
        public async Task<ActionResult<ResponseDto>> UpdateRole([FromBody] UpdateRoleDto dto)
        {
            if (dto.RoleID <= 0)
            {
                return BadRequest(new ResponseDto { Success = false, Message = "Invalid Role ID." });
            }
            if (string.IsNullOrWhiteSpace(dto.RoleName))
            {
                return BadRequest(new ResponseDto { Success = false, Message = "RoleName cannot be empty." });
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@RoleID", dto.RoleID),
                    new SqlParameter("@RoleName", dto.RoleName),
                    new SqlParameter("@Description", (object)dto.Description ?? DBNull.Value)
                };

                await _context.Database.ExecuteSqlRawAsync("EXEC [core].[UpdateRole] @RoleID, @RoleName, @Description", parameters);

                return Ok(new ResponseDto
                {
                    Success = true,
                    Message = "Role updated successfully."
                });
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50001)
                {
                    return NotFound(new ResponseDto
                    {
                        Success = false,
                        Message = $"Role with ID {dto.RoleID} not found."
                    });
                }
                else if (ex.Number == 50002 || ex.Number >= 50000)
                {
                    return BadRequest(new ResponseDto
                    {
                        Success = false,
                        Message = ex.Message.Contains("Error: ") ? ex.Message.Replace("Error: ", "") : ex.Message
                    });
                }
                Console.Error.WriteLine($"SqlException in UpdateRole: {ex.Message} (Error Number: {ex.Number})");
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = $"A database error occurred: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in UpdateRole: {ex.Message}");
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = $"An unexpected error occurred: {ex.Message}"
                });
            }


        }
        [HttpDelete("DeleteRole")]
        public async Task<ActionResult<ResponseDto>> DeleteRole([FromBody] DeleteRoleDto dto)
        {
            if (dto == null || dto.RoleID <= 0)
            {
                return BadRequest(new ResponseDto
                {
                    Success = false,
                    Message = "Invalid Role ID provided for deletion."
                });
            }

            try
            {
                var roleIdParam = new SqlParameter("@RoleID", dto.RoleID);

                // Execute the stored procedure
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC core.DeleteRole @RoleID",
                    roleIdParam
                );

                // If we reach here without exception, the deletion was successful
                return Ok(new ResponseDto
                {
                    Success = true,
                    Message = $"Role with ID {dto.RoleID} deleted successfully."
                });
            }
            catch (SqlException ex) when (ex.Number == 50000)
            {
                // Custom error from stored procedure (role not found)
                return NotFound(new ResponseDto
                {
                    Success = false,
                    Message = $"Role with ID {dto.RoleID} not found or does not exist."
                });
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine($"SqlException in DeleteRole: {ex.Message} (Error Number: {ex.Number})");
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = "A database error occurred while deleting the role."
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unexpected error in DeleteRole: {ex.Message}");
                return StatusCode(500, new ResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while deleting the role."
                });
            }
        }



    }

        public class AddRoleDto
    {
        [Required(ErrorMessage = "RoleName is required.")]
        [StringLength(100, ErrorMessage = "RoleName cannot exceed 100 characters.")]
        public string RoleName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }
    }

    public class UpdateRoleDto
    {
        [Required(ErrorMessage = "RoleID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "RoleID must be a positive integer.")]
        public int RoleID { get; set; }

        [Required(ErrorMessage = "RoleName is required.")]
        [StringLength(100, ErrorMessage = "RoleName cannot exceed 100 characters.")]
        public string RoleName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string Description { get; set; }
    }

    public class GetallRolesDto // Renamed from GetRolesDto to match your GetallRoles method
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; } // Include Description as per your original DTO if the SP returns it
    }

    public class ResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class DeleteRoleDto
    {
        public int RoleID { get; set; }
    }
 
}