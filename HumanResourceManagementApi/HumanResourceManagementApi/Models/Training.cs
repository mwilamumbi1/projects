using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("Training", Schema = "HR")]
    public class Training
    {
        [Key]
        public int TrainingID { get; set; }
        public string Title { get; set; }
        public DateTime? TrainingDate { get; set; }
        public string Description { get; set; }

        public ICollection<EmployeeTraining> EmployeeTrainings { get; set; }
    }

}
