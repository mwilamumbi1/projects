
using System.ComponentModel.DataAnnotations;

namespace Core.HumanResourceManagementApi.Models
{
    public class ContractLeaveType
    {
        [Key]
        public int ContractID { get; set; }

        [Key]
        public int LeaveTypeID { get; set; }

        public int DaysAllocated { get; set; }
    }

}
