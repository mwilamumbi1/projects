using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("Attendance", Schema = "HR")]
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        public int? EmployeeID { get; set; }
        public Employee Employee { get; set; }

        public DateTime? Date { get; set; }
        public TimeSpan? TimeIn { get; set; }
        public TimeSpan? TimeOut { get; set; }
    }


}
