using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("Application", Schema = "HR")]
    public class Application
    {
        [Key]
        public int ApplicationID { get; set; }

        public int? JobOpeningID { get; set; }
        public JobOpening JobOpening { get; set; }

        public string ApplicantName { get; set; }
        public string Resume { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public int Status { get; set; }
        public string? Email { get; set; }
        public ICollection<Interview> Interviews { get; set; }
    }

}
