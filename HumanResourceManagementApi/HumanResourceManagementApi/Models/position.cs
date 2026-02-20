using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("position", Schema = "HR")]
    public class Position
    {
        [Key]
        public int PositionID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }


}
