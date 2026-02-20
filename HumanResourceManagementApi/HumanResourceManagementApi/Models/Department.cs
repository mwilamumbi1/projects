using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.HumanResourceManagementApi.Models
{
    [Table("Department", Schema = "HR")]
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }
        public string Name { get; set; }
        public int? ManagerID { get; set; }
        public Employee Manager { get; set; }
        public ICollection<Employee> Employees { get; set; }
        public ICollection<JobOpening> JobOpenings { get; set; }
    }

}
