using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.HumanResourceManagementApi.DTOs;
using Core.HumanResourceManagementApi.Models;

namespace Core.HumanResourceManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : ControllerBase
    {
        private readonly HRDataContext _context;

        public PayrollController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetPayrollHistory/{employeeId}")]
        public async Task<IActionResult> GetPayrollHistory(int employeeId)
        {
            var result = await _context.Set<EmployeePayrollHistoryDto>()
                .FromSqlRaw("EXEC HR.GetEmployeePayrollHistory {0}", employeeId)
                .ToListAsync();

            if (result == null || result.Count == 0)
            {
                return NotFound(new { success = false, message = "Payroll history not found for this employee." });
            }

            return Ok(result);
        }

        [HttpGet("BankDetails")]
        public async Task<IActionResult> GetPayrollBankDetails([FromQuery] int month, [FromQuery] int year)
        {
            var result = await _context.Set<PayrollBankDetailsDto>()
                .FromSqlRaw("EXEC HR.GetPayrollBankDetails @Month = {0}, @Year = {1}", month, year)
                .ToListAsync();

            if (result == null || result.Count == 0)
            {
                return NotFound(new { success = false, message = "No payroll data found for specified month and year." });
            }

            return Ok(result);
        }

    }
}
public class PayrollBankDetailsDto
{
    public int EmployeeID { get; set; }
    public string EmployeeName { get; set; }
    public string BankName { get; set; }
    public string BankAccountNumber { get; set; }
    public decimal NetSalary { get; set; }
}

public class EmployeePayrollHistoryDto
{
    public int PayrollID { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Deductions { get; set; }
    public decimal Allowances { get; set; }
    public decimal NetSalary { get; set; }
}
