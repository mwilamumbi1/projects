using Core.HumanResourceManagementApi.Models; // Ensure this is correct for your models
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.IO; // Required for Path.Combine, File.Exists, File.ReadAllBytesAsync
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
using System; // Required for Guid
using Microsoft.AspNetCore.Http; // Required for IFormFile
using System.ComponentModel.DataAnnotations; // For [Required]
using Core.HumanResourceManagementApi.DTOs; // Import the email service DTO

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // This will result in routes like /api/Application/GetAllApplicants
    public class ApplicationController : ControllerBase
    {
        private readonly HRDataContext _context;
        private readonly IWebHostEnvironment _env; // Inject IWebHostEnvironment
        private readonly IEmailService _emailService; // Inject the email service

        public ApplicationController(HRDataContext context, IWebHostEnvironment env, IEmailService emailService) // Add IEmailService to constructor
        {
            _context = context;
            _env = env;
            _emailService = emailService; // Assign it
        }

        // --- Endpoints for Applications Management ---

        [HttpGet("GetAllApplicants")] // This will make the endpoint something like GET /api/Application/GetAllApplicants
        public async Task<IActionResult> GetAllApplicants()
        {
            try
            {
                // Uses the GetAllApplicantsDto to map the results of the stored procedure
                var applicants = await _context.Set<GetAllApplicantsDto>()
                    .FromSqlRaw("EXEC HR.GetAllApplications") // Calls the stored procedure
                    .ToListAsync(); // Executes the query and returns a list

                // Returns HTTP 200 OK with the list of applicants, or an empty list if none found
                return Ok(applicants);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        // --- NEW DTO for AddApplication Request (to handle file and form data) ---
        public class AddApplicationRequest
        {
            [Required]
            public int JobOpeningID { get; set; }
            [Required]
            public string ApplicantName { get; set; } = string.Empty;
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
            public IFormFile? ResumeFile { get; set; } // The actual file
        }

        [HttpPost("AddApplication")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddApplication([FromForm] AddApplicationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string resumeFilename = null;

            // 1. Handle File Upload
            if (request.ResumeFile != null && request.ResumeFile.Length > 0)
            {
                try
                {
                    string fileExtension = Path.GetExtension(request.ResumeFile.FileName).ToLowerInvariant();
                    string[] allowedExtensions = { ".pdf", ".doc", ".docx" };
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new { error = "Invalid file type. Only PDF and Word documents are allowed." });
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "Resumes");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.ResumeFile.CopyToAsync(stream);
                    }

                    resumeFilename = uniqueFileName;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file: {ex.Message}");
                    return StatusCode(500, new { error = "Failed to upload resume file: " + ex.Message });
                }
            }
            else
            {
                return BadRequest(new { error = "Resume file is required." });
            }

            // 2. Prepare parameters for the Stored Procedure
            var parameters = new[]
            {
                new SqlParameter("@JobOpeningID", request.JobOpeningID),
                new SqlParameter("@ApplicantName", request.ApplicantName),
                new SqlParameter("@Resume", resumeFilename),
                new SqlParameter("@Email", request.Email)
            };

            try
            {
                var result = await _context.Set<AddApplicationResult>()
                    .FromSqlRaw("EXEC [HR].[AddApplication] @JobOpeningID, @ApplicantName, @Resume, @Email", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    // Clean up file if SP fails unexpectedly
                    DeleteFile(resumeFilename);
                    return StatusCode(500, new { error = "Unexpected error: No result from stored procedure." });
                }

                if (!spResult.Success)
                {
                    // Clean up file if SP returns a specific failure
                    DeleteFile(resumeFilename);
                    return BadRequest(new { error = spResult.Message });
                }

                return Ok(new { message = spResult.Message, resumeFileName = resumeFilename });
            }
            catch (Exception ex)
            {
                // Clean up file if any exception occurs
                DeleteFile(resumeFilename);
                Console.WriteLine($"Error adding application to database: {ex.Message}");
                return StatusCode(500, new { error = "An unexpected error occurred while saving the application: " + ex.Message });
            }
        }

        // --- Helper method to safely delete files ---
        private void DeleteFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            var filePath = Path.Combine(_env.WebRootPath, "Resumes", fileName);
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Deleted uploaded file: {filePath}");
                }
                catch (Exception fileEx)
                {
                    Console.WriteLine($"Error deleting file {filePath}: {fileEx.Message}");
                }
            }
        }

        // --- Other Endpoints (Updated for clarity and consistency) ---

        [HttpPut("AddApplicationStatus")]
        public async Task<IActionResult> AddApplicationStatus([FromBody] AddApplicationStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var parameters = new[]
            {
                new SqlParameter("@StatusName", dto.StatusName.Trim())
            };

            try
            {
                var result = await _context.Set<AddApplicationStatusResult>()
                    .FromSqlRaw("EXEC [HR].[AddApplicationStatus] @StatusName", parameters)
                    .ToListAsync();

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
                return BadRequest(new { error = $"A database error occurred: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPut("UpdateApplicationStatus")]
        public async Task<IActionResult> UpdateApplicationStatus([FromBody] UpdateApplicationStatusDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var parameters = new[]
            {
                new SqlParameter("@StatusID", dto.StatusID),
                new SqlParameter("@NewStatusName", dto.NewStatusName.Trim())
            };

            try
            {
                var result = await _context.Set<UpdateApplicationStatusResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateApplicationStatus] @StatusID, @NewStatusName", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { success = false, message = "Unexpected error: No result from stored procedure." });
                }

                if (!spResult.Success)
                {
                    return BadRequest(new { success = false, message = spResult.Message });
                }

                return Ok(new
                {
                    success = true,
                    message = spResult.Message,
                    statusId = spResult.StatusID,
                    newStatusName = spResult.NewStatusName
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

        [HttpPut("UpdateApplication")]
        public async Task<IActionResult> UpdateApplication([FromBody] UpdateApplicationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the application details *before* updating to get the applicant's email
            var existingApplication = await _context.Applications
                                                    .FirstOrDefaultAsync(a => a.ApplicationID == dto.ApplicationID);

            if (existingApplication == null)
            {
                return NotFound(new { success = false, message = "Application not found." });
            }

            var parameters = new[]
            {
                new SqlParameter("@ApplicationID", dto.ApplicationID),
                new SqlParameter("@NewStatusID", dto.NewStatusID)
            };

            try
            {
                var result = await _context.Set<UpdateApplicationResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateApplication] @ApplicationID, @NewStatusID", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { success = false, message = "Unexpected error: No result from stored procedure." });
                }

                if (!spResult.Success)
                {
                    return BadRequest(new { success = false, message = spResult.Message });
                }

                // Check if a custom message was provided and send the email
                if (!string.IsNullOrEmpty(dto.feedback))
                {
                    string subject;
                    string plainTextContent = dto.feedback;
                    string htmlContent = $"<p>{plainTextContent.Replace("\n", "<br/>")}</p>";

                    // Dynamically set the email subject based on the new status
                    if (spResult.NewStatusName.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        subject = "Update Regarding Your Job Application";
                    }
                    else if (spResult.NewStatusName.Equals("Scheduled for Interview", StringComparison.OrdinalIgnoreCase))
                    {
                        subject = "Interview Invitation for Your Application";
                    }
                    else
                    {
                        // Default subject for other status updates
                        subject = "Update Regarding Your Application";
                    }

                    await _emailService.SendEmailAsync(existingApplication.Email, subject, plainTextContent, htmlContent);
                }

                return Ok(new
                {
                    success = true,
                    message = spResult.Message,
                    applicationId = spResult.ApplicationID,
                    newStatusId = spResult.NewStatusID,
                    newStatusName = spResult.NewStatusName
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

        [HttpDelete("DeleteApplication/{id}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Application ID must be a positive integer." });
            }

            string resumeFilenameToDelete = null;

            try
            {
                // First, retrieve the application to get the resume filename
                var application = await _context.Applications.FirstOrDefaultAsync(a => a.ApplicationID == id);

                if (application != null && !string.IsNullOrWhiteSpace(application.Resume))
                {
                    resumeFilenameToDelete = application.Resume;
                }

                var parameters = new[]
                {
                    new SqlParameter("@ApplicationID", id)
                };

                // Execute stored procedure and read result set using the properly configured DbSet
                var result = await _context.Set<SpResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteApplication] @ApplicationID", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { error = "Unexpected error: No result from stored procedure." });
                }

                if (!spResult.Success)
                {
                    return BadRequest(new { error = spResult.Message });
                }

                // If database deletion was successful, attempt to delete the physical file
                DeleteFile(resumeFilenameToDelete);

                // Stored procedure indicated success
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
        }

        [HttpGet("DownloadResume/{filename}")]
        public async Task<IActionResult> DownloadResume(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return BadRequest("Filename cannot be empty.");
            }

            // SECURITY FIX: Sanitize the filename to prevent directory traversal attacks
            var sanitizedFilename = Path.GetFileName(filename);
            var filePath = Path.Combine(_env.WebRootPath, "Resumes", sanitizedFilename);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Resume '{sanitizedFilename}' not found.");
            }

            string contentType;
            var fileExtension = Path.GetExtension(sanitizedFilename).ToLowerInvariant();
            switch (fileExtension)
            {
                case ".pdf":
                    contentType = "application/pdf";
                    break;
                case ".doc":
                case ".docx":
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                case ".jpg":
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                default:
                    contentType = "application/octet-stream";
                    break;
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, sanitizedFilename);
        }
    }
       
    // --- DTOs (Data Transfer Objects) ---
    // These DTOs are used for communication between the client and the API.

    public class GetAllApplicantsDto
    {
        public int ApplicationID { get; set; }
        public string? ApplicantName { get; set; }
        public string? Email { get; set; }
        public string? Resume { get; set; }
        public DateTime ApplicationDate { get; set; }
        public string? JobTitle { get; set; }
        public string? ApplicationStatus { get; set; }
    }

    public class AddApplicationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AddApplicationStatusDto
    {
        [Required]
        public string StatusName { get; set; } = string.Empty;
    }

    public class AddApplicationStatusResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

   
 

    public class UpdateApplicationStatusDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int StatusID { get; set; }
        [Required]
        public string NewStatusName { get; set; } = string.Empty;
    }

    public class UpdateApplicationStatusResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? StatusID { get; set; }
        public string? NewStatusName { get; set; }
    }

    public class UpdateApplicationDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int ApplicationID { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int NewStatusID { get; set; }
        public string? feedback { get; set; }
    }

    public class UpdateApplicationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? ApplicationID { get; set; }
        public int? NewStatusID { get; set; }
        public string? NewStatusName { get; set; }
    }

    public class SpResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}