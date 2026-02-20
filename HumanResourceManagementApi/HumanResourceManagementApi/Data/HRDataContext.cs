using Core.HumanResourceManagementApi.Controllers;
using Core.HumanResourceManagementApi.Controllers.ERP.DTOs.HR;
using Core.HumanResourceManagementApi.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static Core.HumanResourceManagementApi.Controllers.EmployeesController;
using static Core.HumanResourceManagementApi.Controllers.LeaveController;

namespace Core.HumanResourceManagementApi.Models
{
    public class HRDataContext : DbContext
    {
        public HRDataContext(DbContextOptions<HRDataContext> options) : base(options) { }
        public DbSet<PositionWithEmployeesDto> PositionsWithEmployees { get; set; }
        public DbSet<UpdatePositionResult> UpdatePositionResults { get; set; }
        public DbSet<GetAllKpiResult> GetAllKpiResult { get; set; }
        public DbSet<LeaveRequestByEmployeeDto> LeaveRequestByEmployee { get; set; }

        public DbSet<SpResult> SpResults { get; set; }
        public DbSet<Application> Applications { get; set; }  
        public DbSet<UpdateLeavePolicyResult> UpdateLeavePolicyResults { get; set; }
        public DbSet<EmployeeProfilePicture> EmployeeProfilePictures { get; set; }

