using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Required for User claims
using System.Threading.Tasks;

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobOpeningsController : ControllerBase
    {
        private readonly HRDataContext _context;

        public JobOpeningsController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetAllJobOpenings")]
        public async Task<IActionResult> GetAllJobOpenings()
        {
            try
            {
                var results = await _context.Set<GetAllJobOpeningsResult>()
                                            .FromSqlRaw("EXEC HR.GetAllJobOpenings")
                                            .ToListAsync();

                if (!results.Any())
                {
                    return Ok(new List<GetAllJobOpeningsResult>());
                }

                // If the stored procedure returns a row indicating an error
                if (!results.First().Success)
                {
                    return StatusCode(500, new { error = results.First().Message });
                }

                return Ok(results);
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

        [HttpPost("InsertJobOpening")]
        public async Task<IActionResult> InsertJobOpening([FromBody] InsertJobOpeningDto jobOpening)
        {
            if (jobOpening == null)
            {
                return BadRequest(new { error = "Job opening data is required." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { error = "User ID claim is missing or invalid." });
            }

            var parameters = new[]
            {
                new SqlParameter("@Title", jobOpening.Title ?? (object)DBNull.Value),
                new SqlParameter("@DepartmentID", jobOpening.DepartmentID),
                new SqlParameter("@Location", jobOpening.Location ?? (object)DBNull.Value),
                new SqlParameter("@ClosingDate", jobOpening.ClosingDate),
                new SqlParameter("@JobDescription", jobOpening.JobDescription ?? (object)DBNull.Value)
            };

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Set UserID in SESSION_CONTEXT for auditing
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "EXEC sp_set_session_context @key, @value";
                    command.Parameters.Add(new SqlParameter("@key", "UserID"));
                    command.Parameters.Add(new SqlParameter("@value", currentUserId));
                    await command.ExecuteNonQueryAsync();
                }

                var spResult = (await _context.Set<InsertJobOpeningResultDto>()
                                             .FromSqlRaw("EXEC HR.InsertJobOpening @Title, @DepartmentID, @Location, @ClosingDate, @JobDescription", parameters)
                                             .ToListAsync())
                                             .FirstOrDefault();

                await connection.CloseAsync();

                if (spResult == null)
                {
                    return StatusCode(500, new { error = "Failed to insert job opening: No response from database procedure." });
                }

                if (spResult.Success)
                {
                    return Ok(new { spResult.Message, spResult.JobOpeningID });
                }
                else
                {
                    return BadRequest(new { error = spResult.Message });
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = $"Database operation failed: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        [HttpGet("GetJobOpeningById/{id}")]
        public async Task<IActionResult> GetJobOpeningById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "JobOpeningID must be a positive integer." });
            }
            try
            {
                var parameter = new SqlParameter("@JobOpeningID", id);
                var result = await _context.Set<GetJobOpeningByIdResult>()
                                         .FromSqlRaw("EXEC HR.GetJobOpeningByID @JobOpeningID", parameter)
                                         .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { error = "Unexpected error: No result returned from stored procedure." });

                if (!spResult.Success)
                    return NotFound(new { error = spResult.Message });

                return Ok(spResult);
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

        [HttpPut("toggle-status/{jobOpeningId}")]
        public async Task<IActionResult> ToggleJobOpeningStatus(int jobOpeningId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { error = "User ID claim is missing or invalid." });
            }

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Set UserID in SESSION_CONTEXT for auditing
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "EXEC sp_set_session_context @key, @value";
                    command.Parameters.Add(new SqlParameter("@key", "UserID"));
                    command.Parameters.Add(new SqlParameter("@value", currentUserId));
                    await command.ExecuteNonQueryAsync();
                }

                var param = new SqlParameter("@JobOpeningID", jobOpeningId);
                var affectedRows = await _context.Database.ExecuteSqlRawAsync("EXEC HR.ToggleJobOpeningStatus @JobOpeningID", param);

                await connection.CloseAsync();

                if (affectedRows == 0)
                {
                    return NotFound(new { message = $"Job opening with ID {jobOpeningId} not found." });
                }

                return Ok(new { message = "Job opening status toggled successfully." });
            }
            catch (SqlException ex)
            {
                // Capture custom errors from RAISERROR in the stored procedure
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        [HttpDelete("DeleteJobOpening/{jobOpeningId}")]
        public async Task<IActionResult> DeleteJobOpening(int jobOpeningId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { error = "User ID claim is missing or invalid." });
            }

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Set UserID in SESSION_CONTEXT for auditing
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "EXEC sp_set_session_context @key, @value";
                    command.Parameters.Add(new SqlParameter("@key", "UserID"));
                    command.Parameters.Add(new SqlParameter("@value", currentUserId));
                    await command.ExecuteNonQueryAsync();
                }

                var param = new SqlParameter("@JobOpeningID", jobOpeningId);
                var result = await _context.Database.ExecuteSqlRawAsync("EXEC HR.DeleteJobOpening @JobOpeningID", param);

                await connection.CloseAsync();

                if (result == 0)
                {
                    return NotFound(new { Success = false, Message = $"Job Opening with ID {jobOpeningId} not found." });
                }

                return Ok(new { Success = true, Message = $"Job Opening with ID {jobOpeningId} deleted successfully." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
        [HttpPut("UpdateJobOpening")]
        public async Task<IActionResult> UpdateJobOpening([FromBody] UpdateJobOpeningRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get current user ID for auditing
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { error = "User ID claim is missing or invalid." });
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@JobOpeningID", request.JobOpeningID),
                    new SqlParameter("@Title", request.Title ?? (object)DBNull.Value),
                    new SqlParameter("@DepartmentID", request.DepartmentID),
                    new SqlParameter("@Location", request.Location ?? (object)DBNull.Value),
                    new SqlParameter("@ClosingDate", (object?)request.ClosingDate ?? DBNull.Value),
                    new SqlParameter("@JobDescription", request.JobDescription ?? (object)DBNull.Value),
                    new SqlParameter("@StatusID", (object?)request.StatusID ?? DBNull.Value) // ✅ Added
                };

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // ✅ Set UserID in SESSION_CONTEXT for auditing
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "EXEC sp_set_session_context @key, @value";
                    command.Parameters.Add(new SqlParameter("@key", "UserID"));
                    command.Parameters.Add(new SqlParameter("@value", currentUserId));
                    await command.ExecuteNonQueryAsync();
                }

                // ✅ Call stored procedure with 7 parameters
                var affectedRows = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.UpdateJobOpening @JobOpeningID, @Title, @DepartmentID, @Location, @ClosingDate, @JobDescription, @StatusID",
                    parameters
                );

                await connection.CloseAsync();

                if (affectedRows == 0)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"Job Opening with ID {request.JobOpeningID} not found or no changes were made."
                    });
                }

                return Ok(new { Success = true, Message = "Job opening updated successfully." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }
    }



    public class UpdateJobOpeningRequest
    {
        public int JobOpeningID { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DepartmentID { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime? ClosingDate { get; set; }
        public string JobDescription { get; set; } = string.Empty;
        public int? StatusID { get; set; } // <-- added
    }


    public class InsertJobOpeningResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? JobOpeningID { get; set; }
    }

    public class InsertJobOpeningDto
    {
        public string Title { get; set; } = string.Empty; // Initialize to prevent null reference warnings
        public int DepartmentID { get; set; }
        public string Location { get; set; } = string.Empty; // Initialize to prevent null reference warnings
        public DateTime ClosingDate { get; set; }
        public string JobDescription { get; set; } = string.Empty; // Initialize to prevent null reference warnings
    }

    public class GetJobOpeningByIdDto
    {
        public int JobOpeningID { get; set; }
    }

    public class GetJobOpeningByIdResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? JobOpeningID { get; set; }
        public string? Title { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? Location { get; set; }
        public int? StatusID { get; set; }
        public string? Name { get; set; }
        public DateTime? PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string? JobDescription { get; set; }
    }

    public class GetAllJobOpeningsResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? JobOpeningID { get; set; }
        public string? Title { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public string? Location { get; set; }
        public int? StatusID { get; set; }
        public string? Name { get; set; }
        public DateTime? PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public string? JobDescription { get; set; }  
    }

    public class ToggleJobStatusRequest
    {
        public int JobOpeningID { get; set; }
    }

    public class DeleteJobOpeningResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}