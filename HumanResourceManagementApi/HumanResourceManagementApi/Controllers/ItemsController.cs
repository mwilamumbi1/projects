using Core.HumanResourceManagementApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Controllers
{
   
        [Route("api/[controller]")]
        [ApiController]
        public class ItemsController : ControllerBase
        {
            private readonly HRDataContext _context;

            public ItemsController(HRDataContext context)
            {
                _context = context;
            }
 
        [HttpGet("Employees")]
        public async Task<IActionResult> GetEmployees()
        {
            var positions = await _context.Set<GetEmployeesDto>()
                .FromSqlRaw("EXEC HR.GetEmployeeNames")
                .ToListAsync();
            return Ok(positions);
        }
        [HttpGet("Positions")]
            public async Task<IActionResult> GetPositions()
            {
                var positions = await _context.Set<GetPositionsDto>()
                    .FromSqlRaw("EXEC HR.GetPositions")
                    .ToListAsync();
                return Ok(positions);
            }
        [HttpGet("GetUnlinkedUsers")]
        public async Task<IActionResult> GetUnlinkedUsers()
        {
            var positions = await _context.Set<GetUnlinkedUsersDto>()
                .FromSqlRaw("EXEC core.GetUnlinkedUsers")
                .ToListAsync();
            return Ok(positions);
        }
         
        [HttpGet("Roles")]
        public async Task<IActionResult> GetRoles()
        {
            var positions = await _context.Set<GetRolesDto>()
                .FromSqlRaw("EXEC core.GetAllRoles")
                .ToListAsync();
            return Ok(positions);
        }


        [HttpGet("ApplicationStatus")]
        public async Task<IActionResult> ApplicationStatus()
        {
            var positions = await _context.Set<ApplicationStatusDto>()
                .FromSqlRaw("EXEC HR.GetApplicationStatus")
                .ToListAsync();
            return Ok(positions);
        }

        [HttpGet("Managers")]
        public async Task<IActionResult> GetManagers()
        {
            var employees = await _context.Set<GetManagersDto>()
                .FromSqlRaw("EXEC HR.GetManagers")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("MaritalStatuses")]
        public async Task<IActionResult>GetMaritalStatuses ()
        {
            var employees = await _context.Set<GetMaritalStatusesDto> ()
                .FromSqlRaw("EXEC HR.GetMaritalStatuses")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("Genders")]
        public async Task<IActionResult>GetGenders ()
        {
            var employees = await _context.Set<GetGendersDto> ()
                .FromSqlRaw("EXEC HR.GetGenders")
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("Departments")]
        public async Task<IActionResult>GetDepartments ()
        {
            var employees = await _context.Set<GetDepartmentsDto> ()
                .FromSqlRaw("EXEC HR.GetDepartments")
                .ToListAsync();
            return Ok(employees);
        }
        [HttpGet("Contracts")]
        public async Task<IActionResult> GetContracts()
        {
            var employees = await _context.Set<GetContractsDto>()
                .FromSqlRaw("EXEC HR.GetContracts")
                .ToListAsync();
            return Ok(employees);
        }
        [HttpGet("Leaves")]
        public async Task<IActionResult> GetLeaves()
        {
            var employees = await _context.Set<GetLeavesDto>()
                .FromSqlRaw("EXEC HR.GetLeavetypes")
                .ToListAsync();
            return Ok(employees);
        }
        [HttpGet("KPI")]
        public async Task<IActionResult> GetKPI()
        {
            var employees = await _context.Set<GetKPIDto>()
                .FromSqlRaw("EXEC [HR].[GetAllKPI]")
                .ToListAsync();
            return Ok(employees);
        }
        [HttpGet("Applicants")]
        public async Task<IActionResult> GetApplicants()
        {
            var employees = await _context.Set<GetApplicantsDto>()
                .FromSqlRaw("EXEC [HR].[GetApplicants]")
                .ToListAsync();
            return Ok(employees);
        }
 
        
    }
}



[Keyless]
public class DeleteLeavePolicyResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}


[Keyless]
public class DeleteLeaveTypeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

// NEW DTO for deleting assigned leave to contract
[Keyless]
public class DeleteAssignedLeaveToContractResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
public class GetPositionsDto
{
    public int PositionID { get; set; }
    public string Title { get; set; }
}

public class GetUnlinkedUsersDto
{
    public int UserID { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}
public class GetManagersDto
{
    public int EmployeeID { get; set; }
    public string FullName { get; set; }
}
public class GetMaritalStatusesDto
{
    public string MaritalStatus { get; set; }
}
public class GetGendersDto
{
    public string Gender { get; set; }
}
public class GetDepartmentsDto
{
    public int  DepartmentId { get; set; }
    public string  Name { get; set; }

}

public class GetRolesDto
{
    public int RoleID { get; set; }
    public string RoleName { get; set; }

}
public class GetContractsDto
{
    public int  ContractID{ get; set; }
    public string ContractType { get; set; }
}
public class GetApplicantsDto
{
    public int ApplicationID { get; set; }
    public string ApplicantName { get; set; }
}

 public class ApplicationStatusDto
{
    public int StatusID { get; set; }
    public string StatusName { get; set; }
}
public class GetKPIDto
{
    public int KPI_ID { get; set; }
    public string KPI_Name { get; set; }
}
public class GetEmployeesDto
{
    public int EmployeeID { get; set; }
    public string fullname { get; set; }
}
public class GetLeavesDto
{
    public int LeaveTypeID { get; set; }
    public string LeaveTypeName { get; set; }
}

[Keyless]
public class UpdateLeavePolicyResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? LeavePolicyID { get; set; }
}


[Keyless]
public class AddLeavePolicyResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}


// NEW DTOs for Leave Type CRUD (Update and Delete)
public class UpdateLeaveTypeDto
{
    [Required]
    public int LeaveTypeID { get; set; }
    [Required]
    [StringLength(50)]
    public string LeaveTypeName { get; set; } = string.Empty;
    [StringLength(500)]
    public string? Description { get; set; }
}

[Keyless]
public class UpdateLeaveTypeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? LeaveTypeID { get; set; }
}
