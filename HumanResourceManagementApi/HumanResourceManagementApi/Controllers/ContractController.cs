using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Core.HumanResourceManagementApi.Models
{
   
    public class GetContractLeavePolicyDto
    {
        public string ContractType { get; set; }
        public string LeaveTypeName { get; set; }
        public int DaysAllocated { get; set; }
    }
}

namespace Core.HumanResourceManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContractController : ControllerBase
    {
        private readonly HRDataContext _context;

        public ContractController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetContractLeavePolicy{contractId}")]
        public async Task<IActionResult> GetContractLeavePolicy(int contractId)
        {
            if (contractId <= 0)
            {
                return BadRequest("ContractID must be a positive integer.");
            }

            var parameters = new[]
            {
                new SqlParameter("@ContractID", contractId)
            };

            var leavePolicies = await _context.Set<GetContractLeavePolicyDto>()
                .FromSqlRaw("EXEC HR.GetContractLeavePolicy @ContractID", parameters)
                .ToListAsync();

            return Ok(leavePolicies);
        }

        [HttpGet("GetAllContracts")]
        public async Task<IActionResult> GetAllContracts()
        {
            try
            {
                var contracts = await _context.Set<ContractDto>()
                    .FromSqlRaw("EXEC [HR].[GetAllContracts]")
                    .AsNoTracking()
                    .ToListAsync();

                return Ok(contracts);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { error = $"SQL Error: {ex.Message}" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = $"Unexpected error: {ex.Message}" });
            }

        }
        [HttpPost("AddContract")]
        public async Task<IActionResult> AddContract([FromBody] AddContractDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim missing");
            int currentUserId = int.Parse(userIdClaim);

            var parameters = new[]
            {
        new SqlParameter("@ContractType", string.IsNullOrWhiteSpace(dto.ContractType) ? (object)DBNull.Value : dto.ContractType.Trim()),
        new SqlParameter("@Description", string.IsNullOrWhiteSpace(dto.Description) ? (object)DBNull.Value : dto.Description.Trim())
    };

            try
            {
                // Open connection manually
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Set SESSION_CONTEXT UserID
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

                // Run your stored procedure
                var result = await _context.Set<AddContractResult>()
                    .FromSqlRaw("EXEC [HR].[AddContract] @ContractType, @Description", parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null)
                    return StatusCode(500, new { error = "Unexpected error: No result from stored procedure." });

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
                return StatusCode(500, new { error = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpPut("UpdateContractById")]
        public async Task<IActionResult> UpdateContractById([FromBody] UpdateContractDto dto)
        {
            if (dto.ContractID <= 0)
                return BadRequest(new { success = false, message = "Valid Contract ID is required to identify the contract for update." });

            if (dto.NewContractType != null && string.IsNullOrWhiteSpace(dto.NewContractType))
                return BadRequest(new { success = false, message = "New Contract Type cannot be empty if provided." });

            if (dto.NewContractType != null && dto.NewContractType.Length > 100)
                return BadRequest(new { success = false, message = "New Contract Type exceeds maximum length (100 characters)." });

            if (dto.Description != null && dto.Description.Length > 5000)
                return BadRequest(new { success = false, message = "Description exceeds maximum allowed length (5000 characters)." });

            var parameters = new[]
            {
        new SqlParameter("@ContractID", dto.ContractID),
        new SqlParameter("@NewContractType", string.IsNullOrWhiteSpace(dto.NewContractType) ? (object)DBNull.Value : dto.NewContractType.Trim()),
        new SqlParameter("@Description", string.IsNullOrWhiteSpace(dto.Description) ? (object)DBNull.Value : dto.Description.Trim())
    };

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

                var result = await _context.Set<UpdateContractResult>()
                    .FromSqlRaw("EXEC [HR].[UpdateContract] @ContractID, @NewContractType, @Description", parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "No result from stored procedure. Ensure it always returns a result set." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                return Ok(new
                {
                    success = true,
                    message = spResult.Message,
                    contractId = spResult.ContractID,
                    contractType = spResult.ContractType,
                    description = spResult.Description
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Unexpected error: " + ex.Message });
            }
        }

        [HttpDelete("DeleteContract/{contractId:int}")]
        public async Task<IActionResult> DeleteContractById([FromRoute(Name = "contractId")] int ContractID)
        {
            if (ContractID <= 0)
                return BadRequest(new { success = false, message = "A valid Contract ID is required." });

            var parameters = new[]
            {
        new SqlParameter("@ContractID", ContractID)
    };

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

                var result = await _context.Set<DeleteContractResult>()
                    .FromSqlRaw("EXEC [HR].[DeleteContract] @ContractID", parameters)
                    .ToListAsync();

                await connection.CloseAsync();

                var spResult = result.FirstOrDefault();
                if (spResult == null)
                    return StatusCode(500, new { success = false, message = "Stored procedure did not return a result. Ensure it always returns a result set." });

                if (!spResult.Success)
                    return BadRequest(new { success = false, message = spResult.Message });

                return Ok(new { success = true, message = spResult.Message, contractId = spResult.ContractID });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new { success = false, message = $"Database error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Unexpected error: {ex.Message}" });
            }
        }


    }
}
    public class ContractDto
    {
        public int ContractID { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    // Data Transfer Object
    public class AddContractDto
    {
        public string ContractType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Result Model
    public class AddContractResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;


    }

    // DTOs/UpdateContractDto.cs
    public class UpdateContractDto
    {
 
        public int ContractID { get; set; } 

 
        public string? NewContractType { get; set; }

       
        public string? Description { get; set; }
    }

public class DeleteContractDto
{

    public int ContractID { get; set; }
}

    public class UpdateContractResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        // The SP returns these details on success or error.
        public int? ContractID { get; set; }
        public string? ContractType { get; set; }
        public string? Description { get; set; }
    }

    public class DeleteContractResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
 
        public int? ContractID { get; set; }
    }

