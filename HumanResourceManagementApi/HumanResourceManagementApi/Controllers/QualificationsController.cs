using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Core.HumanResourceManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QualificationsController : ControllerBase
    {
        private readonly HRDataContext _context;

        public QualificationsController(HRDataContext context)
        {
            _context = context;
        }

        // GET: api/Qualifications
        [HttpGet("GetAllEmployeesQualifications")]
        public async Task<IActionResult> GetAllEmployeesQualifications()
        {
            var result = await _context.Set<EmployeeQualificationsSummaryDto>() // Renamed DTO for clarity
                .FromSqlRaw("EXEC HR.GetAllEmployeesQualifications")
                .ToListAsync();

            if (result == null || result.Count == 0)
            {
                return NotFound(new { success = false, message = "No employee qualifications found." });
            }

            return Ok(result);
        }

        // --- NEW ENDPOINT TO GET QUALIFICATIONS BY EMPLOYEE ID ---

        // GET: api/Qualifications/{employeeId}
        [HttpGet("{employeeId}")]
        public async Task<IActionResult> GetEmployeeQualificationsByID(int employeeId)
        {
            // Use SqlParameter to safely pass the employee ID to the stored procedure
            var employeeIdParam = new SqlParameter("@EmployeeID", employeeId);

            var result = await _context.Set<EmployeeQualificationDetailDto>()
                // Call the new stored procedure
                .FromSqlRaw("EXEC HR.GetEmployeeQualificationsByID @EmployeeID", employeeIdParam)
                .ToListAsync();

            if (result == null || result.Count == 0)
            {
                // Check if the list is empty, which could mean the employee has no qualifications
                // or the EmployeeID doesn't exist.
                return NotFound(new { success = false, message = $"No qualifications found for Employee ID {employeeId}." });
            }

            return Ok(result);
        }

        // ----------------------------------------------------------

        // POST: api/Qualifications/Add
        [HttpPost("Add")]
        public async Task<IActionResult> AddQualification([FromBody] AddUpdateQualificationDto dto)
        {
            try
            {
                var employeeIdParam = new SqlParameter("@EmployeeID", dto.EmployeeID);
                var qualificationParam = new SqlParameter("@Qualification", dto.Qualification);
                var institutionParam = new SqlParameter("@InstitutionName", dto.InstitutionName);
                var acquiredDateParam = new SqlParameter("@AcquiredDate", dto.AcquiredDate);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.AddEmployeeQualifications @EmployeeID, @Qualification, @InstitutionName, @AcquiredDate",
                    employeeIdParam, qualificationParam, institutionParam, acquiredDateParam
                );

                return Ok(new { success = true, message = "Qualification added successfully." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // PUT: api/Qualifications/Update/{id}
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> UpdateQualification(int id, [FromBody] AddUpdateQualificationDto dto)
        {
            try
            {
                var idParam = new SqlParameter("@EmployeeQualificationID", id);
                var qualificationParam = new SqlParameter("@Qualification", (object)dto.Qualification ?? DBNull.Value);
                var institutionParam = new SqlParameter("@InstitutionName", (object)dto.InstitutionName ?? DBNull.Value);
                var acquiredDateParam = new SqlParameter("@AcquiredDate", (object?)dto.AcquiredDate ?? DBNull.Value);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.UpdateEmployeeQualifications @EmployeeQualificationID, @Qualification, @InstitutionName, @AcquiredDate",
                    idParam, qualificationParam, institutionParam, acquiredDateParam
                );

                return Ok(new { success = true, message = "Qualification updated successfully." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }

    // --- DTOs ---

    // Renamed for clarity to reflect its summary nature
    public class EmployeeQualificationsSummaryDto
    {
        public int employeeQualificationID {  get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = null!;
        public int? NumberOfQualifications { get; set; }
        public string? QualificationsDetails { get; set; } = null!;
    }

    // NEW DTO to match the output of [HR].[GetEmployeeQualificationsByID]
    public class EmployeeQualificationDetailDto
    {
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = null!;
        // The new field returned by the SP
        public string JobTitle { get; set; } = null!;
        public int EmployeeQualificationID { get; set; }
        public string Qualification { get; set; } = null!;
        public string InstitutionName { get; set; } = null!;
        public DateTime AcquiredDate { get; set; }
    }


    public class AddUpdateQualificationDto
    {
        public int EmployeeID { get; set; }
        public string? Qualification { get; set; }
        public string? InstitutionName { get; set; }
        public DateTime? AcquiredDate { get; set; }
    }
}