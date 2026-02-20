using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingController : ControllerBase
    {
        private readonly HRDataContext _context;

        public TrainingController(HRDataContext context)
        {
            _context = context;
        }

        // Property to get current user ID from claims or default value
        private int CurrentUserId
        {
            get
            {
                // Try to get user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("userId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
                // Return default value if no user ID found (you might want to throw an exception instead)
                return 1; // or throw new UnauthorizedAccessException("User ID not found in claims");
            }
        }

        [HttpGet("{trainingId}")]
        public async Task<IActionResult> GetTrainingById(int trainingId)
        {
            if (trainingId <= 0)
            {
                return BadRequest(new { error = "TrainingID must be a positive integer." });
            }

            var parameter = new SqlParameter("@TrainingID", trainingId);

            try
            {
                var result = await _context.Set<GetTrainingByIdResult>()
                    .FromSqlRaw("EXEC HR.GetTrainingByID @TrainingID", parameter)
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

        [HttpGet("GetTrainingInfo")]
        public async Task<IActionResult> GetPersonalInfo()
        {
            var employees = await _context.Set<TrainingDto>()
                .FromSqlRaw("EXEC HR.GetAllTraining")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpDelete("Deletetraining")]
        public async Task<IActionResult> DeleteTraining([FromBody] DeleteTrainingDto dto)
        {
            var param = new SqlParameter("@TrainingID", dto.TrainingID);

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
                    valueParam.Value = CurrentUserId; // Fixed: using CurrentUserId property
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync(); // Fixed: missing ExecuteNonQueryAsync
                }

                var result = await _context.Set<DeleteResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteTraining] @TrainingID", param)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { error = "No result returned from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { error = spResult.Message });

                return Ok(new { message = spResult.Message });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected Error: " + ex.Message });
            }
            finally
            {
                // Ensure connection is closed
                if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
                {
                    await _context.Database.GetDbConnection().CloseAsync();
                }
            }
        }

        [HttpPost("AddTraining")]
        public async Task<IActionResult> Training([FromBody] AddTrainingDto dto)
        {
            var parameters = new[]
            {
                new SqlParameter("@Title", (object)dto.Title ?? DBNull.Value),
                new SqlParameter("@StartDate", dto.StartDate),
                new SqlParameter("@EndDate", dto.EndDate.HasValue ? (object)dto.EndDate.Value : DBNull.Value),
                new SqlParameter("@Description", (object)dto.Description ?? DBNull.Value)
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
                    valueParam.Value = CurrentUserId; // Fixed: using CurrentUserId property
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync(); // Fixed: missing ExecuteNonQueryAsync
                }

                var result = await _context.Set<AddTrainingResult>()
                    .FromSqlRaw("EXEC HR.AddTraining @Title, @StartDate, @EndDate, @Description", parameters)
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
                return StatusCode(500, new { error = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });
            }
            finally
            {
                // Ensure connection is closed
                if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
                {
                    await _context.Database.GetDbConnection().CloseAsync();
                }
            }
        }

        [HttpDelete("DeleteEmployeeTraining")]
        public async Task<IActionResult> DeleteEmployeeTraining([FromBody] DeleteEmployeeTrainingDto dto)
        {
            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", dto.EmployeeID),
                new SqlParameter("@TrainingID", dto.TrainingID)
            };

            try
            {
                var result = await _context.Set<DeleteEmployeeTrainingResult>()
                    .FromSqlRaw("EXEC HR.DeleteEmployeeTraining @EmployeeID, @TrainingID", parameters)
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
                return StatusCode(500, new { error = $"SQL Error: {ex.Message}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpPut("UpdateEmployeeTraining")]
        public async Task<IActionResult> UpdateEmployeeTraining([FromBody] UpdateEmployeeTrainingDto dto)
        {
            // Basic input validation for mandatory old IDs
            if (dto.OldEmployeeID <= 0)
            {
                return BadRequest(new { success = false, message = "Old Employee ID is required and must be a positive integer." });
            }
            if (dto.OldTrainingID <= 0)
            {
                return BadRequest(new { success = false, message = "Old Training ID is required and must be a positive integer." });
            }

            // Prepare parameters for the stored procedure
            var parameters = new[]
            {
                new SqlParameter("@OldEmployeeID", dto.OldEmployeeID),
                new SqlParameter("@OldTrainingID", dto.OldTrainingID),
                new SqlParameter("@NewEmployeeID", dto.NewEmployeeID.HasValue ? (object)dto.NewEmployeeID.Value : DBNull.Value),
                new SqlParameter("@NewTrainingID", dto.NewTrainingID.HasValue ? (object)dto.NewTrainingID.Value : DBNull.Value)
            };

            try
            {
                // Execute the stored procedure and read the result set
                var result = await _context.Set<UpdateEmployeeTrainingResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateEmployeeTraining] @OldEmployeeID, @OldTrainingID, @NewEmployeeID, @NewTrainingID", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { success = false, message = "Unexpected error: No result from stored procedure. Please ensure the stored procedure always returns a result set." });
                }

                if (!spResult.Success)
                {
                    return BadRequest(new { success = false, message = spResult.Message });
                }

                return Ok(new
                {
                    success = true,
                    message = spResult.Message,
                    oldEmployeeId = spResult.OldEmployeeID,
                    oldTrainingId = spResult.OldTrainingID,
                    newEmployeeId = spResult.NewEmployeeID,
                    newTrainingId = spResult.NewTrainingID
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"A database error occurred: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPost("AddEmployeeTraining")]
        public async Task<IActionResult> AddEmployeeTraining([FromBody] AddEmployeeTrainingDto dto)
        {
            // Validate input DTO
            if (dto.EmployeeID <= 0 || dto.TrainingID <= 0)
            {
                return BadRequest(new { error = "Valid EmployeeID and TrainingID are required." });
            }

            // Prepare SQL parameters
            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", dto.EmployeeID),
                new SqlParameter("@TrainingID", dto.TrainingID)
            };

            try
            {
                // Open connection manually
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using (var setContextCmd = connection.CreateCommand())
                {
                    setContextCmd.CommandText = "EXEC sp_set_session_context @key, @value";
                    var keyParam = setContextCmd.CreateParameter();
                    keyParam.ParameterName = "@key";
                    keyParam.Value = "UserID";
                    setContextCmd.Parameters.Add(keyParam);

                    var valueParam = setContextCmd.CreateParameter();
                    valueParam.ParameterName = "@value";
                    valueParam.Value = CurrentUserId; // Fixed: using CurrentUserId property
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync();
                }

                // Execute stored procedure
                var result = await _context.Set<AddEmployeeTrainingResult>()
                    .FromSqlRaw("EXEC [HR].[AddEmployeeTraining] @EmployeeID, @TrainingID", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { error = "Unexpected error: No result returned from stored procedure." });
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
            finally
            {
                // Ensure connection is closed
                if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
                {
                    await _context.Database.GetDbConnection().CloseAsync();
                }
            }
        }

        [HttpPut("UpdateTraining")]
        public async Task<IActionResult> UpdateTraining([FromBody] UpdateTrainingDto dto)
        {
            // Validate mandatory TrainingID
            if (dto.TrainingID <= 0)
                return BadRequest(new { success = false, message = "Training ID is required and must be a positive integer." });

            // Validate optional fields
            if (dto.Title != null && string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new { success = false, message = "Training Title cannot be empty if provided." });

            if (dto.Title != null && dto.Title.Length > 100)
                return BadRequest(new { success = false, message = "Training Title exceeds maximum length (100 characters)." });

            if (dto.StartDate > DateTime.Today)
                return BadRequest(new { success = false, message = "Start Date cannot be in the future." });

            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
                return BadRequest(new { success = false, message = "End Date cannot be earlier than Start Date." });

            if (dto.Description != null && dto.Description.Length > 255)
                return BadRequest(new { success = false, message = "Training Description exceeds maximum length (255 characters)." });

            // Prepare parameters for the stored procedure
            var parameters = new[]
            {
                new SqlParameter("@TrainingID", dto.TrainingID),
                new SqlParameter("@Title", string.IsNullOrWhiteSpace(dto.Title) ? (object)DBNull.Value : dto.Title.Trim()),
                new SqlParameter("@StartDate", dto.StartDate),
                new SqlParameter("@EndDate", dto.EndDate.HasValue ? (object)dto.EndDate.Value : DBNull.Value),
                new SqlParameter("@Description", string.IsNullOrWhiteSpace(dto.Description) ? (object)DBNull.Value : dto.Description.Trim())
            };

            try
            {
                // Open connection manually
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using (var setContextCmd = connection.CreateCommand())
                {
                    setContextCmd.CommandText = "EXEC sp_set_session_context @key, @value";
                    var keyParam = setContextCmd.CreateParameter();
                    keyParam.ParameterName = "@key";
                    keyParam.Value = "UserID";
                    setContextCmd.Parameters.Add(keyParam);

                    var valueParam = setContextCmd.CreateParameter();
                    valueParam.ParameterName = "@value";
                    valueParam.Value = CurrentUserId; // Fixed: using CurrentUserId property
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync();
                }

                var result = await _context.Set<UpdateTrainingResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateTraining] @TrainingID, @Title, @StartDate, @EndDate, @Description", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "Unexpected error: No result from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                return Ok(new
                {
                    success = true,
                    message = spResult.Message,
                    trainingId = spResult.TrainingID,
                    title = spResult.Title,
                    startDate = spResult.StartDate,
                    endDate = spResult.EndDate,
                    description = spResult.Description
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"A database error occurred: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred: " + ex.Message });
            }
            finally
            {
                // Ensure connection is closed
                if (_context.Database.GetDbConnection().State == System.Data.ConnectionState.Open)
                {
                    await _context.Database.GetDbConnection().CloseAsync();
                }
            }
        }

        [HttpGet("GetAllEmployeeTraining")]
        public async Task<IActionResult> GetAllEmployeeTraining()
        {
            try
            {
                var result = await _context.Set<GetAllEmployeeTrainingResult>()
                    .FromSqlRaw("EXEC [HR].[GetAllEmployeeTraining]")
                    .ToListAsync();

                if (result == null || !result.Any())
                {
                    return NotFound(new { success = false, message = "No employee training records found." });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while fetching data.", error = ex.Message });
            }
        }

        [HttpGet("GetTrainingsByEmployeeId/{employeeId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmployeeTrainingResultDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEmployeeTrainingsByEmployeeId(int employeeId)
        {
            if (employeeId <= 0)
            {
                return BadRequest(new { error = "EmployeeID must be a positive integer." });
            }

            var parameter = new SqlParameter("@EmployeeID", employeeId);

            try
            {
                // Execute the stored procedure and map the results to the EmployeeTrainingResultDto
                var trainings = await _context.Set<EmployeeTrainingResultDto>()
                    .FromSqlRaw("EXEC [HR].[GetEmployeeTrainingsByEmployeeID] @EmployeeID", parameter)
                    .ToListAsync();

                return Ok(trainings);
            }
            catch (SqlException ex)
            {
                // Log the SQL exception details for debugging purposes
                Console.WriteLine($"SQL Error in GetEmployeeTrainingsByEmployeeId: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = $"A database error occurred while retrieving employee trainings: {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Log any other unexpected exceptions
                Console.WriteLine($"Unexpected Error in GetEmployeeTrainingsByEmployeeId: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }
    }

    // Result classes and DTOs
    public class AddTrainingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? TrainingID { get; set; }
        public string? Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class AddTrainingDto
    {
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class TrainingDto
    {
        public int TrainingID { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class DeleteTrainingResult // Fixed: was "DeleteResult"
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteTrainingDto
    {
        public int TrainingID { get; set; }
    }

    public class AddEmployeeTrainingDto
    {
        public int EmployeeID { get; set; }
        public int TrainingID { get; set; }
    }

    public class AddEmployeeTrainingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GetTrainingByIdResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public int? TrainingID { get; set; }
        public string? Title { get; set; }

        // Start and End dates of the training
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string? Description { get; set; }

        // Date when the training was assigned to the employee
        public DateTime? AssignedDate { get; set; }
    }


    public class DeleteEmployeeTrainingDto
    {
        public int EmployeeID { get; set; }
        public int TrainingID { get; set; }
    }

    public class DeleteEmployeeTrainingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateEmployeeTrainingDto
    {
        public int OldEmployeeID { get; set; }
        public int OldTrainingID { get; set; }
        public int? NewEmployeeID { get; set; }
        public int? NewTrainingID { get; set; }
    }

    public class UpdateEmployeeTrainingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OldEmployeeID { get; set; }
        public int? OldTrainingID { get; set; }
        public int? NewEmployeeID { get; set; }
        public int? NewTrainingID { get; set; }
    }

    public class UpdateTrainingDto
    {
        public int TrainingID { get; set; }
        public string? Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateTrainingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? TrainingID { get; set; }
        public string? Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class EmployeeTrainingResultDto
    {
        public int TrainingID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? AssignedDate { get; set; }
    }


    public class GetAllEmployeeTrainingResult
    {
        public int EmployeeID { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public int TrainingID { get; set; }
        public string TrainingTitle { get; set; } = string.Empty;
        public string  Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? AssignedDate { get; set; }
    }
}