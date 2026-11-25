using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHR.Models;

[Table("Departments")]
public class Department
{
    [Key]
    public int DepartmentId { get; set; }

    [Required, MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

[Table("Employees")]
public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Employee";

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    [Required, MaxLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    public int JobLevel { get; set; } = 1;

    [Column(TypeName = "decimal(10, 2)")]
    public decimal CurrentSalary { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PercentSalaryHike { get; set; }

    public DateTime HireDate { get; set; }
    public DateTime? LastPromotionDate { get; set; }

    public int DistanceFromHome { get; set; }
    public int Education { get; set; } = 3;

    [MaxLength(100)]
    public string? EducationField { get; set; }

    [Required, MaxLength(50)]
    public string BusinessTravel { get; set; } = "Travel_Rarely";

    public int NumCompaniesWorked { get; set; }
    public int TrainingTimesLastYear { get; set; }
    public int AnnualLeaveBalance { get; set; } = 14;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Active";

    public DateTime? ResignationDate { get; set; }

    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Performance> Performances { get; set; } = new List<Performance>();
    public ICollection<SurveyResponse> SurveyResponses { get; set; } = new List<SurveyResponse>();
    public ICollection<EmployeeRequest> EmployeeRequests { get; set; } = new List<EmployeeRequest>();
    public ICollection<AttritionPrediction> AttritionPredictions { get; set; } = new List<AttritionPrediction>();
}

[Table("Attendance")]
public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime WorkDate { get; set; }
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public bool IsAbsent { get; set; }
}

[Table("Performance")]
public class Performance
{
    [Key]
    public int PerformanceId { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime ReviewDate { get; set; }
    public int PerformanceRating { get; set; }
    public int JobInvolvement { get; set; }
}

[Table("SurveyResponses")]
public class SurveyResponse
{
    [Key]
    public int ResponseId { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int EnvironmentSatisfaction { get; set; }
    public int JobSatisfaction { get; set; }
    public int WorkLifeBalance { get; set; }

    public string? CommentText { get; set; }

    public DateTime SubmittedAt { get; set; }
}

[Table("EmployeeRequests")]
public class EmployeeRequest
{
    [Key]
    public int RequestId { get; set; }

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required, MaxLength(30)]
    public string RequestType { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TimeSpan? ExitTime { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? AdvanceAmount { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? CurrentSalary { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? RequestedRaiseAmount { get; set; }

    [MaxLength(255)]
    public string? AttachmentPath { get; set; }

    [MaxLength(150)]
    public string? AttachmentName { get; set; }

    [Required, MaxLength(20)]
    public string ManagerStatus { get; set; } = "Pending";

    [Required, MaxLength(20)]
    public string HRStatus { get; set; } = "Pending";

    [MaxLength(500)]
    public string? ManagerNote { get; set; }

    public DateTime CreatedAt { get; set; }
}

[Table("AttritionPredictions")]
public class AttritionPrediction
{
    [Key]
    public int PredictionId { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required, MaxLength(50)]
    public string ModelVersion { get; set; } = "v2026-11-24";

    [Column(TypeName = "decimal(5, 2)")]
    public decimal RiskProbability { get; set; }

    [Required, MaxLength(20)]
    public string RiskLevel { get; set; } = "low";

    [MaxLength(250)]
    public string? PrimaryReason { get; set; }

    [MaxLength(250)]
    public string? SuggestedAction { get; set; }

    public DateTime PredictionDate { get; set; }
    public bool IsLatest { get; set; } = true;
}

public class AiEmployeeFeature
{
    public int employee_id { get; set; }
    public double avg_work_hours_365 { get; set; }
    public double avg_work_hours_90 { get; set; }
    public double work_hours_trend { get; set; }
    public double std_work_hours_365 { get; set; }
    public double late_rate_365 { get; set; }
    public int late_count_90 { get; set; }
    public double avg_late_minutes_90 { get; set; }
    public double late_trend { get; set; }
    public double early_leave_rate_365 { get; set; }
    public int early_leave_count_90 { get; set; }
    public double overtime_hours_90 { get; set; }
    public double overtime_rate_365 { get; set; }
    public int absence_days_365 { get; set; }
    public int absence_days_90 { get; set; }
    public double absence_trend { get; set; }
    public int absence_spells_365 { get; set; }
    public int bradford_factor_365 { get; set; }
    public int max_absence_streak_365 { get; set; }
    public int tenure_months { get; set; }
    public int months_since_promotion { get; set; }
    public double promotion_stagnation { get; set; }
    public int job_level { get; set; }
    public decimal monthly_income { get; set; }
    public decimal percent_salary_hike { get; set; }
    public int num_companies_worked { get; set; }
    public int training_times_last_year { get; set; }
    public int distance_from_home { get; set; }
    public int education { get; set; }
    public string department { get; set; } = string.Empty;
    public string job_role { get; set; } = string.Empty;
    public string business_travel { get; set; } = string.Empty;
    public string education_field { get; set; } = string.Empty;
    public int job_involvement { get; set; }
    public int performance_rating { get; set; }
    public int environment_satisfaction { get; set; }
    public int job_satisfaction { get; set; }
    public int work_life_balance { get; set; }
    public double satisfaction_mean { get; set; }
    public int satisfaction_min { get; set; }
}