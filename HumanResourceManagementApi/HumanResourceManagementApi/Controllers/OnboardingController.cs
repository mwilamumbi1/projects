using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
 
public class HireCandidateDto
{
    public int InterviewID { get; set; }
}

 
public class InterviewReportModel
{
    // Interview Details
    public int InterviewID { get; set; }
    public int ApplicationID { get; set; }
    public DateTime? InterviewDate { get; set; }
    public string? Feedback { get; set; }
    public string? InterviewStatus { get; set; }

    // Interviewer Details
    public string? Interviewers { get; set; }

    // Candidate/Employee Details
    public int EmployeeID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public DateTime? HireDate { get; set; }

    // Lookups (Names/Titles)
    public string? DepartmentName { get; set; }
    public string? PositionTitle { get; set; }
    public string? ManagerName { get; set; }

    // Financial & Identity
    public decimal? Salary { get; set; }
    public string? Email { get; set; }
    public string? NRC { get; set; }
    public string? TPIN { get; set; }
    public string? MaritalStatus { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? ContractType { get; set; }
    public string? NapsaRegNumber { get; set; }
    public string? StatusName { get; set; }

    // Contact & Location
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string? Province { get; set; }

    // Profile Data (VarBinary(max))
    public byte[]? Profile { get; set; }
}
 

namespace Core.HumanResourceManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OnboardingController : ControllerBase
    {
        private readonly HRDataContext _context;

        public OnboardingController(HRDataContext context)
        {
            _context = context;
        }

        
        [HttpPut("hire")]
        public async Task<IActionResult> HireCandidate([FromBody] HireCandidateDto dto)
        {
            if (dto.InterviewID <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid InterviewID." });
            }

            try
            {
                var interviewIdParam = new SqlParameter("@InterviewID", dto.InterviewID);

                // Execute the stored procedure that performs the hire logic
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [HR].[HireCandidate] @InterviewID",
                    interviewIdParam
                );

                return Ok(new { success = true, message = $"Candidate successfully hired for InterviewID {dto.InterviewID}." });
            }
            catch (SqlException ex)
            {
                // Handle errors (like Invalid InterviewID or no linked employee) raised by RAISERROR in the proc
                return BadRequest(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        
        [HttpGet("hired")]
        public async Task<IActionResult> GetHiredInterviews()
        {
            try
            {
                var result = await _context.InterviewReports
                    .FromSqlRaw("EXEC [HR].[GetHiredInterviews]")
                    .ToListAsync();

                if (result == null || result.Count == 0)
                {
                    return NotFound(new { success = false, message = "No hired interviews found." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"An unexpected error occurred while fetching hired interviews: {ex.Message}" });
            }
        }

       
        [HttpGet("unhired")]
        public async Task<IActionResult> GetUnhiredInterviews()
        {
            try
            {
                var result = await _context.InterviewReports
                    .FromSqlRaw("EXEC [HR].[GetUnhiredInterviews]")
                    .ToListAsync();

                if (result == null || result.Count == 0)
                {
                    return NotFound(new { success = false, message = "No unhired (pending) interviews found." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"An unexpected error occurred while fetching pending interviews: {ex.Message}" });
            }
        }
    }
}
