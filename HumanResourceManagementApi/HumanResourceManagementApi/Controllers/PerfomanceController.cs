using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerformanceController : ControllerBase
    {
        private readonly HRDataContext _context;

        public PerformanceController(HRDataContext context)
        {
            _context = context;
        }

        [HttpPost("AddEmployeePerformance")]
        public async Task<IActionResult> AddEmployeePerformance([FromBody] AddEmployeePerformanceDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", dto.EmployeeId.HasValue ? (object)dto.EmployeeId.Value : DBNull.Value),
                new SqlParameter("@KPI_ID", dto.KPI_ID.HasValue ? (object)dto.KPI_ID.Value : DBNull.Value),
                new SqlParameter("@Rating", dto.Rating.HasValue ? (object)dto.Rating.Value : DBNull.Value),
                new SqlParameter("@Review_Date", dto.ReviewDate.HasValue ? (object)dto.ReviewDate.Value : DBNull.Value),
                new SqlParameter("@Comments", string.IsNullOrWhiteSpace(dto.Comments) ? (object)DBNull.Value : dto.Comments.Trim()),
                new SqlParameter("@CallingUserID", dto.CallingUserID.HasValue ? (object)dto.CallingUserID.Value : DBNull.Value)
            };

            try
            {
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
                    valueParam.Value = currentUserId;
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync();
                }

                var result = await _context.Set<AddEmployeePerformanceResult>()
                    .FromSqlRaw("EXEC [HR].[AddEmployeePerformance] @EmployeeID, @KPI_ID, @Rating, @Review_Date, @Comments, @CallingUserID", parameters)
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
                return StatusCode(500, new { error = "A database error occurred: " + ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPut("UpdateEmployeePerformance")]
        public async Task<IActionResult> UpdateEmployeePerformance([FromBody] UpdateEmployeePerformanceDto dto)
        {
            // Basic Validation
            if (dto.PerformanceID <= 0)
                return BadRequest(new { success = false, message = "PerformanceID is required and must be a positive integer." });

            if (dto.EmployeeID <= 0)
                return BadRequest(new { success = false, message = "EmployeeID is required and must be a positive integer." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            // Build parameters safely
            var parameters = new object[]
            {
                new SqlParameter("@PerformanceID", dto.PerformanceID),
                new SqlParameter("@EmployeeID", dto.EmployeeID),
                new SqlParameter("@KPI_ID", dto.KPI_ID.HasValue ? (object)dto.KPI_ID.Value : DBNull.Value),
                new SqlParameter("@ReviewDate", dto.ReviewDate.HasValue ? (object)dto.ReviewDate.Value : DBNull.Value),
                new SqlParameter("@Reviewed_By", dto.Reviewed_By.HasValue ? (object)dto.Reviewed_By.Value : DBNull.Value),
                new SqlParameter("@Rating", dto.Rating.HasValue ? (object)dto.Rating.Value : DBNull.Value),
                new SqlParameter("@Comments", string.IsNullOrWhiteSpace(dto.Comments) ? (object)DBNull.Value : dto.Comments.Trim())
            };

            try
            {
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
                    valueParam.Value = currentUserId;
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync();
                }

                var result = await _context.Set<UpdateEmployeePerformanceResult>()
                    .FromSqlRaw(
                        "EXEC [HR].[UpdateEmployeePerformance] @PerformanceID, @EmployeeID, @KPI_ID, @ReviewDate, @Reviewed_By, @Rating, @Comments",
                        parameters
                    )
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "No result returned from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                return Ok(new
                {
                    success = true,
                    message = spResult.Message
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Unexpected error: " + ex.Message });
            }
        }

        [HttpPut("UpdateReviewIndicator")]
        public async Task<IActionResult> UpdateReviewIndicator([FromBody] UpdateReviewIndicatorDto dto)
        {
            // Basic input validation for mandatory Old IDs
            if (dto.OldReviewID <= 0 || dto.OldIndicatorID <= 0)
            {
                return BadRequest(new { success = false, message = "Old Review ID and Old Indicator ID are required and must be positive integers." });
            }

            // Optional C# side validation for new IDs if provided
            if (dto.NewReviewID.HasValue && dto.NewReviewID.Value <= 0)
            {
                return BadRequest(new { success = false, message = "New Review ID must be a positive integer if provided." });
            }
            if (dto.NewIndicatorID.HasValue && dto.NewIndicatorID.Value <= 0)
            {
                return BadRequest(new { success = false, message = "New Indicator ID must be a positive integer if provided." });
            }

            // Optional C# side validation for Score if provided
            if (dto.Score.HasValue && (dto.Score.Value < 0.00m || dto.Score.Value > 100.00m))
            {
                return BadRequest(new { success = false, message = "Score must be between 0.00 and 100.00 if provided." });
            }

            // Comments length validation
            if (dto.Comments != null && dto.Comments.Length > 5000)
            {
                return BadRequest(new { success = false, message = "Comments exceed maximum practical length (5000 characters)." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            // Prepare parameters for the stored procedure
            var parameters = new[]
            {
                new SqlParameter("@OldReviewID", dto.OldReviewID),
                new SqlParameter("@OldIndicatorID", dto.OldIndicatorID),
                new SqlParameter("@NewReviewID", dto.NewReviewID.HasValue ? (object)dto.NewReviewID.Value : DBNull.Value),
                new SqlParameter("@NewIndicatorID", dto.NewIndicatorID.HasValue ? (object)dto.NewIndicatorID.Value : DBNull.Value),
                new SqlParameter("@Score", dto.Score.HasValue ? (object)dto.Score.Value : DBNull.Value),
                new SqlParameter("@Comments", string.IsNullOrWhiteSpace(dto.Comments) ? (object)DBNull.Value : dto.Comments.Trim())
            };

            try
            {
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
                    valueParam.Value = currentUserId;
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync();
                }

                var result = await _context.Set<UpdateReviewIndicatorResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateReviewIndicator] @OldReviewID, @OldIndicatorID, @NewReviewID, @NewIndicatorID, @Score, @Comments", parameters)
                    .ToListAsync();

                await connection.CloseAsync();

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
                    oldReviewId = spResult.OldReviewID,
                    oldIndicatorId = spResult.OldIndicatorID,
                    newReviewId = spResult.NewReviewID,
                    newIndicatorId = spResult.NewIndicatorID,
                    score = spResult.Score,
                    comments = spResult.Comments
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

        [HttpDelete("DeletePerformanceReview")]
        public async Task<IActionResult> DeletePerformanceReview([FromBody] DeletePerformanceReviewDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var param = new SqlParameter("@ReviewID", dto.ReviewID);

            try
            {
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
                    valueParam.Value = currentUserId;
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync();
                }

                var result = await _context.Set<DeletePerfomanceResult>()
                    .FromSqlRaw("EXEC [HR].[DeletePerformanceReview] @ReviewID", param)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
            }
        }

        [HttpGet("GetEmployeePerformanceSummary/{employeeId}")]
        public async Task<IActionResult> GetEmployeePerformanceSummary(int employeeId)
        {
            // Basic validation
            if (employeeId <= 0)
            {
                return BadRequest(new { error = "Employee ID must be a positive integer." });
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@EmployeeID", employeeId)
                };

                var result = await _context.Set<EmployeePerformanceSummaryDto>()
                    .FromSqlRaw("EXEC [HR].[GetEmployeePerformanceSummary] @EmployeeID", parameters)
                    .ToListAsync();

                // Check if the result is empty (which could mean employee not found)
                if (result.Count == 0)
                {
                    return NotFound(new { message = "No performance data found for the specified employee." });
                }

                return Ok(result);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        [HttpGet("GetAllEmployeePerformance")]
        public async Task<IActionResult> GetAllEmployeePerformance()
        {
            try
            {
                var performances = await _context.Set<EmployeePerformanceDto>()
                    .FromSqlRaw("EXEC HR.GetAllEmployeePerformance")
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(performances);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });
            }
        }

        [HttpGet("GetDepartmentPerformanceSummary")]
        public async Task<IActionResult> GetDepartmentPerformanceSummary(int? year)
        {
            var result = await _context.Set<DepartmentPerformanceSummaryDto>()
                .FromSqlRaw("EXEC HR.GetDepartmentPerformanceSummary {0}", year.HasValue ? year.Value : (object)DBNull.Value)
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("ByReviewer/{reviewerId}")]
        public async Task<ActionResult<IEnumerable<ReviewByReviewerDto>>> GetReviewsByReviewer(int reviewerId)
        {
            var reviews = await _context.Set<ReviewByReviewerDto>()
                .FromSqlRaw("EXEC HR.GetReviewsByReviewer @ReviewerID = {0}", reviewerId)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpDelete("DeleteEmployeePerformanceReview")]
        public async Task<IActionResult> DeletEmployeePerformanceReview([FromBody] DeleteeEmployeePerformanceReviewDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var param = new SqlParameter("@PerformanceID", dto.Performance_ID);

            try
            {
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
                    valueParam.Value = currentUserId;
                    setContextCmd.Parameters.Add(valueParam);

                    await setContextCmd.ExecuteNonQueryAsync();
                }

                var result = await _context.Set<EmployeePerfomanceDeleteResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteEmployeePerformance] @PerformanceID", param)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error occurred: " + ex.Message });
            }
        }

        public class ReviewByReviewerDto
        {
            public int ReviewID { get; set; }
            public string EmployeeReviewed { get; set; }
            public int Rating { get; set; }
            public DateTime ReviewDate { get; set; }
            public string Comments { get; set; }
        }
    }

    public class DepartmentPerformanceSummaryDto
    {
        public string DepartmentName { get; set; }
        public int AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int NumberOfEmployeesInDepartment { get; set; }
    }

    public class DeletePerformanceReviewDto
    {
        public int ReviewID { get; set; }
    }

    public class DeletePerfomanceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EmployeePerformanceDto
    {
        public int Performance_ID { get; set; }
        public int Employee_ID { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public string? KPI_Name { get; set; }
        public int Rating { get; set; }
        public DateTime Review_Date { get; set; }
        public string? Comments { get; set; }
        public string? ReviewerFullName { get; set; }
    }
}

