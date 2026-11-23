using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHR_Platform.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartHR_Platform.Controllers
{
    public record RecentDecisionItem(int RequestId, string EmployeeName, string Decision, string Note, string TimeText);
    public record BulkApproveRequest(List<int> Ids, string? Note);
    public record SupportTicketItem(string TicketNo, string Category, string Priority, string Summary, string Status, string CreatedAtText);

    public interface IDashboardService
    {
        DashboardViewData GetDashboardData(string role, string employeeName);
        Task SubmitRequestAsync(EmployeeRequest model, IFormFile? attachmentFile, bool isAnonymous, string webRootPath);
        void EditRequest(int id, string? details, DateTime? startDate, DateTime? endDate, string? exitTime, decimal? advanceAmount, decimal? currentSalary, decimal? requestedRaiseAmount);
        void CancelRequest(int id);
        (bool Success, string Message, object? Payload) DecideRequest(int id, bool isApproved, string? managerNote);
        (bool Success, string Message, object? Payload) BulkApprove(BulkApproveRequest? payload);
        (bool Success, string Message, SupportTicketItem? Ticket) SubmitSupportTicket(string? category, string? priority, string? description, string? queryEmployeeName, string? identityName);
        byte[] BuildExecutiveReportBytes();
    }

    public record DashboardViewData(
        string CurrentRole,
        string CurrentEmployeeName,
        int CompanySatisfactionScore,
        bool IsManagerSatisfied,
        string ManagerFeedbackSummary,
        bool CanViewCompanyAnalytics,
        bool CanViewAuditTrail,
        List<EmployeeRequest> VisibleRequests,
        List<EmployeeRequest> AllRequestsForHistory,
        List<RecentDecisionItem> RecentDecisions,
        int PendingCount,
        int ApprovedCount,
        int RejectedCount,
        int AnonymousCount,
        List<AuditLogEntry> AuditLogs,
        List<SupportTicketItem> SupportTickets
    );

    public class DashboardService : IDashboardService
    {
        private static readonly object _syncLock = new();

        private static readonly List<EmployeeRequest> _requests = new()
        {
            new EmployeeRequest
            {
                Id = 101,
                EmployeeName = "سارة العلي",
                Department = "تطوير البرمجيات",
                Type = RequestType.SalaryRaise,
                CurrentSalary = 950,
                RequestedRaiseAmount = 150,
                Details = "تولي مهام قيادة الفريق التقني وتحقيق الأهداف الربعية بنسبة 115%.",
                CreatedAt = DateTime.Now.AddHours(-5),
                Status = RequestStatus.Pending
            },
            new EmployeeRequest
            {
                Id = 102,
                EmployeeName = "سارة العلي",
                Department = "تطوير البرمجيات",
                Type = RequestType.AnnualLeave,
                StartDate = DateTime.Now.AddDays(3),
                EndDate = DateTime.Now.AddDays(6),
                Details = "إجازة سنوية مخططة مسبقاً.",
                CreatedAt = DateTime.Now.AddDays(-2),
                Status = RequestStatus.Approved,
                ManagerNote = "تمت الموافقة، نتمنى لك إجازة سعيدة."
            },
            new EmployeeRequest
            {
                Id = 103,
                EmployeeName = "سارة العلي",
                Department = "تطوير البرمجيات",
                Type = RequestType.SalaryAdvance,
                AdvanceAmount = 350,
                Details = "سلفة طارئة لتغطية مصاريف صيانة المنزل.",
                CreatedAt = DateTime.Now.AddDays(-4),
                Status = RequestStatus.Rejected,
                ManagerNote = "نعتذر، يوجد سلفة سابقة لم تُستكمل أقساطها بعد."
            },
            new EmployeeRequest
            {
                Id = 104,
                EmployeeName = "ليلى الحسن",
                Department = "المبيعات",
                Type = RequestType.AnnualLeave,
                StartDate = DateTime.Now.AddDays(4),
                EndDate = DateTime.Now.AddDays(7),
                Details = "إجازة سنوية لمتابعة أمور شخصية.",
                CreatedAt = DateTime.Now.AddDays(-1),
                Status = RequestStatus.Pending
            },
            new EmployeeRequest
            {
                Id = 105,
                EmployeeName = "موظف مجهول (سري)",
                Department = "الدعم الفني",
                Type = RequestType.AnonymousWhistleblowing,
                Details = "ضغط عمل مرتفع خلال المناوبات المسائية.",
                CreatedAt = DateTime.Now.AddHours(-12),
                Status = RequestStatus.Pending,
                IsAnonymous = true
            }
        };

        private static readonly List<AuditLogEntry> _auditLogs = new()
        {
            new AuditLogEntry { Id = 1, Action = "تسجيل دخول النظام", PerformedBy = "System Admin", Timestamp = DateTime.Now.AddMinutes(-45), Details = "دخول ناجح إلى لوحة التحكم المؤسسية" },
            new AuditLogEntry { Id = 2, Action = "اعتماد طلب إجازة", PerformedBy = "المدير المباشر", Timestamp = DateTime.Now.AddDays(-1), Details = "قبول طلب الإجازة #102 للموظفة سارة العلي" },
            new AuditLogEntry { Id = 3, Action = "استلام بلاغ سري", PerformedBy = "نظام الحماية", Timestamp = DateTime.Now.AddHours(-12), Details = "توجيه بلاغ مجهول المصدر إلى صندوق HR" }
        };

        private static readonly List<RecentDecisionItem> _recentDecisions = new()
        {
            new RecentDecisionItem(102, "سارة العلي", "مقبول", "تمت الموافقة، نتمنى لك إجازة سعيدة.", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm")),
            new RecentDecisionItem(103, "سارة العلي", "مرفوض", "نعتذر، يوجد سلفة سابقة لم تُستكمل أقساطها بعد.", DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd HH:mm"))
        };

        private static readonly List<SupportTicketItem> _supportTickets = new()
        {
            new SupportTicketItem("TKT-2041", "عطل فني", "متوسط", "تأخر ظهور إشعار تعديل تاريخ الإجازة بعد الحفظ.", "قيد المعالجة", "منذ ساعتين"),
            new SupportTicketItem("TKT-2038", "صلاحيات", "عالية", "التحقق من عزل البلاغات السرية عن المدير المباشر.", "مفتوحة", "أمس")
        };

        private static int GetNextAuditIdLocked() =>
            _auditLogs.Count > 0 ? _auditLogs.Max(a => a.Id) + 1 : 1;

        public DashboardViewData GetDashboardData(string role, string employeeName)
        {
            var validRoles = new[] { "Employee", "Manager", "HR", "Admin" };
            var normalizedRole = validRoles.Contains(role) ? role : "Employee";
            var normalizedName = string.IsNullOrWhiteSpace(employeeName) ? "سارة العلي" : employeeName.Trim();

            bool canViewCompanyAnalytics = normalizedRole is "Manager" or "HR";
            bool canViewAuditTrail = normalizedRole == "Admin";

            lock (_syncLock)
            {
                List<EmployeeRequest> visibleRequests = normalizedRole switch
                {
                    "Employee" => _requests
                        .Where(r => !r.IsAnonymous && string.Equals(r.EmployeeName, normalizedName, StringComparison.OrdinalIgnoreCase))
                        .ToList(),
                    "Manager" => _requests
                        .Where(r => !r.IsAnonymous)
                        .ToList(),
                    "HR" => _requests.ToList(),
                    _ => new List<EmployeeRequest>()
                };

                var allRequestsForHistory = _requests.Where(r => !r.IsAnonymous).ToList();
                var recentDecisions = _recentDecisions.Take(8).ToList();
                int pendingCount = visibleRequests.Count(r => r.Status == RequestStatus.Pending);
                int approvedCount = visibleRequests.Count(r => r.Status == RequestStatus.Approved);
                int rejectedCount = visibleRequests.Count(r => r.Status == RequestStatus.Rejected);
                int anonymousCount = _requests.Count(r => r.IsAnonymous);
                var auditLogs = canViewAuditTrail ? _auditLogs.ToList() : new List<AuditLogEntry>();
                var supportTickets = _supportTickets.ToList();

                return new DashboardViewData(
                    CurrentRole: normalizedRole,
                    CurrentEmployeeName: normalizedName,
                    CompanySatisfactionScore: 92,
                    IsManagerSatisfied: true,
                    ManagerFeedbackSummary: "أداء متميز والتزام عالٍ بمواعيد تسليم المشاريع.",
                    CanViewCompanyAnalytics: canViewCompanyAnalytics,
                    CanViewAuditTrail: canViewAuditTrail,
                    VisibleRequests: visibleRequests,
                    AllRequestsForHistory: allRequestsForHistory,
                    RecentDecisions: recentDecisions,
                    PendingCount: pendingCount,
                    ApprovedCount: approvedCount,
                    RejectedCount: rejectedCount,
                    AnonymousCount: anonymousCount,
                    AuditLogs: auditLogs,
                    SupportTickets: supportTickets
                );
            }
        }

        public async Task SubmitRequestAsync(EmployeeRequest model, IFormFile? attachmentFile, bool isAnonymous, string webRootPath)
        {
            model.Status = RequestStatus.Pending;
            model.CreatedAt = DateTime.Now;
            model.IsAnonymous = isAnonymous;

            string originalEmployee = string.IsNullOrWhiteSpace(model.EmployeeName)
                ? "سارة العلي"
                : model.EmployeeName.Trim();

            if (attachmentFile is { Length: > 0 })
            {
                string uploadsFolder = Path.Combine(webRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string safeFileName = Path.GetFileName(attachmentFile.FileName);
                string uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await attachmentFile.CopyToAsync(stream);

                model.AttachmentPath = "/uploads/" + uniqueFileName;
                model.AttachmentName = safeFileName;
            }

            if (isAnonymous)
            {
                model.EmployeeName = "موظف مجهول (سري)";
                if (string.IsNullOrWhiteSpace(model.Department))
                {
                    model.Department = "قسم محمي";
                }
            }
            else
            {
                model.EmployeeName = originalEmployee;
            }

            lock (_syncLock)
            {
                model.Id = _requests.Count > 0 ? _requests.Max(r => r.Id) + 1 : 101;
                _requests.Insert(0, model);

                _auditLogs.Insert(0, new AuditLogEntry
                {
                    Id = GetNextAuditIdLocked(),
                    Action = isAnonymous ? "إرسال بلاغ سري مجهول" : "تقديم طلب جديد",
                    PerformedBy = isAnonymous ? "مجهول" : originalEmployee,
                    Timestamp = DateTime.Now,
                    Details = $"الطلب #{model.Id} - النوع: {model.Type}"
                });
            }
        }

        public void EditRequest(int id, string? details, DateTime? startDate, DateTime? endDate, string? exitTime, decimal? advanceAmount, decimal? currentSalary, decimal? requestedRaiseAmount)
        {
            lock (_syncLock)
            {
                var req = _requests.FirstOrDefault(r => r.Id == id);
                if (req != null && req.Status == RequestStatus.Pending)
                {
                    if (details != null) req.Details = details;
                    req.StartDate = startDate;
                    req.EndDate = endDate;
                    req.ExitTime = exitTime;
                    req.AdvanceAmount = advanceAmount;
                    req.CurrentSalary = currentSalary;
                    req.RequestedRaiseAmount = requestedRaiseAmount;

                    _auditLogs.Insert(0, new AuditLogEntry
                    {
                        Id = GetNextAuditIdLocked(),
                        Action = "تعديل طلب معلق",
                        PerformedBy = req.EmployeeName,
                        Timestamp = DateTime.Now,
                        Details = $"تعديل بيانات الطلب #{id}"
                    });
                }
            }
        }

        public void CancelRequest(int id)
        {
            lock (_syncLock)
            {
                var req = _requests.FirstOrDefault(r => r.Id == id);
                if (req != null && req.Status == RequestStatus.Pending)
                {
                    req.Status = RequestStatus.Cancelled;
                    req.ManagerNote = "تم إلغاء الطلب من قِبل الموظف.";

                    _auditLogs.Insert(0, new AuditLogEntry
                    {
                        Id = GetNextAuditIdLocked(),
                        Action = "إلغاء طلب من الموظف",
                        PerformedBy = req.EmployeeName,
                        Timestamp = DateTime.Now,
                        Details = $"إلغاء الطلب #{id}"
                    });
                }
            }
        }

        public (bool Success, string Message, object? Payload) DecideRequest(int id, bool isApproved, string? managerNote)
        {
            lock (_syncLock)
            {
                var req = _requests.FirstOrDefault(r => r.Id == id && !r.IsAnonymous);
                if (req == null)
                {
                    return (false, "الطلب غير موجود.", null);
                }

                if (!isApproved && string.IsNullOrWhiteSpace(managerNote))
                {
                    return (false, "ملاحظة القرار إجبارية عند رفض الطلب.", null);
                }

                string finalNote = string.IsNullOrWhiteSpace(managerNote)
                    ? "تمت الموافقة على الطلب."
                    : managerNote.Trim();

                req.Status = isApproved ? RequestStatus.Approved : RequestStatus.Rejected;
                req.ManagerNote = finalNote;

                string decisionLabel = isApproved ? "مقبول" : "مرفوض";
                string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                _recentDecisions.Insert(0, new RecentDecisionItem(req.Id, req.EmployeeName, decisionLabel, finalNote, timeNow));
                _auditLogs.Insert(0, new AuditLogEntry
                {
                    Id = GetNextAuditIdLocked(),
                    Action = isApproved ? "قبول طلب إداري" : "رفض طلب إداري",
                    PerformedBy = "المدير المباشر",
                    Timestamp = DateTime.Now,
                    Details = $"معرف الطلب #{id} - الحالة: {req.Status}"
                });

                var managerVisible = _requests.Where(r => !r.IsAnonymous).ToList();
                var payload = new
                {
                    success = true,
                    id = req.Id,
                    employeeName = req.EmployeeName,
                    status = req.Status.ToString(),
                    decisionLabel,
                    note = finalNote,
                    timeText = timeNow,
                    pendingCount = managerVisible.Count(r => r.Status == RequestStatus.Pending),
                    approvedCount = managerVisible.Count(r => r.Status == RequestStatus.Approved),
                    rejectedCount = managerVisible.Count(r => r.Status == RequestStatus.Rejected)
                };

                return (true, "تم تحديث حالة الطلب.", payload);
            }
        }

        public (bool Success, string Message, object? Payload) BulkApprove(BulkApproveRequest? payload)
        {
            if (payload?.Ids == null || !payload.Ids.Any())
            {
                return (false, "يرجى تحديد طلب واحد على الأقل.", null);
            }

            string note = string.IsNullOrWhiteSpace(payload.Note)
                ? "تمت الموافقة الجماعية من المدير المباشر."
                : payload.Note.Trim();

            string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            var updated = new List<object>();

            lock (_syncLock)
            {
                foreach (var id in payload.Ids)
                {
                    var req = _requests.FirstOrDefault(r => r.Id == id && !r.IsAnonymous && r.Status == RequestStatus.Pending);
                    if (req != null)
                    {
                        req.Status = RequestStatus.Approved;
                        req.ManagerNote = note;

                        _recentDecisions.Insert(0, new RecentDecisionItem(req.Id, req.EmployeeName, "مقبول (جماعي)", note, timeNow));
                        _auditLogs.Insert(0, new AuditLogEntry
                        {
                            Id = GetNextAuditIdLocked(),
                            Action = "موافقة جماعية",
                            PerformedBy = "المدير المباشر",
                            Timestamp = DateTime.Now,
                            Details = $"ضمن دفعة - اعتماد الطلب #{req.Id}"
                        });

                        updated.Add(new { id = req.Id, employeeName = req.EmployeeName, note, timeText = timeNow });
                    }
                }

                var managerVisible = _requests.Where(r => !r.IsAnonymous).ToList();
                var responsePayload = new
                {
                    success = true,
                    count = updated.Count,
                    updated,
                    pendingCount = managerVisible.Count(r => r.Status == RequestStatus.Pending),
                    approvedCount = managerVisible.Count(r => r.Status == RequestStatus.Approved),
                    rejectedCount = managerVisible.Count(r => r.Status == RequestStatus.Rejected)
                };

                return (true, "تمت الموافقة الجماعية.", responsePayload);
            }
        }

        public (bool Success, string Message, SupportTicketItem? Ticket) SubmitSupportTicket(string? category, string? priority, string? description, string? queryEmployeeName, string? identityName)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return (false, "يرجى كتابة وصف المشكلة.", null);
            }

            lock (_syncLock)
            {
                string ticketNo = "TKT-" + (2042 + _supportTickets.Count);
                string cat = string.IsNullOrWhiteSpace(category) ? "عطل فني" : category.Trim();
                string prio = string.IsNullOrWhiteSpace(priority) ? "متوسطة" : priority.Trim();
                string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                var item = new SupportTicketItem(ticketNo, cat, prio, description.Trim(), "مفتوحة", timeNow);
                _supportTickets.Insert(0, item);

                string performedBy = queryEmployeeName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(performedBy))
                {
                    performedBy = identityName ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(performedBy))
                {
                    performedBy = "مستخدم النظام";
                }

                _auditLogs.Insert(0, new AuditLogEntry
                {
                    Id = GetNextAuditIdLocked(),
                    Action = "فتح تذكرة دعم فني",
                    PerformedBy = performedBy,
                    Timestamp = DateTime.Now,
                    Details = $"{ticketNo} - {cat} ({prio})"
                });

                return (true, "تم إنشاء التذكرة.", item);
            }
        }

        public byte[] BuildExecutiveReportBytes()
        {
            lock (_syncLock)
            {
                var reportContent =
                    $"=== التقرير التنفيذي الشهري لاستقرار الكفاءات (SmartHR Enterprise) ===\n" +
                    $"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                    $"إجمالي الطلبات المسجلة: {_requests.Count}\n" +
                    $"الطلبات قيد الانتظار: {_requests.Count(r => r.Status == RequestStatus.Pending)}\n" +
                    $"الطلبات المعتمدة: {_requests.Count(r => r.Status == RequestStatus.Approved)}\n" +
                    $"البلاغات السرية المسجلة: {_requests.Count(r => r.IsAnonymous)}\n";

                return System.Text.Encoding.UTF8.GetBytes(reportContent);
            }
        }
    }

    public class DashboardController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IDashboardService _dashboardService;
        private readonly IAuthService _authService;

        public DashboardController(IWebHostEnvironment env, IDashboardService dashboardService, IAuthService authService)
        {
            _env = env;
            _dashboardService = dashboardService;
            _authService = authService;
        }

        private bool IsAjaxRequest() => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        public IActionResult PublicHome()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.DemoAccounts = _authService.GetDemoAccounts();
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DemoAccounts = _authService.GetDemoAccounts();
                return View(model);
            }

            var user = _authService.Authenticate(model.Identifier, model.Password);
            if (user == null)
            {
                model.ErrorMessage = "بيانات الدخول غير صحيحة، يرجى التحقق من المعرّف وكلمة المرور.";
                ViewBag.DemoAccounts = _authService.GetDemoAccounts();
                return View(model);
            }

            HttpContext.Session.SetString("SmartHR_Role", user.Role);
            HttpContext.Session.SetString("SmartHR_UserName", user.DisplayName);
            HttpContext.Session.SetString("SmartHR_Identifier", user.Identifier);

            return RedirectToAction("Index", new { role = user.Role, employeeName = user.DisplayName });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("PublicHome");
        }

        public IActionResult Index(string? role = null, string? employeeName = null)
        {
            role ??= HttpContext.Session.GetString("SmartHR_Role") ?? "Employee";
            employeeName ??= HttpContext.Session.GetString("SmartHR_UserName") ?? "سارة العلي";

            var data = _dashboardService.GetDashboardData(role, employeeName);

            ViewBag.CurrentRole = data.CurrentRole;
            ViewBag.CurrentEmployeeName = data.CurrentEmployeeName;
            ViewBag.CompanySatisfactionScore = data.CompanySatisfactionScore;
            ViewBag.IsManagerSatisfied = data.IsManagerSatisfied;
            ViewBag.ManagerFeedbackSummary = data.ManagerFeedbackSummary;
            ViewBag.CanViewCompanyAnalytics = data.CanViewCompanyAnalytics;
            ViewBag.CanViewAuditTrail = data.CanViewAuditTrail;
            ViewBag.AllRequestsForHistory = data.AllRequestsForHistory;
            ViewBag.RecentDecisions = data.RecentDecisions;
            ViewBag.PendingCount = data.PendingCount;
            ViewBag.ApprovedCount = data.ApprovedCount;
            ViewBag.RejectedCount = data.RejectedCount;
            ViewBag.AnonymousCount = data.AnonymousCount;
            ViewBag.AuditLogs = data.AuditLogs;
            ViewBag.SupportTickets = data.SupportTickets;

            return View(data.VisibleRequests);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRequest(EmployeeRequest model, IFormFile? attachmentFile, bool isAnonymous = false)
        {
            await _dashboardService.SubmitRequestAsync(model, attachmentFile, isAnonymous, _env.WebRootPath);

            string originalEmployee = string.IsNullOrWhiteSpace(model.EmployeeName) || isAnonymous
                ? HttpContext.Session.GetString("SmartHR_UserName") ?? "سارة العلي"
                : model.EmployeeName;

            if (isAnonymous)
            {
                return RedirectToAction("Index", new { role = "HR" });
            }

            return RedirectToAction("Index", new { role = "Employee", employeeName = originalEmployee });
        }

        [HttpPost]
        public IActionResult EditRequest(int id, string? details, DateTime? startDate, DateTime? endDate, string? exitTime, decimal? advanceAmount, decimal? currentSalary, decimal? requestedRaiseAmount)
        {
            _dashboardService.EditRequest(id, details, startDate, endDate, exitTime, advanceAmount, currentSalary, requestedRaiseAmount);
            var role = HttpContext.Session.GetString("SmartHR_Role") ?? "Employee";
            return RedirectToAction("Index", new { role });
        }

        [HttpPost]
        public IActionResult CancelRequest(int id)
        {
            _dashboardService.CancelRequest(id);
            var role = HttpContext.Session.GetString("SmartHR_Role") ?? "Employee";
            return RedirectToAction("Index", new { role });
        }

        [HttpPost]
        public IActionResult DecideRequest(int id, bool isApproved, string? managerNote)
        {
            var result = _dashboardService.DecideRequest(id, isApproved, managerNote);

            if (!result.Success)
            {
                return IsAjaxRequest()
                    ? Json(new { success = false, message = result.Message })
                    : RedirectToAction("Index", new { role = "Manager" });
            }

            return IsAjaxRequest()
                ? Json(result.Payload)
                : RedirectToAction("Index", new { role = "Manager" });
        }

        [HttpPost]
        public IActionResult BulkApprove([FromBody] BulkApproveRequest? payload)
        {
            var result = _dashboardService.BulkApprove(payload);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(result.Payload);
        }

        [HttpPost]
        public IActionResult SubmitSupportTicket(string? category, string? priority, string? description)
        {
            var sessionUser = HttpContext.Session.GetString("SmartHR_UserName");
            var result = _dashboardService.SubmitSupportTicket(
                category,
                priority,
                description,
                Request.Query["employeeName"].FirstOrDefault() ?? sessionUser,
                User.Identity?.Name
            );

            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, ticket = result.Ticket });
        }

        [HttpGet]
        public IActionResult ExportExecutiveReport()
        {
            var fileBytes = _dashboardService.BuildExecutiveReportBytes();
            return File(fileBytes, "text/plain; charset=utf-8", $"Executive_HR_Report_{DateTime.Now:yyyyMMdd}.txt");
        }
    }
}