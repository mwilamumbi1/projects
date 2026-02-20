using Microsoft.AspNetCore.Mvc;
using Core.HumanResourceManagementApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Core.HumanResourceManagementApi.Models;

namespace Core.HumanResourceManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly HRDataContext _context;

        public AttendanceController(HRDataContext context)
        {
            _context = context;
        }

        [HttpGet("GetAttendanceWithNames")]
        public async Task<IActionResult> GetAttendanceWithEmployeeNames()
        {
            var result = await _context.Set<AttendanceRecord>()
                .FromSqlRaw("EXEC HR.GetAllMainAttendance")
                .ToListAsync();

            return Ok(result);
        }
    }
}
// Models/AttendanceRecord.cs
public class AttendanceRecord
{
    public int AttendanceID { get; set; }
    public int ID { get; set; } // EmployeeID or similar
    public TimeSpan InTime { get; set; }
    public TimeSpan? OutTime { get; set; }
    public string PersonName { get; set; }
    public string CardNo { get; set; }
    public DateTime CreatedDate { get; set; }
}