public class AddEmployeePerformanceDto
{
    public int? EmployeeId { get; set; }
    public int? KPI_ID { get; set; }
    public int? Rating { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string Comments { get; set; }
    public int? CallingUserID { get; set; }
}

public class AddEmployeePerformanceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UpdateEmployeePerformanceDto
{
    public int PerformanceID { get; set; }
    public int EmployeeID { get; set; }
    public int? KPI_ID { get; set; }
    public DateTime? ReviewDate { get; set; }
    public int? Reviewed_By { get; set; }
    public int? Rating { get; set; }
    public string? Comments { get; set; }
}

public class UpdateEmployeePerformanceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UpdateReviewIndicatorDto
{
    public int OldReviewID { get; set; }
    public int OldIndicatorID { get; set; }
    public int? NewReviewID { get; set; }
    public int? NewIndicatorID { get; set; }
    public decimal? Score { get; set; }
    public string? Comments { get; set; }
}

public class UpdateReviewIndicatorResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int? OldReviewID { get; set; }
    public int? OldIndicatorID { get; set; }
    public int? NewReviewID { get; set; }
    public int? NewIndicatorID { get; set; }
    public decimal? Score { get; set; }
    public string? Comments { get; set; }
}

public class EmployeePerfomanceDeleteResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
}

public class DeleteeEmployeePerformanceReviewDto
{
    public int Performance_ID { get; set; }
}

public class EmployeePerformanceSummaryDto
{
    public int KPI_ID { get; set; }
    public string KPI_Name { get; set; } = string.Empty;
    public decimal AverageRating { get; set; }
}