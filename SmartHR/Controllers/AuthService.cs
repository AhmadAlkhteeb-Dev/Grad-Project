using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartHR.Data;
using SmartHR.Models;
using SmartHR_Platform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DbEmployee = SmartHR.Models.Employee;

namespace SmartHR_Platform.Controllers
{
    public interface IAuthService
    {
        AuthenticatedUserSession? Authenticate(string identifier, string password);
        IReadOnlyList<AuthenticatedUserSession> GetDemoAccounts();
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _dbContext;
        private readonly PasswordHasher<DbEmployee> _passwordHasher = new();

        private static readonly List<AuthenticatedUserSession> _demoUsers = new()
        {
            new AuthenticatedUserSession
            {
                Identifier = "EMP-001",
                DisplayName = "أحمد المنصور",
                Role = "Admin",
                Department = "Human Resources"
            },
            new AuthenticatedUserSession
            {
                Identifier = "EMP-002",
                DisplayName = "سارة العلي",
                Role = "HR",
                Department = "Human Resources"
            },
            new AuthenticatedUserSession
            {
                Identifier = "EMP-003",
                DisplayName = "خالد الزعبي",
                Role = "Manager",
                Department = "Sales"
            },
            new AuthenticatedUserSession
            {
                Identifier = "EMP-004",
                DisplayName = "عمر الخطيب",
                Role = "Employee",
                Department = "Sales"
            }
        };

        public AuthService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private bool IsValidPassword(DbEmployee employee, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(employee.PasswordHash))
                return false;

            // إذا كان الهاش في الداتا بيس قيمة تجريبية نصية مؤقتة مثل HASH_123456
            if (employee.PasswordHash.StartsWith("HASH_", StringComparison.OrdinalIgnoreCase))
            {
                var expectedDevPass = employee.PasswordHash["HASH_".Length..];
                return providedPassword == expectedDevPass || providedPassword == employee.PasswordHash;
            }

            // التحقق القياسي عبر PasswordHasher<Employee>
            var result = _passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, providedPassword);
            return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }

        public AuthenticatedUserSession? Authenticate(string identifier, string password)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                return null;

            var normalizedId = identifier.Trim();

            // 1. البحث والتحقق من جدول Employees في قاعدة البيانات HR_SmartAnalyticsDB
            var dbEmployee = _dbContext.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefault(e =>
                    e.Status == "Active" &&
                    (e.EmployeeCode.ToLower() == normalizedId.ToLower() ||
                     e.Email.ToLower() == normalizedId.ToLower() ||
                     e.FullName.ToLower() == normalizedId.ToLower()));

            if (dbEmployee != null)
            {
                if (!IsValidPassword(dbEmployee, password))
                    return null;

                return new AuthenticatedUserSession
                {
                    Identifier = dbEmployee.EmployeeCode,
                    DisplayName = dbEmployee.FullName,
                    Role = dbEmployee.Role,
                    Department = dbEmployee.Department?.DepartmentName ?? "General"
                };
            }

            return null;
        }

        public IReadOnlyList<AuthenticatedUserSession> GetDemoAccounts()
        {
            var dbAccounts = _dbContext.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Where(e => e.Status == "Active")
                .OrderBy(e => e.EmployeeId)
                .Select(e => new AuthenticatedUserSession
                {
                    Identifier = e.EmployeeCode,
                    DisplayName = e.FullName,
                    Role = e.Role,
                    Department = e.Department != null ? e.Department.DepartmentName : "General"
                })
                .ToList();

            return dbAccounts.Count > 0 ? dbAccounts : _demoUsers;
        }
    }
}