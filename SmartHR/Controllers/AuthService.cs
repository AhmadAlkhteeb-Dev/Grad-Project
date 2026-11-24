using Microsoft.EntityFrameworkCore;
using SmartHR.Data;
using SmartHR_Platform.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private static readonly List<AuthenticatedUserSession> _demoUsers = new()
        {
            new AuthenticatedUserSession
            {
                Identifier = "EMP-1003",
                DisplayName = "طارق الزعبي",
                Role = "Employee",
                Department = "Research & Development"
            },
            new AuthenticatedUserSession
            {
                Identifier = "EMP-1002",
                DisplayName = "أحمد المنصور",
                Role = "Manager",
                Department = "Research & Development"
            },
            new AuthenticatedUserSession
            {
                Identifier = "EMP-1001",
                DisplayName = "سارة العلي",
                Role = "HR",
                Department = "Human Resources"
            },
            new AuthenticatedUserSession
            {
                Identifier = "EMP-1004",
                DisplayName = "ليان حداد",
                Role = "Employee",
                Department = "Sales"
            }
        };

        public AuthService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public AuthenticatedUserSession? Authenticate(string identifier, string password)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                return null;

            var normalizedId = identifier.Trim();

            // 1. البحث المباشر في جدول Employees من قاعدة البيانات HR_SmartAnalyticsDB
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
                return new AuthenticatedUserSession
                {
                    Identifier = dbEmployee.EmployeeCode,
                    DisplayName = dbEmployee.FullName,
                    Role = dbEmployee.Role,
                    Department = dbEmployee.Department?.DepartmentName ?? "General"
                };
            }

            // 2. احتياطي للحسابات التجريبية أثناء التطوير
            var matched = _demoUsers.FirstOrDefault(u =>
                u.Identifier.Equals(normalizedId, StringComparison.OrdinalIgnoreCase) ||
                u.DisplayName.Equals(normalizedId, StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                return new AuthenticatedUserSession
                {
                    Identifier = matched.Identifier,
                    DisplayName = matched.DisplayName,
                    Role = matched.Role,
                    Department = matched.Department
                };
            }

            // 3. دخول مرن لأي معرّف جديد أثناء التطوير
            var role = "Employee";
            if (normalizedId.StartsWith("MGR", StringComparison.OrdinalIgnoreCase)) role = "Manager";
            else if (normalizedId.StartsWith("HR", StringComparison.OrdinalIgnoreCase)) role = "HR";
            else if (normalizedId.StartsWith("ADM", StringComparison.OrdinalIgnoreCase)) role = "Admin";

            return new AuthenticatedUserSession
            {
                Identifier = normalizedId,
                DisplayName = normalizedId,
                Role = role,
                Department = role == "HR" ? "Human Resources" : "Research & Development"
            };
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