using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PositionController : ControllerBase
    {
        private readonly HRDataContext _context;

        public PositionController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("AllPositions")]
        public async Task<ActionResult<IEnumerable<PositionWithEmployeesDto>>> GetAllPositions()
        {
            try
            {
                var positions = await _context.PositionsWithEmployees
                    .FromSqlRaw("EXEC HR.GetAllPositions")
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(positions);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpGet("GetPositionById{id}")]
        public async Task<IActionResult> GetPositionById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "PositionID must be a positive integer." });
            }

            var parameter = new SqlParameter("@PositionID", id);

            try
            {
                var result = await _context.Set<GetPositionByIdResult>()
                    .FromSqlRaw("EXEC [HR].[GetPositionByID] @PositionID", parameter)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return NotFound(new { error = $"No position found with ID {id}." });

                if (!spResult.Success)
                    return BadRequest(new { error = spResult.Message });

                return Ok(new
                {
                    spResult.PositionID,
                    spResult.Title,
                    spResult.Description,
                    spResult.Message
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPut("UpdatePosition")]
        public async Task<IActionResult> UpdatePosition([FromBody] UpdatePositionDto dto)
        {
            if (dto.PositionID <= 0)
            {
                return BadRequest(new { success = false, message = "PositionID must be a positive integer." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@PositionID", dto.PositionID),
                new SqlParameter("@Title", string.IsNullOrWhiteSpace(dto.Title) ? (object)DBNull.Value : dto.Title.Trim()),
                new SqlParameter("@Description", string.IsNullOrWhiteSpace(dto.Description) ? (object)DBNull.Value : dto.Description.Trim())
            };

            try
            {
                // Open connection manually
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Set SESSION_CONTEXT UserID
                using (var setContextCmd = connection.CreateCommand())
                {
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
                }

                var result = await _context.UpdatePositionResults
                    .FromSqlRaw("EXEC [HR].[UpdatePosition] @PositionID, @Title, @Description", parameters)
                    .AsNoTracking()
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { success = false, message = "No result returned from stored procedure." });
                }

                if (!spResult.Success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = spResult.Message,
                        positionId = spResult.PositionID,
                        title = spResult.Title,
                        description = spResult.Description
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = spResult.Message,
                    positionId = spResult.PositionID,
                    title = spResult.Title,
                    description = spResult.Description
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"A database error occurred: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        [HttpPost("AddPosition")]
        public async Task<IActionResult> AddPosition([FromBody] AddPositionDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@Title", string.IsNullOrWhiteSpace(dto.Title) ? (object)DBNull.Value : dto.Title.Trim()),
                new SqlParameter("@Description", string.IsNullOrWhiteSpace(dto.Description) ? (object)DBNull.Value : dto.Description!.Trim())
            };

            try
            {
                // Open connection manually
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Set SESSION_CONTEXT UserID
                using (var setContextCmd = connection.CreateCommand())
                {
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
                }

                var result = await _context.Set<AddPositionResult>()
                    .FromSqlRaw("EXEC [HR].[AddPosition] @Title, @Description", parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { error = "No result returned from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { error = spResult.Message });

                return Ok(new { message = spResult.Message });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });
            }
        }

        [HttpDelete("DeletePosition/{positionId:int}")]
        public async Task<IActionResult> DeletePosition([FromRoute(Name = "positionId")] int positionId)
        {
            if (positionId <= 0)
                return BadRequest(new { success = false, message = "A valid Position ID is required." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var param = new SqlParameter("@PositionID", positionId);

            try
            {
                // Open connection manually
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Set SESSION_CONTEXT UserID
                using (var setContextCmd = connection.CreateCommand())
                {
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
                }

                var result = await _context.Set<DeleteResult>()
                    .FromSqlRaw("EXEC [HR].[DeletePosition] @PositionID", param)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { error = "Unexpected error: No result from stored procedure." });
                }

                if (!spResult.Success)
                {
                    return BadRequest(new { error = spResult.Message });
                }

                return Ok(new { message = spResult.Message });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }
    }
}

public class DeletePositionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PositionWithEmployeesDto
{
    public int PositionID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EmployeeNames { get; set; } // Comma-separated employee full names
}

public class DeletePositionDto
{
    public int PositionID { get; set; }
}

public class GetPositionByNameDto
{
    public string PositionName { get; set; } = string.Empty;
}

public class GetPositionByIdDto
{
    public int PositionID { get; set; }
}

public class GetPositionByIdResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int PositionID { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
}

public class AddPositionDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AddPositionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

[Keyless]
public class UpdatePositionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? PositionID { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class UpdatePositionDto
{
    public int PositionID { get; set; } // Required
    public string? Title { get; set; }  // Optional
    public string? Description { get; set; }  // Optional
}