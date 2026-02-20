using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("EmployeeTraining", Schema = "HR")]
    public class EmployeeTraining
    {
        [Key]
        public int EmployeeID { get; set; }
        public Employee Employee { get; set; }

        public int TrainingID { get; set; }
        public Training Training { get; set; }

        //public string Name { get; set; }
        public int? ManagerID { get; set; }
        public Employee Manager { get; set; }
    }

}
