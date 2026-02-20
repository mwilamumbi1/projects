using System;
using System.ComponentModel.DataAnnotations; 
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Make sure this is present for SqlConnection
using System.Data; // Make sure this is present for CommandType
using Microsoft.Extensions.Configuration; // Add this using directive


namespace Core.HumanResourceManagementApi.DTOs // <--- NEW DTO NAMESPACE
{
    [Keyless] 
    public class ApproveLeaveRequestDto
    {
        [Required]
        public int LeaveRequestID { get; set; }
    }

    public class AddLeaveTypeDto
    {
        [Required]
        [StringLength(50)]
        public string LeaveTypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AddLeaveRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
    public class AddLeaveRequestDto
    {
        public int EmployeeID { get; set; }   // Use ID instead of FirstName/LastName
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public int? StatusID { get; set; }
    }


    public class AssignLeaveToContractDto
    {
        [Required]
        public int ContractID { get; set; }

        [Required]
        public int LeaveTypeID { get; set; }

        [Required]
        public int DaysAllocated { get; set; }
    }

    // This DTO is for the result of the AssignLeaveToContract stored procedure
    public class AssignLeaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AddLeaveStatusDto
    {
        public string StatusName { get; set; } = string.Empty;
    }

    public class AddLeaveStatusResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ApproveLeaveRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteLeaveStatusDto
    {
        public int StatusID { get; set; }
    }

    public class AllocateAnnualLeaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AllocateAnnualLeaveDto
    {
        public int? AllocationYear { get; set; }
    }

    public class UpdateLeaveRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? LeaveRequestID { get; set; }
        public int? EmployeeID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? LeaveTypeID { get; set; }
        public int? StatusID { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class UpdateLeavePolicyDto
    {
        public int LeavePolicyID { get; set; }
        public int ContractID { get; set; }
        public int LeaveTypeID { get; set; }
        public int DaysAllocated { get; set; }
    }
    public class UpdateLeavePolicyResult
    {
         
        public bool Success { get; set; }
        public string Message { get; set; }

    }
        public class UpdateLeaveRequestDto
    {
        public int LeaveRequestID { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? LeaveTypeID { get; set; }
        public int? StatusID { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class UpdateLeaveStatusDto
    {
        public int StatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    public class UpdateLeaveStatusResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? StatusID { get; set; }
        public string? StatusName { get; set; }
    }

    [Keyless] // Assuming this is also a keyless entity for EF Core
    public class LeaveTypeDto
    {
        public int LeaveTypeID { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string? Description { get; set; } = string.Empty;
    }

    [Keyless] // Assuming this is also a keyless entity for EF Core
    public class LeavePolicyResultDto
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? description { get; set; }
        public string? applicableTo { get; set; }
        public int maxDays { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [Keyless] // Assuming this is also a keyless entity for EF Core
    public class ContractLeavePolicyResultDto
    {
        public string ContractType { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;
        public int DaysAllocated { get; set; }
    }

    [Keyless] // Assuming this is also a keyless entity for EF Core
    public class AllContractLeaveTypeDto
    {
        public int ID { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public string LeaveTypeName { get; set; } = string.Empty;
        public int DaysAllocated { get; set; }
    }

   

    public class DeleteLeaveTypeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteLeavePolicyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [Keyless]
    public class LeaveStatusDto
    {
        public int StatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    [Keyless]
    public class LeaveRequestDto
    {
        public int LeaveRequestID { get; set; }
        public int EmployeeID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int LeaveTypeID { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int StatusID { get; set; }
        public string StatusName { get; set; } = "Pending";
        public string? RejectionReason { get; set; }
    }

    public class UpdateLeaveRequestStatusDto
    {
        public int LeaveRequestID { get; set; }
        public int StatusID { get; set; }
        public string? RejectionReason { get; set; }
        public int ApprovedByID { get; set; }
    }

    public class UpdateLeaveTypeDto
    {
        public int LeaveTypeID { get; set; }
        public string LeaveTypeName { get; set; }
        public string? Description { get; set; }
    }

    [Keyless]
    public class UpdateLeaveTypeResult
    {
        public bool Success { get; set; }  // ✅ This matches the SP output
        public string Message { get; set; } = string.Empty;
    }
}