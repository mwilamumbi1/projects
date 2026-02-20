using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("Payroll", Schema = "HR")]
    public class Payroll
    {
        [Key]
        public int PayrollID { get; set; }

        public int? EmployeeID { get; set; }
        public Employee Employee { get; set; }

        public int? Month { get; set; }
        public int? Year { get; set; }

        public decimal? BasicSalary { get; set; }
        public decimal? Deductions { get; set; }
        public decimal? Allowances { get; set; }

        [NotMapped]
        public decimal? NetSalary => (BasicSalary + Allowances) - Deductions;
    }

}
