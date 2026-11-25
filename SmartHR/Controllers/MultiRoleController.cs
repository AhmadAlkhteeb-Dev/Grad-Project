using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHR.Data;
using SmartHR_Platform.Controllers;
using SmartHR_Platform.Models;
using System;
using System.Linq;

namespace SmartHR.Controllers
{
    public interface IMultiRoleService
    {
        EmployeeDashboardViewModel BuildEmployeeDashboard(string? employeeName = null);
        ManagerDashboardViewModel BuildManagerDashboard(string? managerName = null);
        HrDashboardViewModel BuildHrDashboard();
        PublicVisitorViewModel BuildPublicVisitorView();
    }

    public class MultiRoleService : IMultiRoleService
    {
        private readonly IDashboardService _dashboardService;
        private readonly AppDbContext _dbContext;

        public MultiRoleService(IDashboardService dashboardService, AppDbContext dbContext)
        {
            _dashboardService = dashboardService;
            _dbContext = dbContext;
        }

        public EmployeeDashboardViewModel BuildEmployeeDashboard(string? employeeName = null)
        {
            var targetName = string.IsNullOrWhiteSpace(employeeName) ? "عمر الخطيب" : employeeName.Trim();
            var data = _dashboardService.GetDashboardData("Employee", targetName);

            var dbEmployee = _dbContext.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefault(e => e.FullName == targetName || e.EmployeeCode == targetName);

            var employeeSalary = dbEmployee?.CurrentSalary
                ?? data.VisibleRequests
                    .Where(r => r.CurrentSalary.HasValue)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => r.CurrentSalary!.Value)
                    .FirstOrDefault();

            var myRequestSummaries = data.VisibleRequests
                .Select(r => $"#{r.Id} - {r.Type} ({r.Status}): {r.Details}")
                .ToList();

            return new EmployeeDashboardViewModel
            {
                EmployeeName = dbEmployee?.FullName ?? data.CurrentEmployeeName,
                Department = dbEmployee?.Department?.DepartmentName
                    ?? data.VisibleRequests.FirstOrDefault()?.Department
                    ?? "Sales",
                Salary = employeeSalary,
                MyRequests = myRequestSummaries
            };
        }

        public ManagerDashboardViewModel BuildManagerDashboard(string? managerName = null)
        {
            var resolvedManagerName = string.IsNullOrWhiteSpace(managerName) ? "خالد الزعبي" : managerName.Trim();
            var data = _dashboardService.GetDashboardData("Manager", resolvedManagerName);

            var dbManager = _dbContext.Employees
                .AsNoTracking()
                .FirstOrDefault(e => e.FullName == resolvedManagerName || e.EmployeeCode == resolvedManagerName);

            var teamCount = dbManager != null
                ? _dbContext.Employees.AsNoTracking().Count(e => e.ManagerId == dbManager.EmployeeId && e.Status == "Active")
                : 0;

            var pendingTeamRequests = data.VisibleRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .Select(r => $"#{r.Id} - {r.EmployeeName} ({r.Department}): {r.Type}")
                .ToList();

            return new ManagerDashboardViewModel
            {
                ManagerName = dbManager?.FullName ?? resolvedManagerName,
                TeamSize = teamCount > 0 ? teamCount : 12,
                PendingTeamRequests = pendingTeamRequests
            };
        }

        public HrDashboardViewModel BuildHrDashboard()
        {
            var data = _dashboardService.GetDashboardData("HR", "سارة العلي");

            var activeEmployeesCount = _dbContext.Employees
                .AsNoTracking()
                .Count(e => e.Status == "Active");

            var latestPredictions = _dbContext.AttritionPredictions
                .AsNoTracking()
                .Where(p => p.IsLatest)
                .ToList();

            var highRiskCount = latestPredictions.Count(p => p.RiskLevel.ToLower() == "high");

            double stabilityIndex = latestPredictions.Count > 0
                ? Math.Round(100.0 - latestPredictions.Average(p => (double)p.RiskProbability), 1)
                : 88.0;

            return new HrDashboardViewModel
            {
                TotalEmployees = activeEmployeesCount > 0 ? activeEmployeesCount : 240,
                CriticalAtRisk = highRiskCount > 0 ? highRiskCount : (data.AnonymousCount > 0 ? data.AnonymousCount : 27),
                TotalTurnoverCost = 3000000m,
                CompanyStabilityIndex = stabilityIndex
            };
        }

        public PublicVisitorViewModel BuildPublicVisitorView()
        {
            return new PublicVisitorViewModel
            {
                PlatformTitle = "منصة الذكاء الاصطناعي لاستقرار الكفاءات",
                PublicMessage = "نظام متطور للتنبؤ بالدوران الوظيفي وتحليل ارتباط الموظفين",
                IsRegistered = false
            };
        }
    }

    public class MultiRoleController : Controller
    {
        private readonly IMultiRoleService _multiRoleService;

        public MultiRoleController(IMultiRoleService multiRoleService)
        {
            _multiRoleService = multiRoleService;
        }

        // 1. صفحة الموظف
        public IActionResult Employee(string? employeeName = null)
        {
            var model = _multiRoleService.BuildEmployeeDashboard(employeeName);
            return View(model);
        }

        // 2. صفحة المدير المباشر
        public IActionResult Manager(string? managerName = null)
        {
            var model = _multiRoleService.BuildManagerDashboard(managerName);
            return View(model);
        }

        // 3. صفحة الموارد البشرية
        public IActionResult HR()
        {
            var model = _multiRoleService.BuildHrDashboard();
            return View(model);
        }

        // 4. صفحة الزائر العادي / الواجهة العامة
        public IActionResult PublicVisitor()
        {
            var model = _multiRoleService.BuildPublicVisitorView();
            return View(model);
        }
    }
}