using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Core.HumanResourceManagementApi.Services;

namespace Core.HumanResourceManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly HRDataContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        // CORRECTED CONSTRUCTOR with IUserService
        public LoginController(HRDataContext context, IConfiguration configuration, IEmailService emailService, IUserService userService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _userService = userService;
        }

        [HttpGet("LOGS")]
        public async Task<IActionResult> GetAllLogs()
        {
            var employees = await _context.Set<GetAuditLogDto>()
                .FromSqlRaw("EXEC HR.GetAllAuditLog")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                return BadRequest(new { success = false, message = "Email and password are required." });

            SqlDataReader? reader = null;
            try
            {
                if (_context.Database.GetDbConnection() == null)
                {
                    return StatusCode(500, new { success = false, message = "Database connection is not available." });
                }

                var emailParam = new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = loginDto.Email };

                var passwordBytes = HashHelper.ComputeSha256Hash(loginDto.Password);
                var passwordParam = new SqlParameter("@Password", SqlDbType.VarBinary, 64)
                {
                    Value = passwordBytes
                };

                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "core.LoginUser";
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add(emailParam);
                command.Parameters.Add(passwordParam);

                if (_context.Database.GetDbConnection().State != ConnectionState.Open)
                {
                    await _context.Database.OpenConnectionAsync();
                }

                reader = (SqlDataReader?)await command.ExecuteReaderAsync();
                if (reader == null)
                {
                    return StatusCode(500, new { success = false, message = "Failed to execute stored procedure. Reader is null." });
                }

                var users = new List<UserResponseDto>();
                if (!reader.HasRows)
                {
                    return Unauthorized(new { success = false, message = "Invalid email or password." });
                }

                while (await reader.ReadAsync())
                {
                    users.Add(new UserResponseDto
                    {
                        UserID = GetSafeInt32(reader, "UserID"),
                        FullName = GetSafeString(reader, "FullName"),
                        Email = GetSafeString(reader, "Email"),
                        RoleId = GetSafeInt32(reader, "RoleId"),
                        RoleName = GetSafeString(reader, "RoleName"),
                        StatusId = GetSafeInt32(reader, "StatusId"),
                        CreatedAt = GetSafeDateTime(reader, "CreatedAt"),
                        PasswordExpiryDate = GetSafeNullableDateTime(reader, "PasswordExpiryDate"),
                        ComplexityId = GetSafeInt32(reader, "ComplexityId"),
                        CreatedBy = GetSafeString(reader, "CreatedBy"),
                        EmployeeID = GetSafeNullableInt32(reader, "EmployeeID"),
                    });
                }

                if (users.Count == 0)
                    return Unauthorized(new { success = false, message = "Invalid email or password." });

                var user = users.First();
                List<string> permissions = new();
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permissions.Add(GetSafeString(reader, "PermissionName"));
                    }
                }
                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    success = true,
                    token,
                    user,
                    permissions
                });
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {sqlEx.Message}", errorCode = sqlEx.Number });
            }
            catch (InvalidOperationException ioEx)
            {
                return StatusCode(500, new { success = false, message = $"Operation error: {ioEx.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}", stackTrace = ex.StackTrace });
            }
            finally
            {
                if (reader != null && !reader.IsClosed)
                {
                    await reader.DisposeAsync();
                }
                if (_context.Database.GetDbConnection().State == ConnectionState.Open)
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }

        // **NEW ENDPOINT ADDED HERE**
        [Authorize] // This ensures only authenticated users can call this endpoint
        [HttpGet("Permissions")]
        public async Task<IActionResult> GetPermissions()
        {
            // The `User` object is automatically populated with claims from the JWT token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("User ID claim not found or invalid.");
            }

            // Call the service to get the latest permissions from the database
            var permissions = await _userService.GetPermissionsByUserIdAsync(userId);

            if (permissions == null || !permissions.Any())
            {
                return NotFound("No permissions found for this user.");
            }

            return Ok(permissions);
        }

        // Helper methods to safely read data from SqlDataReader... (unchanged)
        private static int GetSafeInt32(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return 0;
            }
        }

        private static int? GetSafeNullableInt32(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private static string GetSafeString(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return string.Empty;
            }
        }

        private static DateTime GetSafeDateTime(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return DateTime.MinValue;
            }
        }

        private static DateTime? GetSafeNullableDateTime(SqlDataReader reader, string columnName)
        {
            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        private string GenerateJwtToken(UserResponseDto user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationHours = int.Parse(jwtSettings["ExpirationInHours"] ?? "24");

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured");
            }

            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, user.RoleName ?? string.Empty),
                    new Claim("RoleId", user.RoleId.ToString()),
                    new Claim("StatusId", user.StatusId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(expirationHours),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] AddUserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string generatedPassword = PasswordGenerator.GenerateRandomPassword();
            var passwordHashBytes = HashHelper.ComputeSha256Hash(generatedPassword);

            System.Diagnostics.Debug.WriteLine($"C# Generated Password: {generatedPassword}");
            System.Diagnostics.Debug.WriteLine($"C# Generated Hash for Add: {BitConverter.ToString(passwordHashBytes).Replace("-", "").ToLowerInvariant()}");

            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            SqlTransaction transaction = null;
            try
            {
                transaction = (SqlTransaction)connection.BeginTransaction();
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "core.AddUser";
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.Add(new SqlParameter("@FullName", userDto.FullName));
                command.Parameters.Add(new SqlParameter("@Email", userDto.Email));
                command.Parameters.Add(new SqlParameter("@Password", generatedPassword));
                command.Parameters.Add(new SqlParameter("@PasswordHash", passwordHashBytes));
                command.Parameters.Add(new SqlParameter("@RoleId", userDto.RoleId));
                command.Parameters.Add(new SqlParameter("@CreatedBy", userDto.CreatedBy));
                command.Parameters.Add(new SqlParameter("@StatusId", (object)userDto.StatusId ?? DBNull.Value));

                var userIdParam = new SqlParameter("@UserID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(userIdParam);

                await command.ExecuteNonQueryAsync();

                var newUserId = (int)userIdParam.Value;
                transaction.Commit();

                string subject = "Your New Account Credentials";
                string plainTextContent = $"Hello {userDto.FullName},\n\nYour account has been created successfully. Your temporary password is: {generatedPassword}\n\nPlease log in and change your password as soon as possible for security reasons.\n\nRegards,\nPSL Dev Team";
                string htmlContent = $@"
                    <p>Hello <strong>{userDto.FullName}</strong>,</p>
                    <p>Your account has been created successfully. Your temporary password is:</p>
                    <h3><strong>{generatedPassword}</strong></h3>
                    <p>it will expire in 3 months Please log in and change your password as soon as possible for security reasons.</p>
                    <p>Regards,<br/>PSL Dev Team</p>
                ";

                try
                {
                    await _emailService.SendEmailAsync(userDto.Email, subject, plainTextContent, htmlContent);
                }
                catch (Exception emailEx)
                {
                    Console.WriteLine($"Error sending welcome email to {userDto.Email}: {emailEx.Message}");
                }

                return Ok(new { success = true, message = "User added successfully and credentials sent to email.", userId = newUserId });
            }
            catch (SqlException sqlEx)
            {
                transaction?.Rollback();
                return StatusCode(400, new { success = false, message = sqlEx.Message });
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                return StatusCode(500, new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }
    }
}

public static class HashHelper
{
    public static byte[] ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(rawData);
            return sha256.ComputeHash(inputBytes);
        }
    }

    public static string ComputeSha256HashHex(string rawData)
    {
        var hash = ComputeSha256Hash(rawData);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public static bool VerifyPassword(string plainTextPassword, byte[] storedHash)
    {
        var computedHash = ComputeSha256Hash(plainTextPassword);
        return computedHash.SequenceEqual(storedHash);
    }
}

public class LoginDto
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public class UserResponseDto
{
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public int ComplexityId { get; set; }
    public string? CreatedBy { get; set; }
    public int? EmployeeID { get; set; }
}

public class GetAuditLogDto
{
    public int AuditID { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int PrimaryKeyValue { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public int UserID { get; set; }
    public string? RoleName { get; set; }
    public string? EmployeeName { get; set; }
}
// You can place this in a "Models" or "Dtos" folder.

public class AddUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int CreatedBy { get; set; }
    public int? StatusId { get; set; }
}