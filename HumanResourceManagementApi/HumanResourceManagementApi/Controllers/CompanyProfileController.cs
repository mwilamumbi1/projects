using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Core.HumanResourceManagementApi.Controllers
{
    // --- DTOs ---

    public class CompanyProfileRequest
    {
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string Motto { get; set; }

        // 🔹 NEW FIELDS
        public string? CompanyPhone { get; set; }
        public string? PhysicalAddress { get; set; }
        public string? PostalAddress { get; set; }

        public string? EmailServerHost { get; set; }
        public int? EmailServerPort { get; set; }
        public string? EmailUsername { get; set; }
        public string? EmailPassword { get; set; }
        public bool? UseSSL { get; set; }
    }


    // What the database returns (Output)
    public class CompanyProfileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public string? CompanyName { get; set; }
        public string? CompanyEmail { get; set; }
        public string? Motto { get; set; }

        // 🔹 OPTIONAL (useful for GetProfile)
        public string? CompanyPhone { get; set; }
        public string? PhysicalAddress { get; set; }
        public string? PostalAddress { get; set; }

        public string? EmailServerHost { get; set; }
        public int? EmailServerPort { get; set; }
        public string? EmailUsername { get; set; }
        public bool? UseSSL { get; set; }
    }

    // --- Controller ---
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyProfileController : ControllerBase
    {
        private readonly HRDataContext _context;

        public CompanyProfileController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _context.Set<CompanyProfileResponse>()
                .FromSqlRaw("EXEC HR.GetCompanyProfile")
                .ToListAsync();

            var response = result.FirstOrDefault();

            if (response == null || !response.Success)
                return NotFound(response ?? new CompanyProfileResponse { Success = false, Message = "Not Found" });

            return Ok(response);
        }

        [HttpPost("AddProfile")]
        public async Task<IActionResult> AddProfile([FromBody] CompanyProfileRequest request)
        {
            var parameters = new[]
            {
        new SqlParameter("@Name", request.CompanyName),
        new SqlParameter("@Email", request.CompanyEmail),
        new SqlParameter("@Motto", request.Motto),

        new SqlParameter("@CompanyPhone", (object?)request.CompanyPhone ?? DBNull.Value),
        new SqlParameter("@PhysicalAddress", (object?)request.PhysicalAddress ?? DBNull.Value),
        new SqlParameter("@PostalAddress", (object?)request.PostalAddress ?? DBNull.Value),

        new SqlParameter("@EmailServerHost", (object?)request.EmailServerHost ?? DBNull.Value),
        new SqlParameter("@EmailServerPort", (object?)request.EmailServerPort ?? DBNull.Value),
        new SqlParameter("@EmailUsername", (object?)request.EmailUsername ?? DBNull.Value),
        new SqlParameter("@EmailPassword", (object?)request.EmailPassword ?? DBNull.Value),
        new SqlParameter("@UseSSL", (object?)request.UseSSL ?? DBNull.Value)
    };

            var result = await _context.Set<CompanyProfileResponse>()
                .FromSqlRaw(
                    "EXEC HR.AddCompanyProfile @Name, @Email, @Motto, " +
                    "@CompanyPhone, @PhysicalAddress, @PostalAddress, " +
                    "@EmailServerHost, @EmailServerPort, @EmailUsername, @EmailPassword, @UseSSL",
                    parameters)
                .ToListAsync();

            var response = result.FirstOrDefault();
            return response != null && response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] CompanyProfileRequest request)
        {
            var parameters = new[]
            {
        new SqlParameter("@Name", request.CompanyName),
        new SqlParameter("@Email", request.CompanyEmail),
        new SqlParameter("@Motto", request.Motto),

        new SqlParameter("@CompanyPhone", (object?)request.CompanyPhone ?? DBNull.Value),
        new SqlParameter("@PhysicalAddress", (object?)request.PhysicalAddress ?? DBNull.Value),
        new SqlParameter("@PostalAddress", (object?)request.PostalAddress ?? DBNull.Value),

        new SqlParameter("@EmailServerHost", (object?)request.EmailServerHost ?? DBNull.Value),
        new SqlParameter("@EmailServerPort", (object?)request.EmailServerPort ?? DBNull.Value),
        new SqlParameter("@EmailUsername", (object?)request.EmailUsername ?? DBNull.Value),
        new SqlParameter("@EmailPassword", (object?)request.EmailPassword ?? DBNull.Value),
        new SqlParameter("@UseSSL", (object?)request.UseSSL ?? DBNull.Value)
    };

            var result = await _context.Set<CompanyProfileResponse>()
                .FromSqlRaw(
                    "EXEC HR.UpdateCompanyProfile @Name, @Email, @Motto, " +
                    "@CompanyPhone, @PhysicalAddress, @PostalAddress, " +
                    "@EmailServerHost, @EmailServerPort, @EmailUsername, @EmailPassword, @UseSSL",
                    parameters)
                .ToListAsync();

            var response = result.FirstOrDefault();
            return response != null && response.Success ? Ok(response) : BadRequest(response);
        }

    }
}