using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Models
{
    public class LeavePolicy
    {
        [Key]
        public int LeavePolicyID { get; set; }

        public int ContractID { get; set; }

        [Required]
        public string LeaveType { get; set; } = null!;

        public int DaysAllocated { get; set; }
    }

}
