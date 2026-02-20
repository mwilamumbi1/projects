using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Models
{
    public class LeaveBalance
    {
        [Key]
        public int EmployeeID { get; set; }

        [Key]
        public int LeaveTypeID { get; set; }

        [Key]
        public int Year { get; set; }

        public int DaysAllocated { get; set; }
        public int DaysUsed { get; set; }
    }
}
