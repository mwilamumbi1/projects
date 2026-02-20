using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly HRDataContext _context;

        public DepartmentController(HRDataContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        [HttpGet("GetDepartmentManagers")]
        public async Task<IActionResult> GetDepartmentManagers()
        {
            var result = await _context.Set<DepartmentManagerDto>()
                .FromSqlRaw("EXEC HR.GetDepartmentManagers")
                .ToListAsync();

            return Ok(result);
        }

        [HttpPost("AddDepartment")]
        public async Task<IActionResult> AddDepartment([FromBody] AddDepartmentDto dto)
        {
            var parameters = new[]
            {
                new SqlParameter("@Name", string.IsNullOrWhiteSpace(dto.Name) ? (object)DBNull.Value : dto.Name.Trim()),
                new SqlParameter("@ManagerFirstName", string.IsNullOrWhiteSpace(dto.ManagerFirstName) ? (object)DBNull.Value : dto.ManagerFirstName.Trim()),
                new SqlParameter("@ManagerLastName", string.IsNullOrWhiteSpace(dto.ManagerLastName) ? (object)DBNull.Value : dto.ManagerLastName.Trim())
            };

            var currentUserId = GetCurrentUserId();

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var setContextCmd = connection.CreateCommand();
                setContextCmd.CommandText = "EXEC sp_set_session_context @key, @value";

                var keyParam = setContextCmd.CreateParameter();
                keyParam.ParameterName = "@key";
                keyParam.Value = "UserID";
                setContextCmd.Parameters.Add(keyParam);

                var valueParam = setContextCmd.CreateParameter();
                valueParam.ParameterName = "@value";
                valueParam.Value = currentUserId;
                setContextCmd.Parameters.Add(valueParam);

                await setContextCmd.ExecuteNonQueryAsync();

                var result = await _context.Set<AddDepartmentResult>()
                    .FromSqlRaw("EXEC [HR].[AddDepartment] @Name, @ManagerFirstName, @ManagerLastName", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { error = "Unexpected error: No result from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { error = spResult.Message });

                return Ok(new { message = spResult.Message });
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error in AddDepartment: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in AddDepartment: {ex.Message}");
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpDelete("DeleteDepartment")]
        public async Task<IActionResult> DeleteDepartment([FromBody] DeleteDepartmentDto dto)
        {
            var parameters = new[] { new SqlParameter("@DepartmentID", dto.DepartmentID) };
            var currentUserId = GetCurrentUserId();

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var setContextCmd = connection.CreateCommand();
                setContextCmd.CommandText = "EXEC sp_set_session_context @key, @value";

                var keyParam = setContextCmd.CreateParameter();
                keyParam.ParameterName = "@key";
                keyParam.Value = "UserID";
                setContextCmd.Parameters.Add(keyParam);

                var valueParam = setContextCmd.CreateParameter();
                valueParam.ParameterName = "@value";
                valueParam.Value = currentUserId;
                setContextCmd.Parameters.Add(valueParam);

                await setContextCmd.ExecuteNonQueryAsync();

                var result = await _context.Set<DeleteDepartmentResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteDepartment] @DepartmentID", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { error = "Unexpected error: No result returned from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { error = spResult.Message });

                return Ok(new { message = spResult.Message });
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error in DeleteDepartment: {ex.Message}");
                return StatusCode(500, new { error = $"SQL error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in DeleteDepartment: {ex.Message}");
                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPut("UpdateDepartment")]
        public async Task<IActionResult> UpdateDepartment([FromBody] UpdateDepartmentDto dto)
        {
            if (dto.DepartmentID <= 0)
            {
                return BadRequest(new { success = false, message = "Department ID must be provided and valid." });
            }

            var parameters = new[]
            {
                new SqlParameter("@DepartmentID", dto.DepartmentID),
                new SqlParameter("@Name", string.IsNullOrWhiteSpace(dto.Name) ? (object)DBNull.Value : dto.Name.Trim()),
                new SqlParameter("@ManagerID", dto.ManagerID.HasValue ? dto.ManagerID.Value : (object)DBNull.Value)
            };

            var currentUserId = GetCurrentUserId();

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var setContextCmd = connection.CreateCommand();
                setContextCmd.CommandText = "EXEC sp_set_session_context @key, @value";

                var keyParam = setContextCmd.CreateParameter();
                keyParam.ParameterName = "@key";
                keyParam.Value = "UserID";
                setContextCmd.Parameters.Add(keyParam);

                var valueParam = setContextCmd.CreateParameter();
                valueParam.ParameterName = "@value";
                valueParam.Value = currentUserId;
                setContextCmd.Parameters.Add(valueParam);

                await setContextCmd.ExecuteNonQueryAsync();

                var result = await _context.Set<UpdateDepartmentResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateDepartment] @DepartmentID, @Name, @ManagerID", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "Unexpected error: No result from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                return Ok(new { success = true, message = spResult.Message });
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error in UpdateDepartment: {ex.Message}");
                return StatusCode(500, new { success = false, message = $"A database error occurred: {ex.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in UpdateDepartment: {ex.Message}");
                return StatusCode(500, new { success = false, message = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPost("GetDepartmentByName")]
        public async Task<IActionResult> GetDepartmentByName([FromBody] GetDepartmentByNameDto dto)
        {
            var param = new SqlParameter("@DepartmentName",
                string.IsNullOrWhiteSpace(dto.DepartmentName) ? DBNull.Value : dto.DepartmentName.Trim());

            try
            {
                var result = await _context.Set<DepartmentLookupResult>()
                    .FromSqlRaw("EXEC [HR].[GetDepartmentByName] @DepartmentName", param)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { error = "No result from stored procedure." });

                if (!spResult.Success)
                    return NotFound(new { message = spResult.Message });

                return Ok(spResult);
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error in GetDepartmentByName: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in GetDepartmentByName: {ex.Message}");
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }
    }
}


namespace Core.HumanResourceManagementApi.Models
{
    public class UpdateDepartmentDto
    {
        public int DepartmentID { get; set; }
        public string? Name { get; set; }
        public int? ManagerID { get; set; }
    }

    public class UpdateDepartmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AddDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string ManagerFirstName { get; set; } = string.Empty;
        public string ManagerLastName { get; set; } = string.Empty;
    }

    public class AddDepartmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteDepartmentDto
    {
        public int DepartmentID { get; set; }
    }

    public class DeleteDepartmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GetDepartmentByNameDto
    {
        public string DepartmentName { get; set; } = string.Empty;
    }

    public class DepartmentLookupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? DepartmentID { get; set; }
        public string? Name { get; set; }
        public int? ManagerID { get; set; }
    }

    public class DepartmentManagerDto
    {
        public int DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? ManagerName { get; set; }
        public int ManagerID { get; set; }
        public int EmployeeCount { get; set; }
        public string? EmployeeNames { get; set; }
    }
}
