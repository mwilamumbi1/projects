using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace Core.HumanResourceManagementApi.Controllers
{
    // --- 1. Consolidated DTOs ---

    public class AddUserDto
    {
        public int EmployeeID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public string CreatedBy { get; set; }
        public int? StatusId { get; set; }
    }

    public class CompanyProfileDto
    {
        public string CompanyName { get; set; }
        public string CompanyEmail { get; set; }
        public string Motto { get; set; }
        // New SMTP Fields from your table
        public string EmailServerHost { get; set; }
        public int EmailServerPort { get; set; }
        public string EmailUsername { get; set; }
        public bool UseSSL { get; set; }
    }
    public class EmployeeAccountStatusDto
    {
        public int EmployeeID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int? UserID { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public string? RoleName { get; set; }
        public string? Name { get; set; } // Maps to s.Name (Status)
        public string AccountStatus { get; set; }
    }

    // --- 2. Controller Implementation ---

    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeAccountController : ControllerBase
    {
        private readonly HRDataContext _context;

        public EmployeeAccountController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetAllEmployeesAccountStatus")]
        public async Task<IActionResult> GetAllEmployeesAccountStatus()
        {
            try
            {
                var result = await _context.Set<EmployeeAccountStatusDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllEmployeesWithAccountStatus]")
                    .ToListAsync();

                if (result == null || !result.Any())
                {
                    return NotFound(new { Success = false, Message = "No records found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount([FromBody] AddUserDto userDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // A. FETCH SENDER DETAILS
            var profile = await GetCompanyProfileAsync();
            if (profile == null)
                return BadRequest(new { success = false, message = "Company Profile not found." });

            // B. GENERATE CREDENTIALS
            string generatedPassword = GeneratePassword(12);
            var passwordHashBytes = ComputeHash(generatedPassword);

            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                // C. EXECUTE STORED PROCEDURE
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "[core].[AddUser]";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@EmployeeID", userDto.EmployeeID));
                command.Parameters.Add(new SqlParameter("@PasswordHash", passwordHashBytes));
                command.Parameters.Add(new SqlParameter("@RoleId", userDto.RoleId));
                command.Parameters.Add(new SqlParameter("@CreatedBy", userDto.CreatedBy));
                command.Parameters.Add(new SqlParameter("@StatusId", (object)userDto.StatusId ?? DBNull.Value));

                var userIdParam = new SqlParameter("@UserID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(userIdParam);

                await command.ExecuteNonQueryAsync();
                var newUserId = (int)userIdParam.Value;

                transaction.Commit();

                // D. SEND NOTIFICATION
                await SendEmailNotification(profile, userDto, generatedPassword);

                return Ok(new { success = true, userId = newUserId, message = "Account created and email sent." });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, new { success = false, message = ex.Message });
            }
            finally
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();
            }
        }

        // --- 3. Private Helper Methods ---

        private async Task<CompanyProfileDto?> GetCompanyProfileAsync()
        {
            // Selecting the extra SMTP columns now
            return await _context.Set<CompanyProfileDto>()
                .FromSqlRaw(@"SELECT TOP 1 
                        CompanyName, 
                        CompanyEmail, 
                        Motto, 
                        EmailServerHost, 
                        EmailServerPort, 
                        EmailUsername, 
                        UseSSL 
                      FROM [HR].[CompanyProfile]")
                .FirstOrDefaultAsync();
        }

        private async Task SendEmailNotification(CompanyProfileDto profile, AddUserDto user, string password)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(profile.CompanyEmail, profile.CompanyName),
                    Subject = $"{profile.CompanyName} - Your New Account Credentials",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; border: 1px solid #eee; padding: 20px;'>
                            <h2 style='color: #2c3e50;'>{profile.CompanyName}</h2>
                            <p><em>{profile.Motto}</em></p>
                            <hr/>
                            <p>Hello <strong>{user.FullName}</strong>,</p>
                            <p>Your account is ready. Temporary password:</p>
                            <div style='background: #f9f9f9; padding: 15px; font-size: 20px; text-align: center; border: 1px dashed #ccc;'>
                                <strong>{password}</strong>
                            </div>
                            <p>Please change this password upon login.</p>
                        </div>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(user.Email);

                using var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(profile.CompanyEmail, "moznphzinpfdtsgr"),
                    EnableSsl = true,
                };

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SMTP Error: " + ex.Message);
            }
        }

        private string GeneratePassword(int length)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private byte[] ComputeHash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            }
        }
    }
}