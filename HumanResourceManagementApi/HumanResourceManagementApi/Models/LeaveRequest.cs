using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("LeaveRequest", Schema = "HR")]
    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestID { get; set; }

        public int? EmployeeID { get; set; }
        public Employee Employee { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string LeaveType { get; set; }
        public string Status { get; set; }
    }

}
