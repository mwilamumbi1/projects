using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Models
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeID { get; set; }

        [Required]
        public string LeaveTypeName { get; set; } = null!;
    }
}
