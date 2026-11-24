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

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Employee";

    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    [Required, MaxLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    public int JobLevel { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal MonthlyIncome { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PercentSalaryHike { get; set; }

    public DateTime HireDate { get; set; }
    public DateTime? LastPromotionDate { get; set; }

    public int DistanceFromHome { get; set; }
    public int Education { get; set; }

    [Required, MaxLength(100)]
    public string EducationField { get; set; } = string.Empty;

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

    [MaxLength(1000)]
    public string? CommentText { get; set; }

    public DateTime SubmittedAt { get; set; }
}

[Table("EmployeeRequests")]
public class EmployeeRequest
{
    [Key]
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required, MaxLength(30)]
    public string RequestType { get; set; } = string.Empty; // Leave, SalaryIncrease, Complaint

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? RequestedAmount { get; set; }

    [Required, MaxLength(20)]
    public string ManagerStatus { get; set; } = "Pending";

    [Required, MaxLength(20)]
    public string HRStatus { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; }
}

[Table("AttritionPredictions")]
public class AttritionPrediction
{
    [Key]
    public int PredictionId { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Required, MaxLength(30)]
    public string ModelVersion { get; set; } = "v2026-11-18";

    [Column(TypeName = "decimal(5, 2)")]
    public decimal RiskProbability { get; set; }

    [Required, MaxLength(20)]
    public string RiskLevel { get; set; } = "Low";

    [Required, MaxLength(250)]
    public string TopShapFactor { get; set; } = string.Empty;

    [Required, MaxLength(250)]
    public string SuggestedAction { get; set; } = string.Empty;

    public DateTime PredictionDate { get; set; }
    public bool IsLatest { get; set; } = true;
}

public class AiEmployeeFeature
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobRole { get; set; } = string.Empty;
    public int JobLevel { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal IncomeRatioInLevel { get; set; }
    public decimal PercentSalaryHike { get; set; }
    public int TenureMonths { get; set; }
    public int MonthsSincePromotion { get; set; }
    public int DistanceFromHome { get; set; }
    public int Education { get; set; }
    public string EducationField { get; set; } = string.Empty;
    public string BusinessTravel { get; set; } = string.Empty;
    public int NumCompaniesWorked { get; set; }
    public int TrainingTimesLastYear { get; set; }
    public int PerformanceRating { get; set; }
    public int JobInvolvement { get; set; }
    public int EnvironmentSatisfaction { get; set; }
    public int JobSatisfaction { get; set; }
    public int WorkLifeBalance { get; set; }
    public int AbsentDays_90d { get; set; }
    public int LateDays_90d { get; set; }
    public int EarlyLeaveDays_90d { get; set; }
    public int OvertimeDays_90d { get; set; }
    public int AbsentDays_365d { get; set; }
    public int LateDays_365d { get; set; }
    public int OvertimeDays_365d { get; set; }
    public decimal AvgDailyWorkHours_365d { get; set; }
}