        public DbSet<AssignLeaveResult> AssignLeaveResults { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeTraining> EmployeeTrainings { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<JobOpening> JobOpenings { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Training> Trainings { get; set; }
        public DbSet<EmployeeStatusDto> EmployeeStatusDtos { get; set; }
        public DbSet<DeleteLeaveTypeResult> DeleteLeaveTypeResults { get; set; }
        public DbSet<ApproveLeaveRequestDto> ApproveLeaveRequestDtos { get; set; } // If used for queries
        public DbSet<AddLeaveRequestResult> AddLeaveRequestResults { get; set; }      
        public DbSet<AddLeaveStatusResult> AddLeaveStatusResults { get; set; }
        public DbSet<ApproveLeaveRequestResult> ApproveLeaveRequestResults { get; set; }
        public DbSet<DeleteResult> DeleteResults { get; set; }
        public DbSet<NotificationDto> Notifications { get; set; }

        public DbSet<AllocateAnnualLeaveResult> AllocateAnnualLeaveResults { get; set; }
        public DbSet<UpdateLeaveRequestResult> UpdateLeaveRequestResults { get; set; }
        public DbSet<UpdateLeaveStatusResult> UpdateLeaveStatusResults { get; set; }
        public DbSet<LeaveTypeDto> LeaveTypeDtos { get; set; }
        public DbSet<LeaveStatusDto> LeaveStatusDtos { get; set; }
        public DbSet<LeavePolicyResultDto> LeavePolicyResultDtos { get; set; }
        public DbSet<ContractLeavePolicyResultDto> ContractLeavePolicyResultDtos { get; set; }
        public DbSet<AllContractLeaveTypeDto> AllContractLeaveTypeDtos { get; set; }
        public DbSet<DeleteAssignedLeaveToContractResult> DeleteAssignedLeaveToContractResults { get; set; }  
        public DbSet<DeleteLeavePolicyResult> DeleteLeavePolicyResults { get; set; }
        public DbSet<LeaveRequestDto> LeaveRequestDtos { get; set; }
        public DbSet<InterviewWithNamesDto> InterviewWithNamesDto { get; set; }
        public DbSet<AddKPIDefinitionResult> AddKPIDefinitionResult { get; set; }
        public DbSet<GetKpiDefinitionByIdResult> GetKpiDefinitionByIdResult { get; set; }
        public DbSet<UpdateKPIDefinitionResult> UpdateKPIDefinitionResult { get; set; }
        public DbSet<ToggleKPIStatusResult> ToggleKPIStatusResult { get; set; }
        public DbSet<DeleteKPIDefinitionResult> DeleteKPIDefinitionResult { get; set; }
        public DbSet<PermissionDto> Permissions { get; set; }
        public DbSet<getPermissionDto> Permit { get; set; }
        public DbSet<InterviewReportModel> InterviewReports { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<getPermissionDto>().HasNoKey();
            modelBuilder.Entity<PermissionDto>().HasNoKey();
            modelBuilder.Entity<SpResult>().HasNoKey();
            modelBuilder.Entity<EmployeePayrollHistoryDto>().HasNoKey();
            modelBuilder.Entity<PositionWithEmployeesDto>().HasNoKey();
            modelBuilder.Entity<AttendanceRecord>().HasNoKey();
            modelBuilder.Entity<DepartmentPerformanceSummaryDto>().HasNoKey();
            modelBuilder.Entity<DepartmentManagerDto>().HasNoKey();
            modelBuilder.Entity<PayrollBankDetailsDto>().HasNoKey();
            modelBuilder.Entity<InsertEmployeeDto>().HasNoKey();
            modelBuilder.Entity<InsertJobOpeningDto>().HasNoKey();
            modelBuilder.Entity<AddContractDto>().HasNoKey();
            modelBuilder.Entity<AddContractResult>().HasNoKey();
            modelBuilder.Entity<AddLeaveTypeDto>().HasNoKey();
            modelBuilder.Entity<EmployeePerformanceDto>().HasNoKey();
            modelBuilder.Entity<AssignLeaveToContractDto>().HasNoKey();
            modelBuilder.Entity<GetContractLeavePolicyDto>().HasNoKey();
            modelBuilder.Entity<ApproveLeaveRequestDto>().HasNoKey();
            modelBuilder.Entity<UpdateReviewObjectiveDto>().HasNoKey();
            modelBuilder.Entity<GetPositionByNameDto>().HasNoKey();
            modelBuilder.Entity<GetEmployeeEmergencyInfoDto>().HasNoKey();
            modelBuilder.Entity<GetEmployeePersonalInfoDto>().HasNoKey();
            modelBuilder.Entity<GetEmployeeJobInfoDto>().HasNoKey();
            modelBuilder.Entity<GetEmployeeBankInfoDto>().HasNoKey();
            modelBuilder.Entity<GetAllEmployeesDto>().HasNoKey();
            modelBuilder.Entity<GetPositionByIdDto>().HasNoKey();
            modelBuilder.Entity<GetRolesDto>().HasNoKey();
            modelBuilder.Entity<AddEmployeeResult>().HasNoKey();
            modelBuilder.Entity<AddApplicationStatusResult>().HasNoKey();
            modelBuilder.Entity<AddDepartmentDto>().HasNoKey();
            modelBuilder.Entity<AddDepartmentResult>().HasNoKey();
            modelBuilder.Entity<AddEmployeePerformanceDto>().HasNoKey();
            modelBuilder.Entity<AddEmployeePerformanceResult>().HasNoKey();
            modelBuilder.Entity<AddEmployeeTrainingResult>().HasNoKey();
            modelBuilder.Entity<AddEmployeeTrainingDto>().HasNoKey();
            modelBuilder.Entity<LeaveRequestByEmployeeDto>().HasNoKey();
            modelBuilder.Entity<AddInterviewDto>().HasNoKey();
            modelBuilder.Entity<InterviewReportModel>().HasNoKey();
            modelBuilder.Entity<AddInterviewResult>().HasNoKey();
            modelBuilder.Entity<DepartmentLookupResult>().HasNoKey();
            modelBuilder.Entity<GetDepartmentByNameDto>().HasNoKey();
            modelBuilder.Entity<DeleteEmployeeTrainingDto>().HasNoKey();
            modelBuilder.Entity<DeleteEmployeeTrainingResult>().HasNoKey();
            modelBuilder.Entity<AllocateAnnualLeaveResult>().HasNoKey();
            modelBuilder.Entity<AllocateAnnualLeaveDto>().HasNoKey();
            modelBuilder.Entity<AddPositionResult>().HasNoKey();
            modelBuilder.Entity<AddLeaveStatusDto>().HasNoKey();
            modelBuilder.Entity<DeleteTrainingResult>().HasNoKey();
            modelBuilder.Entity<DeleteTrainingDto>().HasNoKey();
            modelBuilder.Entity<DeletePositionResult>().HasNoKey();
            modelBuilder.Entity<DeletePositionDto>().HasNoKey();
            modelBuilder.Entity<DeletePerfomanceResult>().HasNoKey();
            modelBuilder.Entity<DeletePerformanceReviewDto>().HasNoKey();
            modelBuilder.Entity<DeleteResult>().HasNoKey();
            modelBuilder.Entity<DeleteLeaveStatusDto>().HasNoKey();
            modelBuilder.Entity<DeleteInterviewDto>().HasNoKey();
            modelBuilder.Entity<DeleteInterviewResult>().HasNoKey();
            modelBuilder.Entity<AddKPIDefinitionResult>().HasNoKey();
            modelBuilder.Entity<AddKPIDefinitionDto>().HasNoKey();
            modelBuilder.Entity<GetPositionByIdResult>().HasNoKey();
            modelBuilder.Entity<GetTrainingByIdResult>().HasNoKey();
            modelBuilder.Entity<GetJobOpeningByIdDto>().HasNoKey();
            modelBuilder.Entity<GetJobOpeningByIdResult>().HasNoKey();
            modelBuilder.Entity<GetKpiDefinitionByIdResult>().HasNoKey();
            modelBuilder.Entity<GetKpiDefinitionByIdDto>().HasNoKey();
            modelBuilder.Entity<AddLeaveRequestResult>().HasNoKey();
            modelBuilder.Entity<AddLeaveRequestDto>().HasNoKey();
            modelBuilder.Entity<ContractDto>().HasNoKey();
            modelBuilder.Entity<UpdateDepartmentDto>().HasNoKey();
            modelBuilder.Entity<UpdateDepartmentResult>().HasNoKey();
            modelBuilder.Entity<UpdateApplicationStatusDto>().HasNoKey();
            modelBuilder.Entity<UpdateApplicationStatusResult>().HasNoKey();
            modelBuilder.Entity<UpdateApplicationDto>().HasNoKey();
            modelBuilder.Entity<UpdateApplicationResult>().HasNoKey();           
            modelBuilder.Entity<UpdateEmployeeTrainingResult>().HasNoKey();
            modelBuilder.Entity<UpdateEmployeeTrainingDto>().HasNoKey();
            modelBuilder.Entity<UpdateInterviewDto>().HasNoKey();
            modelBuilder.Entity<UpdateInterviewResult>().HasNoKey();
            modelBuilder.Entity<UpdateLeaveRequestDto>().HasNoKey();
            modelBuilder.Entity<UpdateLeaveRequestResult>().HasNoKey();
            modelBuilder.Entity<UpdateContractResult>().HasNoKey();
            modelBuilder.Entity<DeleteContractResult>().HasNoKey();
            modelBuilder.Entity<NotificationDto>().HasNoKey();
            modelBuilder.Entity<UpdateEmployeePerformanceResult>().HasNoKey();
            modelBuilder.Entity<UpdateReviewIndicatorResult>().HasNoKey();
            modelBuilder.Entity<UpdateReviewIndicatorDto>().HasNoKey();
            modelBuilder.Entity<UpdateTrainingResult>().HasNoKey();
            modelBuilder.Entity<UpdateTrainingDto>().HasNoKey();
            modelBuilder.Entity<UpdateEmployeeResult>().HasNoKey();
            modelBuilder.Entity<TrainingDto>().HasNoKey();
            modelBuilder.Entity<GetAllApplicantsDto>().HasNoKey();
            modelBuilder.Entity<AddApplicationResult>().HasNoKey();
            modelBuilder.Entity<GetAllJobOpeningsResult>().HasNoKey();
            modelBuilder.Entity<InsertJobOpeningResultDto>().HasNoKey();
            modelBuilder.Entity<TotalJobsCountDto>().HasNoKey();
            modelBuilder.Entity<TotalEmployeeCountDto>().HasNoKey();
            modelBuilder.Entity<GetTotalEmployeeCountResult>().HasNoKey();
            modelBuilder.Entity<TotalPerfomanceCountDto>().HasNoKey();
            modelBuilder.Entity<TotalTrainingCountDto>().HasNoKey();
            modelBuilder.Entity<TotalDepartmenntCountDto>().HasNoKey();
            modelBuilder.Entity<DeleteDepartmentResult>().HasNoKey();
            modelBuilder.Entity<GetPositionsDto>().HasNoKey();
            modelBuilder.Entity<EmployeeProfilePicture>().HasNoKey();
            modelBuilder.Entity<GetManagersDto>().HasNoKey();
            modelBuilder.Entity<GetMaritalStatusesDto>().HasNoKey();
            modelBuilder.Entity<GetGendersDto>().HasNoKey();
            modelBuilder.Entity<GetDepartmentsDto>().HasNoKey();
            modelBuilder.Entity<GetContractsDto>().HasNoKey();
            modelBuilder.Entity<LoginDto>().HasNoKey();
            modelBuilder.Entity<UserResponseDto>().HasNoKey();
            modelBuilder.Entity<GetAuditLogDto>().HasNoKey();
            modelBuilder.Entity<AddTrainingResult>().HasNoKey();
            modelBuilder.Entity<GetEmployeesDto>().HasNoKey();
            modelBuilder.Entity<LeavePolicyResultDto>().HasNoKey();
            modelBuilder.Entity<LeaveStatusDto>().HasNoKey();
            modelBuilder.Entity<ContractLeavePolicyResultDto>().HasNoKey();
            modelBuilder.Entity<AllContractLeaveTypeDto>().HasNoKey();
            modelBuilder.Entity<LeaveTypeDto>().HasNoKey();
            modelBuilder.Entity<GetLeavesDto>().HasNoKey();
            modelBuilder.Entity<AddLeavePolicyResult>().HasNoKey();
            modelBuilder.Entity<UpdateLeavePolicyResult>().HasNoKey();
            modelBuilder.Entity<DeleteLeavePolicyResult>().HasNoKey();
            modelBuilder.Entity<UpdateLeaveTypeResult>().HasNoKey();
            modelBuilder.Entity<DeleteLeaveTypeResult>().HasNoKey();
            modelBuilder.Entity<DeleteAssignedLeaveToContractResult>().HasNoKey();
            modelBuilder.Entity<UpdateLeaveStatusResult>().HasNoKey();
            modelBuilder.Entity<AddLeaveStatusResult>().HasNoKey();
            modelBuilder.Entity<LeaveRequestDto>().HasNoKey();
            modelBuilder.Entity<UpdateLeaveRequestStatusDto>().HasNoKey();
            modelBuilder.Entity<AssignLeaveResult>().HasNoKey();
            modelBuilder.Entity<UpdatePositionResult>().HasNoKey();
            modelBuilder.Entity<GetKPIDto>().HasNoKey();
            modelBuilder.Entity<DeleteeEmployeePerformanceReviewDto>().HasNoKey();
            modelBuilder.Entity<EmployeePerfomanceDeleteResult>().HasNoKey();
            modelBuilder.Entity<UserDetailsDto>().HasNoKey();
            modelBuilder.Entity<EmployeeStatusDto>().HasNoKey();
            modelBuilder.Entity<ToggleEmployeeStatusDto>().HasNoKey();
            modelBuilder.Entity<EmployeePerformanceSummaryDto>().HasNoKey();
            modelBuilder.Entity<EmployeeTrainingResultDto>().HasNoKey();
            modelBuilder.Entity<ApproveLeaveRequestResult>().HasNoKey();
             modelBuilder.Entity<UpdateLeavePolicyDto>().HasNoKey();
            modelBuilder.Entity<UpdateLeaveTypeDto>().HasNoKey();
            modelBuilder.Entity<UpdateLeavePolicyResult>().HasNoKey();
            modelBuilder.Entity<InterviewWithNamesDto>().HasNoKey();
            modelBuilder.Entity<GetApplicantsDto>().HasNoKey();
            modelBuilder.Entity<GetAllEmployeeTrainingResult>().HasNoKey();
            modelBuilder.Entity<ApplicationStatusDto>().HasNoKey();      
            modelBuilder.Entity<AddKPIDefinitionResult>().HasNoKey();
            modelBuilder.Entity<GetKpiDefinitionByIdResult>().HasNoKey();
            modelBuilder.Entity<UpdateKPIDefinitionResult>().HasNoKey();
            modelBuilder.Entity<ToggleKPIStatusResult>().HasNoKey();
            modelBuilder.Entity<DeleteKPIDefinitionResult>().HasNoKey();
            modelBuilder.Entity<GetAllKpiResult>().HasNoKey();
            modelBuilder.Entity<ToggleJobStatusRequest>().HasNoKey();
            modelBuilder.Entity<GetRolesDto>().HasNoKey();
            modelBuilder.Entity<GetUnlinkedUsersDto>().HasNoKey();
            modelBuilder.Entity<UpdateUserRoleDto>().HasNoKey();
            modelBuilder.Entity<SpResult>().HasNoKey();
            modelBuilder.Entity<GetallRolesDto>().HasNoKey();
            modelBuilder.Entity<UpdateRoleDto>().HasNoKey();
            modelBuilder.Entity<AddRoleDto>().HasNoKey();
            modelBuilder.Entity<ProfilePictureDto>().HasNoKey();
            modelBuilder.Entity<EmployeeQualificationsSummaryDto>().HasNoKey();
            modelBuilder.Entity<EmployeeQualificationDetailDto>().HasNoKey();
            modelBuilder.Entity<CompanyProfileResponse>().HasNoKey();
            modelBuilder.Entity<EmployeeAccountStatusDto>().HasNoKey();
            modelBuilder.Entity<CompanyProfileDto>().HasNoKey();
            modelBuilder.Entity<AddInterviewResult>().HasNoKey();
            modelBuilder.Entity<UpdateInterviewResult>().HasNoKey();
            modelBuilder.Entity<DeleteInterviewResult>().HasNoKey();
            modelBuilder.Entity<InterviewWithNamesDto>().HasNoKey();
            modelBuilder.Entity<CompanyProfileDto>().HasNoKey();
            modelBuilder.Entity<EmployeeEmailDto>().HasNoKey();

            // Attendance -> Employee
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeID);

            // Department -> Manager (Employee)
            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithMany(e => e.ManagedDepartments)
                .HasForeignKey(d => d.ManagerID)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee -> Department
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentID);

            // Employee -> Position
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Position)
                .WithMany(p => p.Employees)
                .HasForeignKey(e => e.PositionID);

            // EmployeeTraining -> Employee
            modelBuilder.Entity<EmployeeTraining>()
                .HasOne(et => et.Employee)
                .WithMany(e => e.EmployeeTrainings)
                .HasForeignKey(et => et.EmployeeID);

            // EmployeeTraining -> Training
            modelBuilder.Entity<EmployeeTraining>()
                .HasOne(et => et.Training)
                .WithMany(t => t.EmployeeTrainings)
                .HasForeignKey(et => et.TrainingID);

            // Interview -> Application
            modelBuilder.Entity<Interview>()
                .HasOne(i => i.Application)
                .WithMany(a => a.Interviews)
                .HasForeignKey(i => i.ApplicationID);

       

            // JobOpening -> Department
            modelBuilder.Entity<JobOpening>()
                .HasOne(j => j.Department)
                .WithMany(d => d.JobOpenings)
                .HasForeignKey(j => j.DepartmentID);

            // LeaveRequest -> Employee
            modelBuilder.Entity<LeaveRequest>()
                .HasOne(lr => lr.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(lr => lr.EmployeeID);

            // Payroll -> Employee
            modelBuilder.Entity<Payroll>()
                .HasOne(p => p.Employee)
                .WithMany(e => e.Payrolls)
                .HasForeignKey(p => p.EmployeeID);

            // PerformanceReview -> Employee (Reviewed Employee)
            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.Employee)
                .WithMany(e => e.PerformanceReviews)
                .HasForeignKey(pr => pr.EmployeeID);

            // PerformanceReview -> Reviewer (Employee)
            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.Reviewer)
                .WithMany(e => e.ReviewsGiven)
                .HasForeignKey(pr => pr.ReviewerID)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}