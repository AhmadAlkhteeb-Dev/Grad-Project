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

    // Keyless entity mapped to vw_AI_EmployeeFeatures
    public DbSet<AiEmployeeFeature> AiEmployeeFeatures => Set<AiEmployeeFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>()
            .HasIndex(d => d.DepartmentName)
            .IsUnique();

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.Property(e => e.HireDate).HasColumnType("date");
            entity.Property(e => e.LastPromotionDate).HasColumnType("date");
            entity.Property(e => e.ResignationDate).HasColumnType("date");

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
            entity.HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique();
            entity.Property(a => a.WorkDate).HasColumnType("date");
            entity.Property(a => a.CheckInTime).HasColumnType("time");
            entity.Property(a => a.CheckOutTime).HasColumnType("time");

            entity.HasOne(a => a.Employee)
                .WithMany(e => e.Attendances)
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Performance>(entity =>
        {
            entity.Property(p => p.ReviewDate).HasColumnType("date");

            entity.HasOne(p => p.Employee)
                .WithMany(e => e.Performances)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.HasOne(s => s.Employee)
                .WithMany(e => e.SurveyResponses)
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeRequest>(entity =>
        {
            entity.Property(r => r.StartDate).HasColumnType("date");
            entity.Property(r => r.EndDate).HasColumnType("date");

            entity.HasOne(r => r.Employee)
                .WithMany(e => e.EmployeeRequests)
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttritionPrediction>(entity =>
        {
            entity.HasOne(p => p.Employee)
                .WithMany(e => e.AttritionPredictions)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiEmployeeFeature>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_AI_EmployeeFeatures");
            entity.Property(v => v.MonthlyIncome).HasColumnType("decimal(18, 2)");
            entity.Property(v => v.IncomeRatioInLevel).HasColumnType("decimal(6, 3)");
            entity.Property(v => v.PercentSalaryHike).HasColumnType("decimal(5, 2)");
            entity.Property(v => v.AvgDailyWorkHours_365d).HasColumnType("decimal(5, 2)");
        });
    }
}