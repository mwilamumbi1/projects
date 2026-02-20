namespace Core.HumanResourceManagementApi.Models
{
    public class Employee
    {
        public int EmployeeID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public DateTime HireDate { get; set; }
        public int DepartmentID { get; set; }
        public int PositionID { get; set; }
        public int? ManagerID { get; set; }
        public decimal Salary { get; set; }
        public int? StatusID { get; set; }
        public string Email { get; set; }
        public string NRC { get; set; }
        public string TPIN { get; set; }
        public string MaritalStatus { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public string PhoneNumber { get; set; }
        public string EmergencyContactName { get; set; }
        public string EmergencyContactNumber { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public int? ContractID { get; set; }
        public String? NapsaRegNumber { get; set; }
        
        public Department Department { get; set; }
        public Position Position { get; set; }
        public Employee Manager { get; set; }
       
        public ICollection<Attendance> Attendances { get; set; }
       public ICollection<EmployeeTraining> EmployeeTrainings { get; set; }
      
        public ICollection<JobOpening> ManagedJobOpenings { get; set; }
        public ICollection<LeaveRequest> LeaveRequests { get; set; }
        public ICollection<Payroll> Payrolls { get; set; }
        public ICollection<PerformanceReview> PerformanceReviews { get; set; }
        public ICollection<PerformanceReview> ReviewsGiven { get; set; }
        public ICollection<Department> ManagedDepartments { get; set; }
    }
}