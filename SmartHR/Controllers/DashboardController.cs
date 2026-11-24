using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHR.Data;
using SmartHR_Platform.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DbRequest = SmartHR.Models.EmployeeRequest;

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
        private readonly AppDbContext _dbContext;
        private static readonly object _syncLock = new();

        private static readonly List<AuditLogEntry> _auditLogs = new()
        {
            new AuditLogEntry { Id = 1, Action = "تسجيل دخول النظام", PerformedBy = "System Admin", Timestamp = DateTime.Now.AddMinutes(-45), Details = "دخول ناجح إلى لوحة التحكم المؤسسية" },
            new AuditLogEntry { Id = 2, Action = "ربط قاعدة البيانات", PerformedBy = "النظام", Timestamp = DateTime.Now.AddMinutes(-10), Details = "تفعيل الاتصال بقاعدة HR_SmartAnalyticsDB عبر Entity Framework Core" }
        };

        private static readonly List<SupportTicketItem> _supportTickets = new()
        {
            new SupportTicketItem("TKT-2041", "عطل فني", "متوسط", "تأخر ظهور إشعار تعديل تاريخ الإجازة بعد الحفظ.", "قيد المعالجة", "منذ ساعتين"),
            new SupportTicketItem("TKT-2038", "صلاحيات", "عالية", "التحقق من عزل البلاغات السرية عن المدير المباشر.", "مفتوحة", "أمس")
        };

        public DashboardService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static int GetNextAuditIdLocked() =>
            _auditLogs.Count > 0 ? _auditLogs.Max(a => a.Id) + 1 : 1;

        private static RequestType MapDbTypeToUiType(string dbType) => dbType switch
        {
            "SalaryIncrease" => RequestType.SalaryRaise,
            "Complaint" => RequestType.AnonymousWhistleblowing,
            _ => RequestType.AnnualLeave
        };

        private static string MapUiTypeToDbType(RequestType type, bool isAnonymous)
        {
            if (isAnonymous || type == RequestType.AnonymousWhistleblowing)
                return "Complaint";

            return type switch
            {
                RequestType.SalaryRaise => "SalaryIncrease",
                _ => "Leave"
            };
        }

        private static RequestStatus MapDbStatusesToUiStatus(string managerStatus, string hrStatus)
        {
            if (managerStatus == "Rejected" || hrStatus == "Rejected")
                return RequestStatus.Rejected;

            if (managerStatus == "Approved" && hrStatus == "Approved")
                return RequestStatus.Approved;

            if (managerStatus == "Approved" && hrStatus == "Pending")
                return RequestStatus.Pending;

            return RequestStatus.Pending;
        }

        private EmployeeRequest MapToUiRequest(DbRequest dbReq)
        {
            bool isAnon = dbReq.RequestType == "Complaint";
            string empName = isAnon
                ? "موظف مجهول (سري)"
                : (dbReq.Employee?.FullName ?? "موظف");

            string deptName = isAnon
                ? "قسم محمي"
                : (dbReq.Employee?.Department?.DepartmentName ?? "General");

            string combinedNote = string.Empty;
            if (!string.IsNullOrWhiteSpace(dbReq.ManagerStatus) && dbReq.ManagerStatus != "Pending")
                combinedNote = $"المدير: {dbReq.ManagerStatus}";
            if (!string.IsNullOrWhiteSpace(dbReq.HRStatus) && dbReq.HRStatus != "Pending")
                combinedNote = string.IsNullOrWhiteSpace(combinedNote)
                    ? $"HR: {dbReq.HRStatus}"
                    : $"{combinedNote} | HR: {dbReq.HRStatus}";

            return new EmployeeRequest
            {
                Id = dbReq.RequestId,
                EmployeeName = empName,
                Department = deptName,
                Type = MapDbTypeToUiType(dbReq.RequestType),
                Details = dbReq.Description ?? dbReq.Title,
                StartDate = dbReq.StartDate,
                EndDate = dbReq.EndDate,
                CurrentSalary = dbReq.Employee?.MonthlyIncome,
                RequestedRaiseAmount = dbReq.RequestType == "SalaryIncrease" ? dbReq.RequestedAmount : null,
                AdvanceAmount = dbReq.RequestType == "Leave" ? null : dbReq.RequestedAmount,
                CreatedAt = dbReq.CreatedAt,
                Status = MapDbStatusesToUiStatus(dbReq.ManagerStatus, dbReq.HRStatus),
                ManagerNote = combinedNote,
                IsAnonymous = isAnon
            };
        }

        private List<EmployeeRequest> LoadAllUiRequests()
        {
            var dbItems = _dbContext.EmployeeRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                    .ThenInclude(e => e!.Department)
                .OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.RequestId)
                .ToList();

            return dbItems.Select(MapToUiRequest).ToList();
        }

        public DashboardViewData GetDashboardData(string role, string employeeName)
        {
            var validRoles = new[] { "Employee", "Manager", "HR", "Admin" };
            var normalizedRole = validRoles.Contains(role) ? role : "Employee";
            var normalizedName = string.IsNullOrWhiteSpace(employeeName) ? "سارة العلي" : employeeName.Trim();

            bool canViewCompanyAnalytics = normalizedRole is "Manager" or "HR";
            bool canViewAuditTrail = normalizedRole == "Admin";

            var allUiRequests = LoadAllUiRequests();

            var latestPredictions = _dbContext.AttritionPredictions
                .AsNoTracking()
                .Where(p => p.IsLatest)
                .ToList();

            int satisfactionScore = 88;
            if (latestPredictions.Count > 0)
            {
                var avgRisk = latestPredictions.Average(p => (double)p.RiskProbability);
                satisfactionScore = Math.Clamp((int)Math.Round(100.0 - avgRisk), 10, 99);
            }

            List<EmployeeRequest> visibleRequests = normalizedRole switch
            {
                "Employee" => allUiRequests
                    .Where(r => !r.IsAnonymous && string.Equals(r.EmployeeName, normalizedName, StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                "Manager" => allUiRequests
                    .Where(r => !r.IsAnonymous)
                    .ToList(),
                "HR" => allUiRequests.ToList(),
                _ => new List<EmployeeRequest>()
            };

            var allRequestsForHistory = allUiRequests.Where(r => !r.IsAnonymous).ToList();

            var recentDecisions = _dbContext.EmployeeRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                .Where(r => r.RequestType != "Complaint" && (r.ManagerStatus != "Pending" || r.HRStatus != "Pending"))
                .OrderByDescending(r => r.CreatedAt)
                .Take(8)
                .ToList()
                .Select(r =>
                {
                    string decision = (r.ManagerStatus == "Rejected" || r.HRStatus == "Rejected") ? "مرفوض"
                        : (r.ManagerStatus == "Approved" && r.HRStatus == "Approved") ? "مقبول نهائياً"
                        : "مقبول من المدير";
                    string note = $"المدير: {r.ManagerStatus} | HR: {r.HRStatus}";
                    return new RecentDecisionItem(
                        r.RequestId,
                        r.Employee?.FullName ?? "موظف",
                        decision,
                        note,
                        r.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                    );
                })
                .ToList();

            int pendingCount = visibleRequests.Count(r => r.Status == RequestStatus.Pending);
            int approvedCount = visibleRequests.Count(r => r.Status == RequestStatus.Approved);
            int rejectedCount = visibleRequests.Count(r => r.Status == RequestStatus.Rejected);
            int anonymousCount = allUiRequests.Count(r => r.IsAnonymous);

            lock (_syncLock)
            {
                var auditLogs = canViewAuditTrail ? _auditLogs.ToList() : new List<AuditLogEntry>();
                var supportTickets = _supportTickets.ToList();

                return new DashboardViewData(
                    CurrentRole: normalizedRole,
                    CurrentEmployeeName: normalizedName,
                    CompanySatisfactionScore: satisfactionScore,
                    IsManagerSatisfied: true,
                    ManagerFeedbackSummary: "البيانات متصلة مباشرة بقاعدة بيانات HR_SmartAnalyticsDB.",
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
            string originalEmployee = string.IsNullOrWhiteSpace(model.EmployeeName)
                ? "طارق الزعبي"
                : model.EmployeeName.Trim();

            var employeeEntity = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.FullName == originalEmployee || e.EmployeeCode == originalEmployee)
                ?? await _dbContext.Employees.OrderBy(e => e.EmployeeId).FirstAsync();

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

            string dbType = MapUiTypeToDbType(model.Type, isAnonymous);
            string title = dbType switch
            {
                "SalaryIncrease" => "طلب مراجعة راتب",
                "Complaint" => "بلاغ سري مجهول",
                _ => "طلب إجازة"
            };

            decimal? amount = model.RequestedRaiseAmount ?? model.AdvanceAmount;

            var dbRequest = new DbRequest
            {
                EmployeeId = employeeEntity.EmployeeId,
                RequestType = dbType,
                Title = title,
                Description = model.Details,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                RequestedAmount = amount,
                ManagerStatus = "Pending",
                HRStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            _dbContext.EmployeeRequests.Add(dbRequest);
            await _dbContext.SaveChangesAsync();

            lock (_syncLock)
            {
                _auditLogs.Insert(0, new AuditLogEntry
                {
                    Id = GetNextAuditIdLocked(),
                    Action = isAnonymous ? "إرسال بلاغ سري مجهول" : "تقديم طلب جديد",
                    PerformedBy = isAnonymous ? "مجهول" : employeeEntity.FullName,
                    Timestamp = DateTime.Now,
                    Details = $"الطلب #{dbRequest.RequestId} - النوع: {dbType}"
                });
            }
        }

        public void EditRequest(int id, string? details, DateTime? startDate, DateTime? endDate, string? exitTime, decimal? advanceAmount, decimal? currentSalary, decimal? requestedRaiseAmount)
        {
            var dbReq = _dbContext.EmployeeRequests.FirstOrDefault(r => r.RequestId == id);
            if (dbReq != null && dbReq.ManagerStatus == "Pending" && dbReq.HRStatus == "Pending")
            {
                if (details != null) dbReq.Description = details;
                dbReq.StartDate = startDate;
                dbReq.EndDate = endDate;
                if (requestedRaiseAmount.HasValue) dbReq.RequestedAmount = requestedRaiseAmount;
                else if (advanceAmount.HasValue) dbReq.RequestedAmount = advanceAmount;

                _dbContext.SaveChanges();

                lock (_syncLock)
                {
                    _auditLogs.Insert(0, new AuditLogEntry
                    {
                        Id = GetNextAuditIdLocked(),
                        Action = "تعديل طلب معلق",
                        PerformedBy = "مستخدم النظام",
                        Timestamp = DateTime.Now,
                        Details = $"تعديل بيانات الطلب #{id}"
                    });
                }
            }
        }

        public void CancelRequest(int id)
        {
            var dbReq = _dbContext.EmployeeRequests.FirstOrDefault(r => r.RequestId == id);
            if (dbReq != null && dbReq.ManagerStatus == "Pending" && dbReq.HRStatus == "Pending")
            {
                dbReq.ManagerStatus = "Rejected";
                dbReq.HRStatus = "Rejected";
                _dbContext.SaveChanges();

                lock (_syncLock)
                {
                    _auditLogs.Insert(0, new AuditLogEntry
                    {
                        Id = GetNextAuditIdLocked(),
                        Action = "إلغاء طلب من الموظف",
                        PerformedBy = "مستخدم النظام",
                        Timestamp = DateTime.Now,
                        Details = $"إلغاء الطلب #{id}"
                    });
                }
            }
        }

        public (bool Success, string Message, object? Payload) DecideRequest(int id, bool isApproved, string? managerNote)
        {
            var dbReq = _dbContext.EmployeeRequests
                .Include(r => r.Employee)
                .FirstOrDefault(r => r.RequestId == id && r.RequestType != "Complaint");

            if (dbReq == null)
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

            dbReq.ManagerStatus = isApproved ? "Approved" : "Rejected";
            if (!isApproved)
            {
                dbReq.HRStatus = "Rejected";
            }

            _dbContext.SaveChanges();

            string decisionLabel = isApproved ? "مقبول" : "مرفوض";
            string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string empName = dbReq.Employee?.FullName ?? "موظف";

            lock (_syncLock)
            {
                _auditLogs.Insert(0, new AuditLogEntry
                {
                    Id = GetNextAuditIdLocked(),
                    Action = isApproved ? "قبول طلب إداري" : "رفض طلب إداري",
                    PerformedBy = "المدير المباشر",
                    Timestamp = DateTime.Now,
                    Details = $"معرف الطلب #{id} - الحالة: {dbReq.ManagerStatus} ({finalNote})"
                });
            }

            var managerVisible = LoadAllUiRequests().Where(r => !r.IsAnonymous).ToList();
            var payload = new
            {
                success = true,
                id = dbReq.RequestId,
                employeeName = empName,
                status = isApproved ? RequestStatus.Approved.ToString() : RequestStatus.Rejected.ToString(),
                decisionLabel,
                note = finalNote,
                timeText = timeNow,
                pendingCount = managerVisible.Count(r => r.Status == RequestStatus.Pending),
                approvedCount = managerVisible.Count(r => r.Status == RequestStatus.Approved),
                rejectedCount = managerVisible.Count(r => r.Status == RequestStatus.Rejected)
            };

            return (true, "تم تحديث حالة الطلب.", payload);
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

            var targetRequests = _dbContext.EmployeeRequests
                .Include(r => r.Employee)
                .Where(r => payload.Ids.Contains(r.RequestId) && r.RequestType != "Complaint" && r.ManagerStatus == "Pending")
                .ToList();

            foreach (var req in targetRequests)
            {
                req.ManagerStatus = "Approved";
                updated.Add(new { id = req.RequestId, employeeName = req.Employee?.FullName ?? "موظف", note, timeText = timeNow });

                lock (_syncLock)
                {
                    _auditLogs.Insert(0, new AuditLogEntry
                    {
                        Id = GetNextAuditIdLocked(),
                        Action = "موافقة جماعية",
                        PerformedBy = "المدير المباشر",
                        Timestamp = DateTime.Now,
                        Details = $"ضمن دفعة - اعتماد الطلب #{req.RequestId}"
                    });
                }
            }

            _dbContext.SaveChanges();

            var managerVisible = LoadAllUiRequests().Where(r => !r.IsAnonymous).ToList();
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
            var totalRequests = _dbContext.EmployeeRequests.Count();
            var pendingRequests = _dbContext.EmployeeRequests.Count(r => r.ManagerStatus == "Pending" || r.HRStatus == "Pending");
            var approvedRequests = _dbContext.EmployeeRequests.Count(r => r.ManagerStatus == "Approved" && r.HRStatus == "Approved");
            var anonymousReports = _dbContext.EmployeeRequests.Count(r => r.RequestType == "Complaint");
            var highRiskCount = _dbContext.AttritionPredictions.Count(p => p.IsLatest && p.RiskLevel == "High");

            var reportContent =
                $"=== التقرير التنفيذي الشهري لاستقرار الكفاءات (SmartHR Enterprise) ===\n" +
                $"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                $"إجمالي الطلبات المسجلة: {totalRequests}\n" +
                $"الطلبات قيد الانتظار: {pendingRequests}\n" +
                $"الطلبات المعتمدة نهائياً: {approvedRequests}\n" +
                $"البلاغات السرية المسجلة: {anonymousReports}\n" +
                $"الموظفون ذوو الخطورة المرتفعة (AI): {highRiskCount}\n";

            return System.Text.Encoding.UTF8.GetBytes(reportContent);
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
            employeeName ??= HttpContext.Session.GetString("SmartHR_UserName") ?? "طارق الزعبي";

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
                ? HttpContext.Session.GetString("SmartHR_UserName") ?? "طارق الزعبي"
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