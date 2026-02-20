    using Core.HumanResourceManagementApi.Models; // For HRDataContext and GetTotalEmployeeCountResult
    using global::Core.HumanResourceManagementApi.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Data.SqlClient; // Important for SqlParameter
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    namespace Core.HumanResourceManagementApi.Controllers
    {
        [ApiController]
        [Route("api/[controller]")]
        public class CountsController : ControllerBase
        {
            private readonly HRDataContext _context;

            public CountsController(HRDataContext context)
            {
                _context = context;
            }

            [HttpGet("total-employees")]
            public async Task<ActionResult<TotalEmployeeCountDto>> GetTotalEmployeeCount()
            {
                try
                {
                    // Execute the stored procedure
                    var result = await _context.Set<GetTotalEmployeeCountResult>()
                        .FromSqlRaw("EXEC [HR].[GetTotalEmployeeCount]")
                        .ToListAsync();

                    var spResult = result.FirstOrDefault();

                    if (spResult == null)
                    {
                        
                        return StatusCode(500, new { error = "No count result returned from stored procedure." });
                    }

                    // Map the result to your DTO and return OK
                    return Ok(new TotalEmployeeCountDto { TotalEmployees = spResult.TotalEmployees });
                }
                catch (SqlException ex)
                {
                    // Log the SQL exception details for debugging
                    // Consider using ILogger<EmployeeController> for proper logging
                    return StatusCode(500, new { error = $"Database error: {ex.Message}" });
                }
                catch (Exception ex)
                {
                    // Catch any other unexpected exceptions
                    // Log the exception details
                    return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
                }
            }

        [HttpGet("total-Jobs")]
        public async Task<ActionResult<TotalEmployeeCountDto>> TotalJobsCountDto()
        {
            try
            {
                // Execute the stored procedure
                var result = await _context.Set<TotalJobsCountDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllJobCount]")
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    // This scenario means the stored procedure returned no rows,
                    // which for a COUNT(*) is unlikely unless the table is truly empty
                    // and the SP returns nothing at all, not even a 0.
                    // Assuming a count will always return at least one row with 0 if no records.
                    // Adjust this error message based on expected SP behavior.
                    return StatusCode(500, new { error = "No count result returned from stored procedure." });
                }

                // Map the result to your DTO and return OK
                return Ok(new TotalJobsCountDto { TotalJobs = spResult.TotalJobs });
            }
            catch (SqlException ex)
            {
                // Log the SQL exception details for debugging
                // Consider using ILogger<EmployeeController> for proper logging
                return StatusCode(500, new { error = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                // Log the exception details
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }
        [HttpGet("total-Perfomance")]
        public async Task<ActionResult<TotalPerfomanceCountDto>> TotalPerfomanceCountDto()
        {
            try
            {
                // Execute the stored procedure
                var result = await _context.Set<TotalPerfomanceCountDto>()
                    .FromSqlRaw("EXEC [HR].[GetEmployeePerformanceCount]")
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { error = "No count result returned from stored procedure." });
                }

                // Map the result to your DTO and return OK
                return Ok(new TotalPerfomanceCountDto { TotalPerfomance = spResult.TotalPerfomance });
            }
            catch (SqlException ex)
            {
                // Log the SQL exception details for debugging
                // Consider using ILogger<EmployeeController> for proper logging
                return StatusCode(500, new { error = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                // Log the exception details
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        [HttpGet("total-Training")]
        public async Task<ActionResult<TotalTrainingCountDto>> TotalTrainingCountDto()
        {
            try
            {
                // Execute the stored procedure
                var result = await _context.Set<TotalTrainingCountDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllJobCount]")
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    
                    return StatusCode(500, new { error = "No count result returned from stored procedure." });
                }

                // Map the result to your DTO and return OK
                return Ok(new TotalTrainingCountDto { TotalTraining = spResult.TotalTraining });
            }
            catch (SqlException ex)
            {
                // Log the SQL exception details for debugging
                // Consider using ILogger<EmployeeController> for proper logging
                return StatusCode(500, new { error = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                // Log the exception details
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }
        [HttpGet("total-Department")]
        public async Task<ActionResult<TotalDepartmenntCountDto>> TotalDepartmenntCount()
        {
            try
            {
                // Execute the stored procedure
                var result = await _context.Set<TotalDepartmenntCountDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllDepartmentsCount]")
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {

                    return StatusCode(500, new { error = "No count result returned from stored procedure." });
                }

                // Map the result to your DTO and return OK
                return Ok(new TotalDepartmenntCountDto { TotalDepartment = spResult.TotalDepartment });
            }
            catch (SqlException ex)
            {
                // Log the SQL exception details for debugging
                // Consider using ILogger<EmployeeController> for proper logging
                return StatusCode(500, new { error = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                // Log the exception details
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }
    }
    }
 
 public class GetTotalEmployeeCountResult
{
    public int TotalEmployees { get; set; }
}
public class TotalEmployeeCountDto
{
    // This will directly hold the result from the stored procedure
    public int TotalEmployees { get; set; }
}
public class TotalJobsCountDto
{
    // This will directly hold the result from the stored procedure
    public int TotalJobs { get; set; }
}

public class TotalPerfomanceCountDto
{
    // This will directly hold the result from the stored procedure
    public int TotalPerfomance { get; set; }
}
public class TotalTrainingCountDto
{
    // This will directly hold the result from the stored procedure
    public int TotalTraining { get; set; }
}
public class TotalDepartmenntCountDto
{
    // This will directly hold the result from the stored procedure
    public int TotalDepartment { get; set; }
}
