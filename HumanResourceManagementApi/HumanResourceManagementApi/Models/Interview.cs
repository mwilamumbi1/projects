using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("Interview", Schema = "HR")]
    public class Interview
    {
        [Key]
        public int InterviewID { get; set; }

        public int? ApplicationID { get; set; }
        public Application Application { get; set; }

        public DateTime? InterviewDate { get; set; }

       
        public string Interviewers { get; set; }

        public string Feedback { get; set; }
    }

}