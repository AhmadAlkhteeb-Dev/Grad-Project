using Microsoft.EntityFrameworkCore;
using SmartHR.Models;

namespace SmartHR.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Performance> Performances => Set<Performance>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<EmployeeRequest> EmployeeRequests => Set<EmployeeRequest>();
    public DbSet<AttritionPrediction> AttritionPredictions => Set<AttritionPrediction>();
    public DbSet<AiEmployeeFeature> AiEmployeeFeatures => Set<AiEmployeeFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(e => e.DepartmentId);
            entity.Property(e => e.DepartmentName).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.DepartmentName).IsUnique();
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.EmployeeId);

            entity.Property(e => e.EmployeeCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(150).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("Employee").IsRequired();
            entity.Property(e => e.JobTitle).HasMaxLength(100).IsRequired();
            entity.Property(e => e.JobLevel).HasDefaultValue(1);
            entity.Property(e => e.CurrentSalary).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PercentSalaryHike).HasColumnType("decimal(5, 2)").HasDefaultValue(0m);
            entity.Property(e => e.HireDate).HasColumnType("date");
            entity.Property(e => e.LastPromotionDate).HasColumnType("date");
            entity.Property(e => e.DistanceFromHome).HasDefaultValue(0);
            entity.Property(e => e.Education).HasDefaultValue(3);
            entity.Property(e => e.EducationField).HasMaxLength(100);
            entity.Property(e => e.BusinessTravel).HasMaxLength(50).HasDefaultValue("Travel_Rarely").IsRequired();
            entity.Property(e => e.NumCompaniesWorked).HasDefaultValue(0);
            entity.Property(e => e.TrainingTimesLastYear).HasDefaultValue(0);
            entity.Property(e => e.AnnualLeaveBalance).HasDefaultValue(14);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Active").IsRequired();
            entity.Property(e => e.ResignationDate).HasColumnType("date");

            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Manager)
                .WithMany(m => m.Subordinates)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("Attendance");
            entity.HasKey(e => e.AttendanceId);

            entity.Property(e => e.WorkDate).HasColumnType("date");
            entity.Property(e => e.CheckInTime).HasColumnType("time");
            entity.Property(e => e.CheckOutTime).HasColumnType("time");
            entity.Property(e => e.IsAbsent).HasDefaultValue(false);

            entity.HasIndex(e => new { e.EmployeeId, e.WorkDate })
                .IsUnique()
                .HasDatabaseName("UQ_Employee_WorkDate");

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Attendances)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Performance>(entity =>
        {
            entity.ToTable("Performance");
            entity.HasKey(e => e.PerformanceId);

            entity.Property(e => e.ReviewDate).HasColumnType("date");

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.Performances)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.ToTable("SurveyResponses");
            entity.HasKey(e => e.ResponseId);

            entity.Property(e => e.SubmittedAt).HasColumnType("datetime");

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.SurveyResponses)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeRequest>(entity =>
        {
            entity.ToTable("EmployeeRequests");
            entity.HasKey(e => e.RequestId);

            entity.Property(e => e.RequestType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(150).IsRequired();
            entity.Property(e => e.StartDate).HasColumnType("date");
            entity.Property(e => e.EndDate).HasColumnType("date");
            entity.Property(e => e.ExitTime).HasColumnType("time");
            entity.Property(e => e.AdvanceAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.CurrentSalary).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.RequestedRaiseAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.AttachmentPath).HasMaxLength(255);
            entity.Property(e => e.AttachmentName).HasMaxLength(150);
            entity.Property(e => e.ManagerStatus).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            entity.Property(e => e.HRStatus).HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
            entity.Property(e => e.ManagerNote).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.EmployeeRequests)
                .HasForeignKey(e => e.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AttritionPrediction>(entity =>
        {
            entity.ToTable("AttritionPredictions");
            entity.HasKey(e => e.PredictionId);

            entity.Property(e => e.ModelVersion).HasMaxLength(50).HasDefaultValue("v2026-11-24").IsRequired();
            entity.Property(e => e.RiskProbability).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RiskLevel).HasMaxLength(20).IsRequired();
            entity.Property(e => e.PrimaryReason).HasMaxLength(250);
            entity.Property(e => e.SuggestedAction).HasMaxLength(250);
            entity.Property(e => e.PredictionDate).HasColumnType("datetime");
            entity.Property(e => e.IsLatest).HasDefaultValue(true);

            entity.HasOne(e => e.Employee)
                .WithMany(emp => emp.AttritionPredictions)
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiEmployeeFeature>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_AI_EmployeeFeatures", "dbo");
            entity.Property(e => e.monthly_income).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.percent_salary_hike).HasColumnType("decimal(5, 2)");
        });
    }
}