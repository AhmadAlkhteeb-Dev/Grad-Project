using Microsoft.AspNetCore.Mvc;
using SmartHR_Platform.Models;

namespace SmartHR.Controllers
{
    public class MultiRoleController : Controller
    {
        // 1. صفحة الموظف
        public IActionResult Employee()
        {
            var model = new EmployeeDashboardViewModel();
            return View(model);
        }

        // 2. صفحة المدير المباشر
        public IActionResult Manager()
        {
            var model = new ManagerDashboardViewModel();
            return View(model);
        }

        // 3. صفحة الموارد البشرية
        public IActionResult HR()
        {
            var model = new HrDashboardViewModel();
            return View(model);
        }

        // 4. صفحة الزائر العادي / الواجهة العامة
        public IActionResult PublicVisitor()
        {
            var model = new PublicVisitorViewModel();
            return View(model);
        }
    }
}