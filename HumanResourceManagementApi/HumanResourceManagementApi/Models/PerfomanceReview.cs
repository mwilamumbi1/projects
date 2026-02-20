using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("PerformanceReview", Schema = "HR")]
    public class PerformanceReview
    {
        [Key]
        public int ReviewID { get; set; }

        public int? EmployeeID { get; set; }
        public Employee Employee { get; set; }

        public DateTime? ReviewDate { get; set; }

        public int? ReviewerID { get; set; }
        public Employee Reviewer { get; set; }

        public int? Rating { get; set; }
        public string Comments { get; set; }
    }

}
