using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System; // For DateTime, DBNull
using System.Collections.Generic; // For IEnumerable
using System.Data;
using System.Linq;
using System.Security.Claims; // For FirstOrDefault, Any




namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeaveController : ControllerBase
    {
        private readonly HRDataContext _context;

        public LeaveController(HRDataContext context)
        {
            _context = context;
        }

        [HttpPut("ApproveLeaveRequest/{leaveRequestId}")]
        public async Task<IActionResult> ApproveLeaveRequest(int leaveRequestId)
        {
            if (leaveRequestId <= 0)
            {
                return BadRequest("LeaveRequestID must be a positive integer.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
                new SqlParameter("@LeaveRequestID", leaveRequestId)
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
                    "EXEC HR.usp_ApproveLeaveRequest @LeaveRequestID",
                    parameters);

                await connection.CloseAsync();

                return Ok(new { Message = "Leave request approved successfully" });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Message = "Failed to approve leave request", Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPost("AllocateAnnualLeave")]
        public async Task<IActionResult> AllocateAnnualLeave([FromBody] AllocateAnnualLeaveDto dto)
        {
            var parameters = new[]
            {
                new SqlParameter("@AllocationYear", dto.AllocationYear ?? (object)DBNull.Value)
            };

            try
            {
                var result = await _context.Set<AllocateAnnualLeaveResult>()
                    .FromSqlRaw("EXEC [HR].[AllocateAnnualLeave] @AllocationYear", parameters)
                    .ToListAsync();

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
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });
            }
        }


        [HttpDelete("DeleteLeaveStatus/{statusId}")]
        public async Task<IActionResult> DeleteLeaveStatus(int statusId)
        {
            var param = new SqlParameter("@StatusID", statusId);

            try
            {
                var result = await _context.Set<DeleteResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteLeaveStatus] @StatusID", param)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                {
                    return StatusCode(500, new { error = "No response from stored procedure." });
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
        }

        [HttpPost("ApproveLeaveRequestByBody")]
        public async Task<IActionResult> ApproveLeaveRequestByBody([FromBody] ApproveLeaveRequestDto approveLeaveDto)
        {
            if (approveLeaveDto == null || approveLeaveDto.LeaveRequestID <= 0)
            {
                return BadRequest("Valid LeaveRequestID is required.");
            }

            var parameters = new[]
            {
                new SqlParameter("@LeaveRequestID", approveLeaveDto.LeaveRequestID)
            };

            try
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC HR.usp_ApproveLeaveRequest @LeaveRequestID",
                    parameters);

                return Ok(new { Message = "Leave request approved successfully" });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Message = "Failed to approve leave request", Error = ex.Message });
            }
        }


        [HttpPost("AssignLeaveToContract")]
        public async Task<IActionResult> AssignLeaveToContract([FromBody] AssignLeaveToContractDto assignLeaveDto)
        {
            if (assignLeaveDto == null)
            {
                return BadRequest(new { Success = false, Message = "Assign leave data is required." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            try
            {
                var sql = "EXEC HR.AssignLeaveToContract @ContractID, @LeaveTypeID, @DaysAllocated";

                var parameters = new[]
                {
            new SqlParameter("@ContractID", assignLeaveDto.ContractID),
            new SqlParameter("@LeaveTypeID", assignLeaveDto.LeaveTypeID),
            new SqlParameter("@DaysAllocated", assignLeaveDto.DaysAllocated)
        };

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

                var result = await _context.AssignLeaveResults
                    .FromSqlRaw(sql, parameters)
                    .AsNoTracking()
                    .ToListAsync();

                await connection.CloseAsync();

                var procResult = result.FirstOrDefault();

                if (procResult == null)
                {
                    return StatusCode(500, new { Success = false, Message = "No result returned from the procedure." });
                }

                if (!procResult.Success)
                {
                    return BadRequest(new { success = false, message = procResult.Message });
                }
                return Ok(new { success = true, message = procResult.Message });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPut("UpdateLeaveStatus")]
        public async Task<IActionResult> UpdateLeaveStatus([FromBody] UpdateLeaveStatusDto dto)
        {
            if (dto.StatusID <= 0)
            {
                return BadRequest(new { success = false, message = "Status ID is required and must be a positive integer." });
            }
            if (string.IsNullOrWhiteSpace(dto.StatusName))
            {
                return BadRequest(new { success = false, message = "Status Name cannot be null or empty." });
            }
            if (dto.StatusName.Length > 50)
            {
                return BadRequest(new { success = false, message = "Status Name cannot exceed 50 characters." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
        new SqlParameter("@StatusID", dto.StatusID),
        new SqlParameter("@StatusName", dto.StatusName.Trim())
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

                var result = await _context.Set<UpdateLeaveStatusResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateLeaveStatus] @StatusID, @StatusName", parameters)
                    .ToListAsync();

                await connection.CloseAsync();

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
                    statusName = spResult.StatusName
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

        [HttpPut("UpdateLeaveType")]
        public async Task<IActionResult> UpdateLeaveType([FromBody] UpdateLeaveTypeDto dto)
        {
            if (dto.LeaveTypeID <= 0)
            {
                return BadRequest(new { success = false, message = "LeaveType ID must be a positive integer." });
            }
            if (string.IsNullOrWhiteSpace(dto.LeaveTypeName))
            {
                return BadRequest(new { success = false, message = "LeaveType Name is required." });
            }
            if (dto.LeaveTypeName.Length > 255)
            {
                return BadRequest(new { success = false, message = "LeaveType Name cannot exceed 255 characters." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

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

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "EXEC [HR].[UpdateLeaveType] @LeaveTypeID, @LeaveTypeName, @Description";
                    command.Parameters.Add(new SqlParameter("@LeaveTypeID", dto.LeaveTypeID));
                    command.Parameters.Add(new SqlParameter("@LeaveTypeName", dto.LeaveTypeName.Trim()));
                    command.Parameters.Add(new SqlParameter("@Description", string.IsNullOrWhiteSpace(dto.Description) ? DBNull.Value : dto.Description));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            bool success = reader.GetBoolean(reader.GetOrdinal("Success"));
                            string message = reader.GetString(reader.GetOrdinal("Message"));

                            if (!success)
                            {
                                return BadRequest(new { success = false, message = message });
                            }

                            return Ok(new { success = true, message = message });
                        }
                        else
                        {
                            return StatusCode(500, new { success = false, message = "No result returned from stored procedure." });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"A database error occurred: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
            finally
            {
                if (_context.Database.GetDbConnection().State == ConnectionState.Open)
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }

        [HttpPut("UpdateLeavePolicy")]
        public async Task<IActionResult> UpdateLeavePolicy([FromBody] UpdateLeavePolicyDto dto)
        {
            if (dto.LeavePolicyID <= 0)
            {
                return BadRequest(new { success = false, message = "LeavePolicy ID must be a positive integer." });
            }
            if (dto.ContractID <= 0)
            {
                return BadRequest(new { success = false, message = "Contract ID must be a positive integer." });
            }
            if (dto.LeaveTypeID <= 0)
            {
                return BadRequest(new { success = false, message = "LeaveType ID must be a positive integer." });
            }
            if (dto.DaysAllocated < 0)
            {
                return BadRequest(new { success = false, message = "Days Allocated cannot be negative." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            try
            {
                var connection = _context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

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

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "EXEC [HR].[UpdateLeavePolicy] @LeavePolicyID, @ContractID, @LeaveTypeID, @DaysAllocated";
                    command.Parameters.Add(new SqlParameter("@LeavePolicyID", dto.LeavePolicyID));
                    command.Parameters.Add(new SqlParameter("@ContractID", dto.ContractID));
                    command.Parameters.Add(new SqlParameter("@LeaveTypeID", dto.LeaveTypeID));
                    command.Parameters.Add(new SqlParameter("@DaysAllocated", dto.DaysAllocated));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            bool success = reader.GetBoolean(reader.GetOrdinal("Success"));
                            string message = reader.GetString(reader.GetOrdinal("Message"));

                            if (!success)
                            {
                                return BadRequest(new { success = false, message = message });
                            }

                            return Ok(new { success = true, message = message });
                        }
                        else
                        {
                            return StatusCode(500, new { success = false, message = "No result returned from stored procedure." });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"A database error occurred: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"An unexpected error occurred: {ex.Message}" });
            }
            finally
            {
                if (_context.Database.GetDbConnection().State == ConnectionState.Open)
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
        }

        [HttpPost("AddLeaveRequest")]
        public async Task<IActionResult> AddLeaveRequest([FromBody] AddLeaveRequestDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");

            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
        new SqlParameter("@EmployeeID", dto.EmployeeID),
        new SqlParameter("@StartDate", dto.StartDate),
        new SqlParameter("@EndDate", dto.EndDate),
        new SqlParameter("@LeaveTypeName", string.IsNullOrWhiteSpace(dto.LeaveTypeName) ? (object)DBNull.Value : dto.LeaveTypeName.Trim()),
        new SqlParameter("@StatusID", dto.StatusID.HasValue ? dto.StatusID : (object)DBNull.Value)
    };

            try
            {
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Attach current user context
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

                // Call the stored procedure
                var result = await _context.Set<AddLeaveRequestResult>()
                    .FromSqlRaw("EXEC [HR].[AddLeaveRequest] @EmployeeID, @StartDate, @EndDate, @LeaveTypeName, @StatusID", parameters)
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
                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });
            }
        }

        [HttpPost("AddLeaveType")]
        public async Task<IActionResult> AddLeaveType([FromBody] AddLeaveTypeDto leaveTypeDto)
        {
            if (leaveTypeDto == null || string.IsNullOrEmpty(leaveTypeDto.LeaveTypeName))
            {
                return BadRequest("LeaveTypeName is required.");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
        new SqlParameter("@LeaveTypeName", leaveTypeDto.LeaveTypeName),
        new SqlParameter("@Description", (object?)leaveTypeDto.Description ?? DBNull.Value)
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
                    "EXEC HR.AddLeaveType @LeaveTypeName, @Description",
                    parameters);

                await connection.CloseAsync();

                return Ok(new { Message = "Leave type added successfully" });
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


        [HttpPost("AddLeaveStatus")]
        public async Task<IActionResult> AddLeaveStatus([FromBody] AddLeaveStatusDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
        new SqlParameter("@StatusName", string.IsNullOrWhiteSpace(dto.StatusName) ? (object)DBNull.Value : dto.StatusName.Trim())
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

                var result = await _context.Set<AddLeaveStatusResult>()
                    .FromSqlRaw("EXEC [HR].[AddLeaveStatus] @StatusName", parameters)
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
                return StatusCode(500, new { error = "Unexpected error: " + ex.Message });
            }
        }

        [HttpPut("UpdateLeaveRequestStatus")]
        public async Task<IActionResult> UpdateLeaveRequestStatus([FromBody] UpdateLeaveRequestStatusDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

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

                var sql = "EXEC [HR].[UpdateLeaveRequestStatus] @LeaveRequestID = {0}, @StatusID = {1}, @RejectionReason = {2}, @ApprovedByID = {3}";
                await _context.Database.ExecuteSqlRawAsync(sql, dto.LeaveRequestID, dto.StatusID, dto.RejectionReason, dto.ApprovedByID);

                await connection.CloseAsync();

                return Ok("Leave request status updated successfully.");
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating leave request: {ex.Message}");
            }
        }


        [HttpPut("UpdateLeaveRequest")]
        public async Task<IActionResult> UpdateLeaveRequest([FromBody] UpdateLeaveRequestDto dto)
        {
            if (dto.LeaveRequestID <= 0)
            {
                return BadRequest(new { success = false, message = "Leave Request ID is required and must be a positive integer." });
            }

            var parameters = new[]
            {
                new SqlParameter("@LeaveRequestID", dto.LeaveRequestID),
                new SqlParameter("@StartDate", dto.StartDate.HasValue ? (object)dto.StartDate.Value : DBNull.Value),
                new SqlParameter("@EndDate", dto.EndDate.HasValue ? (object)dto.EndDate.Value : DBNull.Value),
                new SqlParameter("@LeaveTypeID", dto.LeaveTypeID.HasValue ? (object)dto.LeaveTypeID.Value : DBNull.Value),
                new SqlParameter("@StatusID", dto.StatusID.HasValue ? (object)dto.StatusID.Value : DBNull.Value),
                new SqlParameter("@RejectionReason", string.IsNullOrWhiteSpace(dto.RejectionReason) ? (object)DBNull.Value : dto.RejectionReason.Trim())
            };

            try
            {
                var result = await _context.Set<UpdateLeaveRequestResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateLeaveRequest] @LeaveRequestID, @StartDate, @EndDate, @LeaveTypeID, @StatusID, @RejectionReason", parameters)
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
                    leaveRequestId = spResult.LeaveRequestID,
                    employeeId = spResult.EmployeeID,
                    startDate = spResult.StartDate,
                    endDate = spResult.EndDate,
                    leaveTypeId = spResult.LeaveTypeID,
                    statusId = spResult.StatusID,
                    rejectionReason = spResult.RejectionReason
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

        [HttpGet("AllLeaveTypes")]
        public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetAllLeaveTypes()
        {
            try
            {
                var leaveTypes = await _context.Set<LeaveTypeDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllLeaveTypes]")
                    .ToListAsync();

                return Ok(leaveTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching leave types: {ex.Message}");
            }
        }

        [HttpGet("AllLeaveTypesstatuses")]
        public async Task<ActionResult<IEnumerable<LeaveStatusDto>>> LeaveStatus()
        {
            try
            {
                var leaveTypes = await _context.Set<LeaveStatusDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllLeaveStatuses]")
                    .ToListAsync();

                return Ok(leaveTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching leave types: {ex.Message}");
            }
        }

        [HttpGet("ContractLeavePolicy/{contractType}")]
        public async Task<ActionResult<IEnumerable<ContractLeavePolicyResultDto>>> GetContractLeavePolicy(string contractType)
        {
            if (string.IsNullOrWhiteSpace(contractType))
            {
                return BadRequest(new { success = false, message = "Contract Type is required." });
            }

            var parameters = new[]
            {
                new SqlParameter("@ContractType", contractType.Trim())
            };

            try
            {
                var policies = await _context.Set<ContractLeavePolicyResultDto>()
                    .FromSqlRaw("EXEC [HR].[GetContractLeavePolicy] @ContractType", parameters)
                    .ToListAsync();

                return Ok(policies);
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

        [HttpGet("AllContractLeaveTypes")]
        public async Task<ActionResult<IEnumerable<AllContractLeaveTypeDto>>> GetAllContractLeaveTypes()
        {
            try
            {
                var contractLeaveTypes = await _context.Set<AllContractLeaveTypeDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllContractLeaveTypes]")
                    .ToListAsync();

                return Ok(contractLeaveTypes);
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

        [HttpDelete("DeleteLeaveType/{leaveTypeId}")]
        public async Task<IActionResult> DeleteLeaveType(int leaveTypeId)
        {
            if (leaveTypeId <= 0)
            {
                return BadRequest(new { success = false, message = "LeaveTypeID must be a positive integer." });
            }

            var param = new SqlParameter("@LeaveTypeID", leaveTypeId);

            try
            {
                var result = await _context.Set<DeleteResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteLeaveType] @LeaveTypeID", param)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "No result returned from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                return Ok(new { success = true, message = spResult.Message });
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

        [HttpGet("AllLeaveRequests")]
        public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetAllLeaveRequests()
        {
            try
            {
                var leaveRequests = await _context.Set<LeaveRequestDto>()
                    .FromSqlRaw("EXEC [HR].[GetLeaveRequests]")
                    .ToListAsync();

                return Ok(leaveRequests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching leave requests: {ex.Message}");
            }
        }

        [HttpDelete("DeleteLeavePolicy/{leavePolicyId}")]
        public async Task<IActionResult> DeleteLeavePolicy(int leavePolicyId)
        {
            if (leavePolicyId <= 0)
            {
                return BadRequest(new { success = false, message = "LeavePolicyID must be a positive integer." });
            }

            var param = new SqlParameter("@LeavePolicyID", leavePolicyId);

            try
            {
                var result = await _context.Set<DeleteResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteLeavePolicy] @LeavePolicyID", param)
                    .ToListAsync();

                var spResult = result.FirstOrDefault();

                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "No result returned from stored procedure." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                return Ok(new { success = true, message = spResult.Message });
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


        [HttpGet("GetByEmployee/{employeeId}")]
        public async Task<IActionResult> GetLeaveRequestsByEmployee(int employeeId)
        {
            try
            {
                if (employeeId <= 0)
                {
                    return BadRequest(new { message = "Invalid Employee ID" });
                }

                // SQL Parameter
                var employeeIdParam = new SqlParameter("@EmployeeID", employeeId);

                // Call stored procedure
                var results = await _context.Set<LeaveRequestByEmployeeDto>()
                    .FromSqlRaw("EXEC [HR].[GetLeaveRequestByEmployeeID] @EmployeeID", employeeIdParam)
                    .ToListAsync();

                if (results == null || results.Count == 0)
                {
                    return NotFound(new { message = "No leave requests found for this employee." });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving leave requests.", error = ex.Message });
            }
        }
    }
    public class LeaveRequestByEmployeeDto
    {
        public int ID { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
    }
}

