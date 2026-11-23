using Microsoft.AspNetCore.Mvc;
using SmartHR_Platform.Controllers;
using SmartHR_Platform.Models;
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

        public MultiRoleService(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public EmployeeDashboardViewModel BuildEmployeeDashboard(string? employeeName = null)
        {
            var targetName = string.IsNullOrWhiteSpace(employeeName) ? "سارة العلي" : employeeName.Trim();
            var data = _dashboardService.GetDashboardData("Employee", targetName);

            var employeeSalary = data.VisibleRequests
                .Where(r => r.CurrentSalary.HasValue)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => r.CurrentSalary!.Value)
                .FirstOrDefault();

            var myRequestSummaries = data.VisibleRequests
                .Select(r => $"#{r.Id} - {r.Type} ({r.Status}): {r.Details}")
                .ToList();

            return new EmployeeDashboardViewModel
            {
                EmployeeName = data.CurrentEmployeeName,
                Department = data.VisibleRequests.FirstOrDefault()?.Department ?? "تطوير البرمجيات",
                Salary = employeeSalary,
                MyRequests = myRequestSummaries
            };
        }

        public ManagerDashboardViewModel BuildManagerDashboard(string? managerName = null)
        {
            var data = _dashboardService.GetDashboardData("Manager", managerName ?? "م. خالد العمري");

            var pendingTeamRequests = data.VisibleRequests
                .Where(r => r.Status == RequestStatus.Pending)
                .Select(r => $"#{r.Id} - {r.EmployeeName} ({r.Department}): {r.Type}")
                .ToList();

            return new ManagerDashboardViewModel
            {
                ManagerName = string.IsNullOrWhiteSpace(managerName) ? "م. خالد العمري" : managerName.Trim(),
                TeamSize = 12,
                PendingTeamRequests = pendingTeamRequests
            };
        }

        public HrDashboardViewModel BuildHrDashboard()
        {
            var data = _dashboardService.GetDashboardData("HR", "الموارد البشرية");

            return new HrDashboardViewModel
            {
                TotalEmployees = 240,
                CriticalAtRisk = data.AnonymousCount > 0 ? Math.Max(27, data.AnonymousCount) : 27,
                TotalTurnoverCost = 3000000m,
                CompanyStabilityIndex = 88.0
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