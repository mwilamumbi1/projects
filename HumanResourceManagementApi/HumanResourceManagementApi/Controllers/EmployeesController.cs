using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Core.HumanResourceManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly HRDataContext _context;

        public EmployeesController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetAllInfo")]
        public async Task<IActionResult> GetAllInfo()
        {
            var employees = await _context.Set<GetAllEmployeesDto>()
                .FromSqlRaw("EXEC HR.GetAllEmployees")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("GetPersonalInfo")]
        public async Task<IActionResult> GetPersonalInfo()
        {
            var employees = await _context.Set<GetEmployeePersonalInfoDto>()
                .FromSqlRaw("EXEC HR.GetAllEmployees")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("GetJobInfo")]
        public async Task<IActionResult> GetJobInfo()
        {
            var employees = await _context.Set<GetEmployeeJobInfoDto>()
                .FromSqlRaw("EXEC HR.GetAllEmployees")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("GetEmergencyInfo")]
        public async Task<IActionResult> GetEmergencyInfo()
        {
            var employees = await _context.Set<GetEmployeeEmergencyInfoDto>()
                .FromSqlRaw("EXEC HR.GetAllEmployees")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("GetBankInfo")]
        public async Task<IActionResult> GetBankInfo()
        {
            var employees = await _context.Set<GetEmployeeBankInfoDto>()
                .FromSqlRaw("EXEC HR.GetAllEmployees")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            var result = await _context.Set<GetAllEmployeesDto>()
                .FromSqlRaw("EXEC HR.GetEmployeeByID {0}", id)
                .ToListAsync();

            if (result == null || !result.Any())
            {
                return NotFound($"Employee with ID {id} not found.");
            }

            var employeeDto = result.FirstOrDefault();
            return Ok(employeeDto);
        }
        [HttpPost("InsertEmployee")]
        public async Task<IActionResult> InsertEmployee([FromBody] InsertEmployeeDto employee)
        {
            var parameters = new[]
            {
        new SqlParameter("@FirstName", employee.FirstName ?? (object)DBNull.Value),
        new SqlParameter("@LastName", employee.LastName ?? (object)DBNull.Value),
        new SqlParameter("@DateOfBirth", (object)employee.DateOfBirth ?? DBNull.Value),
        new SqlParameter("@Gender", employee.Gender ?? (object)DBNull.Value),
        new SqlParameter("@HireDate", (object)employee.HireDate ?? DBNull.Value),
        new SqlParameter("@DepartmentID", (object)employee.DepartmentID ?? DBNull.Value),
        new SqlParameter("@PositionID", (object)employee.PositionID ?? DBNull.Value),
        new SqlParameter("@ManagerID", (object)employee.ManagerID ?? DBNull.Value),
        new SqlParameter("@Salary", (object)employee.Salary ?? DBNull.Value),
        new SqlParameter("@Email", employee.Email ?? (object)DBNull.Value),
        new SqlParameter("@NRC", employee.NRC ?? (object)DBNull.Value),
        new SqlParameter("@TPIN", employee.TPIN ?? (object)DBNull.Value),
        new SqlParameter("@MaritalStatus", employee.MaritalStatus ?? (object)DBNull.Value),
        new SqlParameter("@Address", employee.Address ?? (object)DBNull.Value),
        new SqlParameter("@PhoneNumber", employee.PhoneNumber ?? (object)DBNull.Value),
        new SqlParameter("@EmergencyContactName", employee.EmergencyContactName ?? (object)DBNull.Value),
        new SqlParameter("@EmergencyContactNumber", employee.EmergencyContactNumber ?? (object)DBNull.Value),
        new SqlParameter("@BankName", employee.BankName ?? (object)DBNull.Value),
        new SqlParameter("@BankAccountNumber", employee.BankAccountNumber ?? (object)DBNull.Value),
        new SqlParameter("@ContractID", (object)employee.ContractID ?? DBNull.Value),
        new SqlParameter("@NapsaRegNumber", employee.NapsaRegNumber ?? (object)DBNull.Value),
        new SqlParameter("@Province", employee.Province ?? (object)DBNull.Value),
        new SqlParameter("@Qualifications", employee.Qualifications ?? (object)DBNull.Value),
        new SqlParameter("@StatusID", (object)employee.StatusID ?? DBNull.Value),
        new SqlParameter("@ActionWord", employee.ActionWord ?? (object)DBNull.Value)
    };

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var result = await _context.Set<AddEmployeeResult>()
                    .FromSqlRaw(
                        "EXEC HR.AddEmployee @FirstName, @LastName, @DateOfBirth, @Gender, @HireDate, @DepartmentID, @PositionID, @ManagerID, @Salary, @Email, @NRC, @TPIN, @MaritalStatus, @Address, @PhoneNumber, @EmergencyContactName, @EmergencyContactNumber, @BankName, @BankAccountNumber, @ContractID, @NapsaRegNumber, @Province, @Qualifications, @StatusID, @ActionWord",
                        parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { error = "Unexpected error: No result from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { error = spResult.Message });

                return Ok(new { message = spResult.Message, employeeId = spResult.NewEmployeeID });
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


        [HttpPut("UpdatePersonalInfo/{id}")]
        public async Task<IActionResult> UpdatePersonalInfo(int id, [FromBody] UpdateEmployeePersonalInfo employee)
        {
            if (id != employee.EmployeeID)
                return BadRequest("Employee ID in URL does not match EmployeeID in request body.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", employee.EmployeeID),
                new SqlParameter("@FirstName", (object)employee.FirstName ?? DBNull.Value),
                new SqlParameter("@LastName", (object)employee.LastName ?? DBNull.Value),
                new SqlParameter("@DateOfBirth", (object)employee.DateOfBirth ?? DBNull.Value),
                new SqlParameter("@Gender", (object)employee.Gender ?? DBNull.Value),
                new SqlParameter("@HireDate", DBNull.Value),
                new SqlParameter("@DepartmentID", DBNull.Value),
                new SqlParameter("@PositionID", DBNull.Value),
                new SqlParameter("@ManagerID", DBNull.Value),
                new SqlParameter("@Salary", DBNull.Value),
                new SqlParameter("@Email", (object)employee.Email ?? DBNull.Value),
                new SqlParameter("@NRC", (object)employee.NRC ?? DBNull.Value),
                new SqlParameter("@TPIN", DBNull.Value),
                new SqlParameter("@MaritalStatus", (object)employee.MaritalStatus ?? DBNull.Value),
                new SqlParameter("@Address", (object)employee.Address ?? DBNull.Value),
                new SqlParameter("@Province", (object)employee.Province ?? DBNull.Value),
             
                new SqlParameter("@PhoneNumber", (object)employee.PhoneNumber ?? DBNull.Value),
                new SqlParameter("@EmergencyContactName", DBNull.Value),
                new SqlParameter("@EmergencyContactNumber", DBNull.Value),
                new SqlParameter("@BankName", DBNull.Value),
                new SqlParameter("@BankAccountNumber", DBNull.Value),
                new SqlParameter("@ContractID", DBNull.Value),
                new SqlParameter("@NapsaRegNumber", DBNull.Value),
                new SqlParameter("@StatusID", DBNull.Value),
                new SqlParameter("@ActionWord", employee.ActionWord ?? "Update Personal Info")
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

                var result = await _context.Set<UpdateEmployeeResult>()
                    .FromSqlRaw(
                        "EXEC HR.UpdateEmployee @EmployeeID, @FirstName, @LastName, @DateOfBirth, @Gender, @HireDate, @DepartmentID, @PositionID, @ManagerID, @Salary, @Email, @NRC, @TPIN, @MaritalStatus, @Address, @Province, @Qualification, @PhoneNumber, @EmergencyContactName, @EmergencyContactNumber, @BankName, @BankAccountNumber, @ContractID, @NapsaRegNumber, @StatusID, @ActionWord",
                        parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null) return StatusCode(500, new { error = "No result from stored procedure." });
                if (!spResult.Success) return BadRequest(new { error = spResult.Message });

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

        [HttpPut("UpdateJobInfo/{id}")]
        public async Task<IActionResult> UpdateJobInfo(int id, [FromBody] UpdateEmployeeJobInfo employee)
        {
            if (id != employee.EmployeeID)
                return BadRequest("Employee ID in URL does not match EmployeeID in request body.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", employee.EmployeeID),
                new SqlParameter("@FirstName", DBNull.Value),
                new SqlParameter("@LastName", DBNull.Value),
                new SqlParameter("@DateOfBirth", DBNull.Value),
                new SqlParameter("@Gender", DBNull.Value),
                new SqlParameter("@HireDate", (object)employee.HireDate ?? DBNull.Value),
                new SqlParameter("@DepartmentID", (object)employee.DepartmentID ?? DBNull.Value),
                new SqlParameter("@PositionID", (object)employee.PositionID ?? DBNull.Value),
                new SqlParameter("@ManagerID", (object)employee.ManagerID ?? DBNull.Value),
                new SqlParameter("@Salary", (object)employee.Salary ?? DBNull.Value),
                new SqlParameter("@Email", DBNull.Value),
                new SqlParameter("@NRC", DBNull.Value),
                new SqlParameter("@TPIN", (object)employee.TPIN ?? DBNull.Value),
                new SqlParameter("@MaritalStatus", DBNull.Value),
                new SqlParameter("@Address", DBNull.Value),
                new SqlParameter("@Province", DBNull.Value),
                 
                new SqlParameter("@PhoneNumber", DBNull.Value),
                new SqlParameter("@EmergencyContactName", DBNull.Value),
                new SqlParameter("@EmergencyContactNumber", DBNull.Value),
                new SqlParameter("@BankName", DBNull.Value),
                new SqlParameter("@BankAccountNumber", DBNull.Value),
                new SqlParameter("@ContractID", (object)employee.ContractID ?? DBNull.Value),
                new SqlParameter("@NapsaRegNumber", DBNull.Value),
                new SqlParameter("@StatusID", (object)employee.StatusID ?? DBNull.Value),
                new SqlParameter("@ActionWord", employee.ActionWord ?? "Update Job Info")
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

                var result = await _context.Set<UpdateEmployeeResult>()
                    .FromSqlRaw(
                        "EXEC HR.UpdateEmployee @EmployeeID, @FirstName, @LastName, @DateOfBirth, @Gender, @HireDate, @DepartmentID, @PositionID, @ManagerID, @Salary, @Email, @NRC, @TPIN, @MaritalStatus, @Address, @Province, @Qualification, @PhoneNumber, @EmergencyContactName, @EmergencyContactNumber, @BankName, @BankAccountNumber, @ContractID, @NapsaRegNumber, @StatusID, @ActionWord",
                        parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null) return StatusCode(500, new { error = "No result from stored procedure." });
                if (!spResult.Success) return BadRequest(new { error = spResult.Message });

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

        [HttpPut("UpdateBankInfo/{id}")]
        public async Task<IActionResult> UpdateBankInfo(int id, [FromBody] UpdateEmployeeBankInfo employee)
        {
            if (id != employee.EmployeeID)
                return BadRequest("Employee ID in URL does not match EmployeeID in request body.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", employee.EmployeeID),
                new SqlParameter("@FirstName", DBNull.Value),
                new SqlParameter("@LastName", DBNull.Value),
                new SqlParameter("@DateOfBirth", DBNull.Value),
                new SqlParameter("@Gender", DBNull.Value),
                new SqlParameter("@HireDate", DBNull.Value),
                new SqlParameter("@DepartmentID", DBNull.Value),
                new SqlParameter("@PositionID", DBNull.Value),
                new SqlParameter("@ManagerID", DBNull.Value),
                new SqlParameter("@Salary", DBNull.Value),
                new SqlParameter("@Email", DBNull.Value),
                new SqlParameter("@NRC", DBNull.Value),
                new SqlParameter("@TPIN", DBNull.Value),
                new SqlParameter("@MaritalStatus", DBNull.Value),
                new SqlParameter("@Address", DBNull.Value),
                new SqlParameter("@Province", DBNull.Value),
                new SqlParameter("@Qualification", DBNull.Value),
                new SqlParameter("@PhoneNumber", DBNull.Value),
                new SqlParameter("@EmergencyContactName", DBNull.Value),
                new SqlParameter("@EmergencyContactNumber", DBNull.Value),
                new SqlParameter("@BankName", (object)employee.BankName ?? DBNull.Value),
                new SqlParameter("@BankAccountNumber", (object)employee.BankAccountNumber ?? DBNull.Value),
                new SqlParameter("@ContractID", DBNull.Value),
                new SqlParameter("@NapsaRegNumber", (object)employee.NapsaRegNumber ?? DBNull.Value),
                new SqlParameter("@StatusID", DBNull.Value),
                new SqlParameter("@ActionWord", employee.ActionWord ?? "Update Bank Info")
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

                var result = await _context.Set<UpdateEmployeeResult>()
                    .FromSqlRaw(
                        "EXEC HR.UpdateEmployee @EmployeeID, @FirstName, @LastName, @DateOfBirth, @Gender, @HireDate, @DepartmentID, @PositionID, @ManagerID, @Salary, @Email, @NRC, @TPIN, @MaritalStatus, @Address, @Province, @Qualification, @PhoneNumber, @EmergencyContactName, @EmergencyContactNumber, @BankName, @BankAccountNumber, @ContractID, @NapsaRegNumber, @StatusID, @ActionWord",
                        parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null) return StatusCode(500, new { error = "No result from stored procedure." });
                if (!spResult.Success) return BadRequest(new { error = spResult.Message });

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

        [HttpPut("UpdateEmergencyInfo/{id}")]
        public async Task<IActionResult> UpdateEmergencyInfo(int id, [FromBody] UpdateEmployeeEmergencyInfo employee)
        {
            if (id != employee.EmployeeID)
                return BadRequest("Employee ID in URL does not match EmployeeID in request body.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@EmployeeID", employee.EmployeeID),
                new SqlParameter("@FirstName", DBNull.Value),
                new SqlParameter("@LastName", DBNull.Value),
                new SqlParameter("@DateOfBirth", DBNull.Value),
                new SqlParameter("@Gender", DBNull.Value),
                new SqlParameter("@HireDate", DBNull.Value),
                new SqlParameter("@DepartmentID", DBNull.Value),
                new SqlParameter("@PositionID", DBNull.Value),
                new SqlParameter("@ManagerID", DBNull.Value),
                new SqlParameter("@Salary", DBNull.Value),
                new SqlParameter("@Email", DBNull.Value),
                new SqlParameter("@NRC", DBNull.Value),
                new SqlParameter("@TPIN", DBNull.Value),
                new SqlParameter("@MaritalStatus", DBNull.Value),
                new SqlParameter("@Address", DBNull.Value),
                new SqlParameter("@Province", DBNull.Value),
                new SqlParameter("@Qualification", DBNull.Value),
                new SqlParameter("@PhoneNumber", DBNull.Value),
                new SqlParameter("@EmergencyContactName", (object)employee.EmergencyContactName ?? DBNull.Value),
                new SqlParameter("@EmergencyContactNumber", (object)employee.EmergencyContactNumber ?? DBNull.Value),
                new SqlParameter("@BankName", DBNull.Value),
                new SqlParameter("@BankAccountNumber", DBNull.Value),
                new SqlParameter("@ContractID", DBNull.Value),
                new SqlParameter("@NapsaRegNumber", DBNull.Value),
                new SqlParameter("@StatusID", DBNull.Value),
                new SqlParameter("@ActionWord", employee.ActionWord ?? "Update Emergency Info")
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

                var result = await _context.Set<UpdateEmployeeResult>()
                    .FromSqlRaw(
                        "EXEC HR.UpdateEmployee @EmployeeID, @FirstName, @LastName, @DateOfBirth, @Gender, @HireDate, @DepartmentID, @PositionID, @ManagerID, @Salary, @Email, @NRC, @TPIN, @MaritalStatus, @Address, @Province, @Qualification, @PhoneNumber, @EmergencyContactName, @EmergencyContactNumber, @BankName, @BankAccountNumber, @ContractID, @NapsaRegNumber, @StatusID, @ActionWord",
                        parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null) return StatusCode(500, new { error = "No result from stored procedure." });
                if (!spResult.Success) return BadRequest(new { error = spResult.Message });

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
        [HttpPost("UpdateProfilePicture")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfilePicture([FromForm] UpdateProfilePictureRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            byte[] photoBytes = null;

            if (request.PhotoFile != null && request.PhotoFile.Length > 0)
            {
                try
                {
                    string fileExtension = Path.GetExtension(request.PhotoFile.FileName).ToLowerInvariant();
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(new { error = "Invalid file type. Only JPG, PNG, and GIF images are allowed." });
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await request.PhotoFile.CopyToAsync(memoryStream);
                        photoBytes = memoryStream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Failed to process photo file: " + ex.Message });
                }
            }
            else
            {
                return BadRequest(new { error = "Photo file is required." });
            }

            var parameters = new[]
            {
        new SqlParameter("@EmployeeID", request.EmployeeID),
        new SqlParameter("@Profile", SqlDbType.VarBinary, -1) { Value = photoBytes }
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

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [HR].[UpdateProfilePicture] @EmployeeID, @Profile",
                    parameters);

                await connection.CloseAsync();

                return Ok(new { message = "Profile picture updated successfully." });
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


        [HttpGet("profile/{employeeId}")]
        public async Task<IActionResult> GetProfilePicture(int employeeId)
        {
            try
            {
                var results = await _context.Set<ProfilePictureDto>()
                    .FromSqlRaw("EXEC [HR].[GetEmployeeProfilePicture] @EmployeeID = {0}", employeeId)
                    .ToListAsync();

                var profileData = results.FirstOrDefault();

                if (profileData == null || profileData.Profile == null || profileData.Profile.Length == 0)
                {
                    return NotFound(new { message = "Profile picture not found for this employee." });
                }

                string contentType = "image/jpeg";
                return File(profileData.Profile, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal server error occurred: " + ex.Message });
            }
        }

        [HttpDelete("DeleteEmployee/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
        new SqlParameter("@EmployeeID", id),
        new SqlParameter("@CallingUserID", currentUserId)
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

                var result = await _context.Set<DeleteEmployeeResult>()
                    .FromSqlRaw("EXEC HR.DeleteEmployee @EmployeeID, @CallingUserID", parameters)
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
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }
    }


        public class UpdateEmployeeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteEmployeeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GetEmployeePersonalInfoDto
    {
        public int? EmployeeID { get; set; }
        public string? ManagerName { get; set; }
        public string? MaritalStatus { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Gender { get; set; }
        public string? NRC { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Address { get; set; }
        public string? TPIN { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class GetEmployeeBankInfoDto
    {
        public int? EmployeeID { get; set; }
        public string? Gender { get; set; }
        public string? FullName { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? NapsaRegNumber { get; set; }
        public decimal? Salary { get; set; }
    }

    public class GetEmployeeJobInfoDto
    {
        public int? EmployeeID { get; set; }
        public string? FullName { get; set; }
        public DateOnly? HireDate { get; set; }
        public string? DepartmentName { get; set; }
        public string? Title { get; set; }
        public string? StatusName { get; set; }
        public string? TPIN { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContractName { get; set; }
        public string? NapsaRegNumber { get; set; }
        public string? Qualifications { get; set; }
        public string? Province { get; set; }
    }

    public class GetEmployeeEmergencyInfoDto
    {
        public int? EmployeeID { get; set; }
        public string? FullName { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
    }

    public class GetAllEmployeesDto
    {
        public int? EmployeeID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public DateTime? HireDate { get; set; }
        public int? DepartmentID { get; set; }
        public string? DepartmentName { get; set; }
        public int? PositionID { get; set; }
        public string? Title { get; set; }
        public int? ManagerID { get; set; }
        public string? ManagerName { get; set; }
        public decimal? Salary { get; set; }
        public string? Email { get; set; }
        public string? NRC { get; set; }
        public string? TPIN { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public int? ContractID { get; set; }
        public string? ContractName { get; set; }
        public string? NapsaRegNumber { get; set; }
        public int? StatusID { get; set; }
        public string? StatusName { get; set; }

        public string? Qualifications { get; set; }
        public string? Province { get; set; }

    }


    public class AddEmployeeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int NewEmployeeID { get; set; }
    }

    public class InsertEmployeeDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public DateTime? HireDate { get; set; }
        public int? DepartmentID { get; set; }
        public int? PositionID { get; set; }
        public int? ManagerID { get; set; }
        public decimal? Salary { get; set; }
        public int? StatusID { get; set; }
        public string? Email { get; set; }
        public string? NRC { get; set; }
        public string? TPIN { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public int? ContractID { get; set; }
        public string? NapsaRegNumber { get; set; }
        public string? ActionWord { get; set; }
        public string? Province { get; set; }
        public string? Qualifications { get; set; }
    }



    public class UpdateEmployeePersonalInfo
    {
        public int EmployeeID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Email { get; set; }
        public string? NRC { get; set; }
        public string? MaritalStatus { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public int? CallingUserID { get; set; }
        public string? ActionWord { get; set; }
        public string? Province { get; set; }
     
    }

    public class UpdateEmployeeJobInfo
    {
        public int EmployeeID { get; set; }
        public DateTime? HireDate { get; set; }
        public int? DepartmentID { get; set; }
        public int? PositionID { get; set; }
        public int? ManagerID { get; set; }
        public decimal? Salary { get; set; }
        public int? ContractID { get; set; }
        public string? TPIN { get; set; }
        public int? StatusID { get; set; }
        public int CallingUserID { get; set; }
        public string? ActionWord { get; set; }
    }

    public class UpdateProfilePictureRequest
    {
        [Required]
        public int EmployeeID { get; set; }

        [Required]
        public IFormFile PhotoFile { get; set; }
    }

    public class UpdateProfileResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public class ProfilePictureDto
    {
        public int EmployeeID { get; set; }
        public byte[]? Profile { get; set; } // It's a byte array now
    }
    public class UpdateEmployeeBankInfo
    {
        public int EmployeeID { get; set; }
        public string? BankName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? NapsaRegNumber { get; set; }
        public int CallingUserID { get; set; }
        public string? ActionWord { get; set; }
    }

    public class UpdateEmployeeEmergencyInfo
    {
        public int EmployeeID { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public int CallingUserID { get; set; }
        public string? ActionWord { get; set; }
    }
}