using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("JobOpening", Schema = "HR")]
    public class JobOpening
    {
        [Key]
        public int JobOpeningID { get; set; }
        public string Title { get; set; }

        public int? DepartmentID { get; set; }
        public Department Department { get; set; }

        public string Location { get; set; }
        public string Status { get; set; }
        public DateTime? PostedDate { get; set; }
        public DateTime? ClosingDate { get; set; }

        public ICollection<Application> Applications { get; set; }
    }

}
