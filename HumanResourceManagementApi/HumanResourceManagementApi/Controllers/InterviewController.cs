using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Controllers
{
    // ============================================
    // INTERVIEW CONTROLLER
    // ============================================
    [ApiController]
    [Route("api/[controller]")]
    public class InterviewController : ControllerBase
    {
        private readonly HRDataContext _context;

        public InterviewController(HRDataContext context)
        {
            _context = context;
        }

        [HttpPost("AddInterview")]
        public async Task<IActionResult> AddInterview([FromBody] AddInterviewDto dto)
        {
            if (dto.ApplicationID <= 0)
                return BadRequest(new { success = false, message = "Application ID must be a positive number." });

            if (dto.InterviewDate == default)
                return BadRequest(new { success = false, message = "Interview Date is required." });

            if (string.IsNullOrWhiteSpace(dto.Interviewers))
                return BadRequest(new { success = false, message = "At least one Interviewer ID is required." });

            // Fetch company profile for SMTP settings
            var profile = await GetCompanyProfileAsync();
            if (profile == null)
                return BadRequest(new { success = false, message = "Company Profile not found." });

            // Get application details (including applicant email)
            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.ApplicationID == dto.ApplicationID);

            if (application == null)
                return NotFound(new { success = false, message = "Application not found." });

            // Parse interviewer IDs
            var interviewerIds = dto.Interviewers.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            // Get interviewer emails
            if (interviewerIds.Any())
            {
                var idParams = string.Join(",", interviewerIds.Select((id, index) => $"@p{index}"));
                var sqlParams = interviewerIds.Select((id, index) => new SqlParameter($"@p{index}", id)).ToArray();

                var interviewerEmails = await _context.Set<EmployeeEmailDto>()
                    .FromSqlRaw($@"SELECT e.Email, e.FirstName, e.LastName 
                                   FROM [HR].[Employee] e 
                                   WHERE e.EmployeeID IN ({idParams})", sqlParams)
                    .ToListAsync();

                // Send emails to interviewers
                foreach (var interviewer in interviewerEmails)
                {
                    await SendEmailToInterviewer(profile, interviewer.Email, application.ApplicantName, dto.InterviewDate!.Value);
                }
            }

            var parameters = new[]
            {
                new SqlParameter("@ApplicationID", dto.ApplicationID),
                new SqlParameter("@InterviewDate", dto.InterviewDate),
                new SqlParameter("@Interviewers", dto.Interviewers.Trim()),
                new SqlParameter("@Feedback", string.IsNullOrWhiteSpace(dto.Feedback) ? (object)DBNull.Value : dto.Feedback.Trim())
            };

            try
            {
                var result = await _context.Set<AddInterviewResult>()
                    .FromSqlRaw("EXEC [HR].[AddInterview] @ApplicationID, @InterviewDate, @Interviewers, @Feedback", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "Unexpected error: No result from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                // Send email to applicant
                if (!string.IsNullOrWhiteSpace(application.Email))
                {
                    await SendEmailToApplicant(profile, application, dto.InterviewDate!.Value, dto.EmailMessage);
                }

                return Ok(new { success = true, message = spResult.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPut("UpdateInterview")]
        public async Task<IActionResult> UpdateInterview([FromBody] UpdateInterviewDto dto)
        {
            if (dto.InterviewID <= 0)
                return BadRequest(new { success = false, message = "Interview ID is required and must be a positive integer." });

            // Fetch company profile for SMTP settings
            var profile = await GetCompanyProfileAsync();
            if (profile == null)
                return BadRequest(new { success = false, message = "Company Profile not found." });

            var existingInterview = await _context.Interviews
                .FirstOrDefaultAsync(i => i.InterviewID == dto.InterviewID);

            if (existingInterview == null)
                return NotFound(new { success = false, message = "Interview not found." });

            var application = await _context.Applications
                .FirstOrDefaultAsync(a => a.ApplicationID == existingInterview.ApplicationID);

            if (application == null)
                return NotFound(new { success = false, message = "Associated application not found." });

            // Determine the interview date to use
            var interviewDate = dto.InterviewDate ?? existingInterview.InterviewDate;

            if (!interviewDate.HasValue)
            {
                return BadRequest(new { success = false, message = "Interview date is required." });
            }

            // Parse interviewer IDs (use updated interviewers if provided, otherwise use existing)
            var interviewersToUse = !string.IsNullOrWhiteSpace(dto.Interviewers)
                ? dto.Interviewers
                : existingInterview.Interviewers;

            var interviewerIds = interviewersToUse.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToList();

            // Get interviewer emails
            if (interviewerIds.Any())
            {
                var idParams = string.Join(",", interviewerIds.Select((id, index) => $"@p{index}"));
                var sqlParams = interviewerIds.Select((id, index) => new SqlParameter($"@p{index}", id)).ToArray();

                var interviewerEmails = await _context.Set<EmployeeEmailDto>()
                    .FromSqlRaw($@"SELECT e.Email, e.FirstName, e.LastName 
                                   FROM [HR].[Employee] e 
                                   WHERE e.EmployeeID IN ({idParams})", sqlParams)
                    .ToListAsync();

                // Send emails to interviewers
                if (interviewerEmails.Any())
                {
                    foreach (var interviewer in interviewerEmails)
                    {
                        await SendEmailToInterviewerUpdate(profile, interviewer.Email, application.ApplicantName, interviewDate.Value);
                    }
                }
            }

            var parameters = new[]
            {
                new SqlParameter("@InterviewID", dto.InterviewID),
                new SqlParameter("@ApplicationID", dto.ApplicationID.HasValue ? (object)dto.ApplicationID.Value : DBNull.Value),
                new SqlParameter("@InterviewDate", dto.InterviewDate.HasValue ? (object)dto.InterviewDate.Value : DBNull.Value),
                new SqlParameter("@Interviewers", string.IsNullOrWhiteSpace(dto.Interviewers) ? (object)DBNull.Value : dto.Interviewers.Trim()),
                new SqlParameter("@Feedback", string.IsNullOrWhiteSpace(dto.Feedback) ? (object)DBNull.Value : dto.Feedback.Trim())
            };

            try
            {
                var result = await _context.Set<UpdateInterviewResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateInterview] @InterviewID, @ApplicationID, @InterviewDate, @Interviewers, @Feedback", parameters)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "Unexpected error: No result from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                // Send email to applicant about interview update
                if (!string.IsNullOrWhiteSpace(application.Email))
                {
                    await SendEmailToApplicantUpdate(profile, application, interviewDate.Value, dto.EmailMessage);
                }

                return Ok(new
                {
                    success = true,
                    message = spResult.Message,
                    interviewId = spResult.InterviewID,
                    applicationId = spResult.ApplicationID,
                    interviewDate = spResult.InterviewDate,
                    interviewers = dto.Interviewers,
                    feedback = spResult.Feedback
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpDelete("DeleteInterview")]
        public async Task<IActionResult> DeleteInterview([FromBody] DeleteInterviewDto dto)
        {
            var param = new SqlParameter("@InterviewID", dto.InterviewID);

            try
            {
                var result = await _context.Set<DeleteInterviewResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteInterview] @InterviewID", param)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null)
                    return StatusCode(500, new { error = "Unexpected error: No result returned from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { message = spResult.Message });

                return Ok(new { message = spResult.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("GetInterviews")]
        public async Task<IActionResult> GetInterviewsWithNames()
        {
            try
            {
                var interviews = await _context.InterviewWithNamesDto
                    .FromSqlRaw("EXEC [HR].[GetAllInterviews]")
                    .ToListAsync();

                return Ok(new { success = true, data = interviews });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // --- Interview Email Helper Methods ---
        private async Task<CompanyProfileDto?> GetCompanyProfileAsync()
        {
            return await _context.Set<CompanyProfileDto>()
                .FromSqlRaw(@"SELECT TOP 1 CompanyName, CompanyEmail, Motto, EmailServerHost, EmailServerPort, EmailUsername, UseSSL FROM [HR].[CompanyProfile]")
                .FirstOrDefaultAsync();
        }

        private async Task SendEmailToApplicant(CompanyProfileDto profile, Application application, DateTime interviewDate, string? customMessage)
        {
            try
            {
                string bodyContent = string.IsNullOrWhiteSpace(customMessage)
                    ? $@"<p>Dear {application.ApplicantName},</p>
                         <p>We are pleased to inform you that your interview has been scheduled.</p>
                         <p><strong>Interview Date:</strong> {interviewDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>
                         <p>Please be prepared and arrive on time. We look forward to meeting you!</p>"
                    : $@"<p>Dear {application.ApplicantName},</p>
                         <p>{customMessage.Replace("\n", "<br>")}</p>
                         <p><strong>Interview Date:</strong> {interviewDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(profile.CompanyEmail, profile.CompanyName),
                    Subject = $"{profile.CompanyName} - Interview Scheduled",
                    Body = $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #333;'>{profile.CompanyName}</h2>
                                <p style='color: #666; font-style: italic;'>{profile.Motto}</p>
                                <hr style='border: 1px solid #eee;'/>
                                {bodyContent}
                                <p>Best regards,<br/>{profile.CompanyName} Team</p>
                              </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(application.Email);

                using var smtpClient = new SmtpClient(profile.EmailServerHost)
                {
                    Port = profile.EmailServerPort,
                    Credentials = new NetworkCredential(profile.EmailUsername, "ifqqjekbrqahodqr"),
                    EnableSsl = profile.UseSSL,
                };

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SMTP Error (Applicant): " + ex.Message);
            }
        }

        private async Task SendEmailToInterviewer(CompanyProfileDto profile, string interviewerEmail, string applicantName, DateTime interviewDate)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(profile.CompanyEmail, profile.CompanyName),
                    Subject = $"{profile.CompanyName} - Interview Assignment",
                    Body = $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #333;'>{profile.CompanyName}</h2>
                                <p style='color: #666; font-style: italic;'>{profile.Motto}</p>
                                <hr style='border: 1px solid #eee;'/>
                                <p>Hello,</p>
                                <p>You have been assigned to conduct an interview with <strong>{applicantName}</strong>.</p>
                                <p><strong>Interview Date:</strong> {interviewDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>
                                <p><strong>Applicant:</strong> {applicantName}</p>
                                <p>Please prepare accordingly and review the applicant's resume before the interview.</p>
                                <p>Best regards,<br/>{profile.CompanyName} HR Team</p>
                              </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(interviewerEmail);

                using var smtpClient = new SmtpClient(profile.EmailServerHost)
                {
                    Port = profile.EmailServerPort,
                    Credentials = new NetworkCredential(profile.EmailUsername, "moznphzinpfdtsgr"),
                    EnableSsl = profile.UseSSL,
                };

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SMTP Error (Interviewer): " + ex.Message);
            }
        }

        private async Task SendEmailToApplicantUpdate(CompanyProfileDto profile, Application application, DateTime interviewDate, string? customMessage)
        {
            try
            {
                string bodyContent = string.IsNullOrWhiteSpace(customMessage)
                    ? $@"<p>Dear {application.ApplicantName},</p>
                         <p>Your interview details have been updated.</p>
                         <p><strong>Updated Interview Date:</strong> {interviewDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>
                         <p>Please note the changes and be prepared accordingly.</p>"
                    : $@"<p>Dear {application.ApplicantName},</p>
                         <p>{customMessage.Replace("\n", "<br>")}</p>
                         <p><strong>Updated Interview Date:</strong> {interviewDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(profile.CompanyEmail, profile.CompanyName),
                    Subject = $"{profile.CompanyName} - Interview Updated",
                    Body = $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #333;'>{profile.CompanyName}</h2>
                                <p style='color: #666; font-style: italic;'>{profile.Motto}</p>
                                <hr style='border: 1px solid #eee;'/>
                                {bodyContent}
                                <p>Best regards,<br/>{profile.CompanyName} Team</p>
                              </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(application.Email);

                using var smtpClient = new SmtpClient(profile.EmailServerHost)
                {
                    Port = profile.EmailServerPort,
                    Credentials = new NetworkCredential(profile.EmailUsername, "moznphzinpfdtsgr"),
                    EnableSsl = profile.UseSSL,
                };

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SMTP Error (Applicant Update): " + ex.Message);
            }
        }

        private async Task SendEmailToInterviewerUpdate(CompanyProfileDto profile, string interviewerEmail, string applicantName, DateTime interviewDate)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(profile.CompanyEmail, profile.CompanyName),
                    Subject = $"{profile.CompanyName} - Interview Updated",
                    Body = $@"<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                <h2 style='color: #333;'>{profile.CompanyName}</h2>
                                <p style='color: #666; font-style: italic;'>{profile.Motto}</p>
                                <hr style='border: 1px solid #eee;'/>
                                <p>Hello,</p>
                                <p>The interview details for <strong>{applicantName}</strong> have been updated.</p>
                                <p><strong>Updated Interview Date:</strong> {interviewDate:dddd, MMMM dd, yyyy 'at' hh:mm tt}</p>
                                <p><strong>Applicant:</strong> {applicantName}</p>
                                <p>Please note the changes and adjust your schedule accordingly.</p>
                                <p>Best regards,<br/>{profile.CompanyName} HR Team</p>
                              </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(interviewerEmail);

                using var smtpClient = new SmtpClient(profile.EmailServerHost)
                {
                    Port = profile.EmailServerPort,
                    Credentials = new NetworkCredential(profile.EmailUsername, "moznphzinpfdtsgr"),
                    EnableSsl = profile.UseSSL,
                };

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SMTP Error (Interviewer Update): " + ex.Message);
            }
        }
    }

    #region DTOs
    public class AddInterviewDto
    {
        public int? ApplicationID { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? Interviewers { get; set; }
        public string? Feedback { get; set; }
        public string? EmailMessage { get; set; }
    }

    public class UpdateInterviewDto
    {
        public int? InterviewID { get; set; }
        public int? ApplicationID { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? Interviewers { get; set; }
        public string? Feedback { get; set; }
        public string? EmailMessage { get; set; }
    }

    public class DeleteInterviewDto
    {
        public int? InterviewID { get; set; }
    }

    public class AddInterviewResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class UpdateInterviewResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? InterviewID { get; set; }
        public int? ApplicationID { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? Interviewers { get; set; }
        public string? Feedback { get; set; }
    }

    public class DeleteInterviewResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class InterviewWithNamesDto
    {
        public int? InterviewID { get; set; }
        public int? ApplicationID { get; set; }
        public string? ApplicantName { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? Interviewers { get; set; }
        public string? Feedback { get; set; }
    }

    public class EmployeeEmailDto
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
    #endregion
}