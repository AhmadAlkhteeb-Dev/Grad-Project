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

    public class DashboardController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public DashboardController(IWebHostEnvironment env)
        {
            _env = env;
        }

        private static List<EmployeeRequest> _requests = new List<EmployeeRequest>
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
                Details = "إجازة سنوية عائلية مخططة مسبقاً.",
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
                EmployeeName = "موظف مجهول (سري 🔒)",
                Department = "الدعم الفني",
                Type = RequestType.AnonymousWhistleblowing,
                Details = "ضغط عمل مرتفع خلال المناوبات المسائية.",
                CreatedAt = DateTime.Now.AddHours(-12),
                Status = RequestStatus.Pending,
                IsAnonymous = true
            }
        };

        private static List<AuditLogEntry> _auditLogs = new List<AuditLogEntry>
        {
            new AuditLogEntry { Id = 1, Action = "تسجيل دخول النظام", PerformedBy = "System Admin", Timestamp = DateTime.Now.AddMinutes(-45), Details = "دخول ناجح إلى لوحة التحكم المؤسسية" },
            new AuditLogEntry { Id = 2, Action = "اعتماد طلب إجازة", PerformedBy = "المدير المباشر", Timestamp = DateTime.Now.AddDays(-1), Details = "قبول طلب الإجازة #102 للموظفة سارة العلي" },
            new AuditLogEntry { Id = 3, Action = "استلام بلاغ سري", PerformedBy = "نظام الحماية", Timestamp = DateTime.Now.AddHours(-12), Details = "توجيه بلاغ مجهول المصدر إلى صندوق HR" }
        };

        private static List<RecentDecisionItem> _recentDecisions = new List<RecentDecisionItem>
        {
            new RecentDecisionItem(102, "سارة العلي", "مقبول", "تمت الموافقة، نتمنى لك إجازة سعيدة.", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm")),
            new RecentDecisionItem(103, "سارة العلي", "مرفوض", "نعتذر، يوجد سلفة سابقة لم تُستكمل أقساطها بعد.", DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd HH:mm"))
        };

        private static List<SupportTicketItem> _supportTickets = new List<SupportTicketItem>
        {
            new SupportTicketItem("TKT-2041", "بوابة الموظف", "متوسطة", "تأخر ظهور إشعار تعديل تاريخ الإجازة بعد الحفظ.", "قيد المعالجة", "منذ ساعتين"),
            new SupportTicketItem("TKT-2038", "صلاحيات", "عالية", "التحقق من عزل البلاغات السرية عن المدير المباشر.", "مغلقة", "أمس")
        };

        private static int GetNextAuditId() => _auditLogs.Any() ? _auditLogs.Max(a => a.Id) + 1 : 1;

        public IActionResult PublicHome()
        {
            return View();
        }

        public IActionResult Index(string role = "Employee", string employeeName = "سارة العلي")
        {
            var validRoles = new[] { "Employee", "Manager", "HR", "Admin" };
            if (!validRoles.Contains(role)) role = "Employee";

            ViewBag.CurrentRole = role;
            ViewBag.CurrentEmployeeName = employeeName;

            ViewBag.CompanySatisfactionScore = 92;
            ViewBag.IsManagerSatisfied = true;
            ViewBag.ManagerFeedbackSummary = "أداء متميز والتزام عالٍ بمواعيد تسليم المشاريع.";

            bool canViewCompanyAnalytics = (role == "Manager" || role == "HR");
            bool canViewAuditTrail = (role == "Admin");

            ViewBag.CanViewCompanyAnalytics = canViewCompanyAnalytics;
            ViewBag.CanViewAuditTrail = canViewAuditTrail;

            List<EmployeeRequest> visibleRequests;
            if (role == "Employee")
            {
                visibleRequests = _requests
                    .Where(r => !r.IsAnonymous && string.Equals(r.EmployeeName, employeeName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else if (role == "Manager")
            {
                visibleRequests = _requests.Where(r => !r.IsAnonymous).ToList();
            }
            else if (role == "HR")
            {
                visibleRequests = _requests.ToList();
            }
            else
            {
                visibleRequests = new List<EmployeeRequest>();
            }

            ViewBag.AllRequestsForHistory = _requests.Where(r => !r.IsAnonymous).ToList();
            ViewBag.RecentDecisions = _recentDecisions.Take(8).ToList();
            ViewBag.PendingCount = visibleRequests.Count(r => r.Status == RequestStatus.Pending);
            ViewBag.ApprovedCount = visibleRequests.Count(r => r.Status == RequestStatus.Approved);
            ViewBag.RejectedCount = visibleRequests.Count(r => r.Status == RequestStatus.Rejected);
            ViewBag.AnonymousCount = _requests.Count(r => r.IsAnonymous);
            ViewBag.AuditLogs = canViewAuditTrail ? _auditLogs : new List<AuditLogEntry>();
            ViewBag.SupportTickets = _supportTickets;

            return View(visibleRequests);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRequest(EmployeeRequest model, IFormFile? attachmentFile, bool isAnonymous = false)
        {
            model.Id = _requests.Any() ? _requests.Max(r => r.Id) + 1 : 101;
            model.Status = RequestStatus.Pending;
            model.CreatedAt = DateTime.Now;
            model.IsAnonymous = isAnonymous;

            string originalEmployee = string.IsNullOrWhiteSpace(model.EmployeeName) ? "سارة العلي" : model.EmployeeName;

            if (attachmentFile != null && attachmentFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(attachmentFile.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachmentFile.CopyToAsync(stream);
                }

                model.AttachmentPath = "/uploads/" + uniqueFileName;
                model.AttachmentName = attachmentFile.FileName;
            }

            if (isAnonymous)
            {
                model.EmployeeName = "موظف مجهول (سري 🔒)";
                if (string.IsNullOrWhiteSpace(model.Department))
                    model.Department = "قسم محمي";
            }

            _requests.Insert(0, model);

            _auditLogs.Insert(0, new AuditLogEntry
            {
                Id = GetNextAuditId(),
                Action = isAnonymous ? "إرسال بلاغ سري مجهول" : "تقديم طلب جديد",
                PerformedBy = isAnonymous ? "مجهول" : originalEmployee,
                Timestamp = DateTime.Now,
                Details = $"الطلب #{model.Id} - النوع: {model.Type}"
            });

            if (isAnonymous)
                return RedirectToAction("Index", new { role = "HR" });

            return RedirectToAction("Index", new { role = "Employee", employeeName = originalEmployee });
        }

        [HttpPost]
        public IActionResult EditRequest(int id, string details, DateTime? startDate, DateTime? endDate, string? exitTime, decimal? advanceAmount, decimal? currentSalary, decimal? requestedRaiseAmount)
        {
            var req = _requests.FirstOrDefault(r => r.Id == id);
            if (req != null && req.Status == RequestStatus.Pending)
            {
                req.Details = details;
                req.StartDate = startDate;
                req.EndDate = endDate;
                req.ExitTime = exitTime;
                req.AdvanceAmount = advanceAmount;
                req.CurrentSalary = currentSalary;
                req.RequestedRaiseAmount = requestedRaiseAmount;

                _auditLogs.Insert(0, new AuditLogEntry
                {
                    Id = GetNextAuditId(),
                    Action = "تعديل طلب معلق",
                    PerformedBy = req.EmployeeName,
                    Timestamp = DateTime.Now,
                    Details = $"تعديل بيانات الطلب #{id}"
                });
            }
            return RedirectToAction("Index", new { role = "Employee", employeeName = req?.EmployeeName ?? "سارة العلي" });
        }

        [HttpPost]
        public IActionResult CancelRequest(int id)
        {
            var req = _requests.FirstOrDefault(r => r.Id == id);
            if (req != null && req.Status == RequestStatus.Pending)
            {
                req.Status = RequestStatus.Cancelled;
                req.ManagerNote = "تم إلغاء الطلب من قِبل الموظف.";

                _auditLogs.Insert(0, new AuditLogEntry
                {
                    Id = GetNextAuditId(),
                    Action = "إلغاء طلب من الموظف",
                    PerformedBy = req.EmployeeName,
                    Timestamp = DateTime.Now,
                    Details = $"إلغاء الطلب #{id}"
                });
            }
            return RedirectToAction("Index", new { role = "Employee", employeeName = req?.EmployeeName ?? "سارة العلي" });
        }

        [HttpPost]
        public IActionResult DecideRequest(int id, bool isApproved, string? managerNote)
        {
            var req = _requests.FirstOrDefault(r => r.Id == id && !r.IsAnonymous);
            if (req == null)
            {
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? Json(new { success = false, message = "الطلب غير موجود." })
                    : RedirectToAction("Index", new { role = "Manager" });
            }

            if (!isApproved && string.IsNullOrWhiteSpace(managerNote))
            {
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? Json(new { success = false, message = "ملاحظة القرار إجبارية عند رفض الطلب." })
                    : RedirectToAction("Index", new { role = "Manager" });
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
                Id = GetNextAuditId(),
                Action = isApproved ? "قبول طلب إداري" : "رفض طلب إداري",
                PerformedBy = "المدير المباشر",
                Timestamp = DateTime.Now,
                Details = $"معرف الطلب: #{id} - الحالة: {req.Status}"
            });

            var managerVisible = _requests.Where(r => !r.IsAnonymous).ToList();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
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
                });
            }

            return RedirectToAction("Index", new { role = "Manager" });
        }

        [HttpPost]
        public IActionResult BulkApprove([FromBody] BulkApproveRequest payload)
        {
            if (payload?.Ids == null || !payload.Ids.Any())
                return Json(new { success = false, message = "يرجى تحديد طلب واحد على الأقل." });

            string note = string.IsNullOrWhiteSpace(payload.Note)
                ? "تمت الموافقة الجماعية من المدير المباشر."
                : payload.Note.Trim();

            var updated = new List<object>();
            string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

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
                        Id = GetNextAuditId(),
                        Action = "موافقة جماعية",
                        PerformedBy = "المدير المباشر",
                        Timestamp = DateTime.Now,
                        Details = $"اعتماد الطلب #{req.Id} ضمن دفعة"
                    });

                    updated.Add(new { id = req.Id, employeeName = req.EmployeeName, note, timeText = timeNow });
                }
            }

            var managerVisible = _requests.Where(r => !r.IsAnonymous).ToList();

            return Json(new
            {
                success = true,
                count = updated.Count,
                updated,
                pendingCount = managerVisible.Count(r => r.Status == RequestStatus.Pending),
                approvedCount = managerVisible.Count(r => r.Status == RequestStatus.Approved),
                rejectedCount = managerVisible.Count(r => r.Status == RequestStatus.Rejected)
            });
        }

        [HttpPost]
        public IActionResult SubmitSupportTicket(string category, string priority, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return Json(new { success = false, message = "يرجى كتابة وصف المشكلة." });

            string ticketNo = "TKT-" + (2042 + _supportTickets.Count);
            string cat = string.IsNullOrWhiteSpace(category) ? "عطل فني" : category.Trim();
            string prio = string.IsNullOrWhiteSpace(priority) ? "متوسطة" : priority.Trim();
            string timeNow = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            var item = new SupportTicketItem(ticketNo, cat, prio, description.Trim(), "مفتوحة", timeNow);
            _supportTickets.Insert(0, item);

            string performedBy = Request.Query["employeeName"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(performedBy))
                performedBy = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(performedBy))
                performedBy = "مستخدم النظام";

            _auditLogs.Insert(0, new AuditLogEntry
            {
                Id = GetNextAuditId(),
                Action = "فتح تذكرة دعم فني",
                PerformedBy = performedBy,
                Timestamp = DateTime.Now,
                Details = $"{ticketNo} - {cat} ({prio})"
            });

            return Json(new { success = true, ticket = item });
        }

        [HttpGet]
        public IActionResult ExportExecutiveReport()
        {
            var reportContent =
                "=== التقرير التنفيذي الشهري لاستقرار الكفاءات (SmartHR Enterprise) ===\n" +
                $"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}\n" +
                $"إجمالي الطلبات المسجلة: {_requests.Count}\n" +
                $"الطلبات قيد الانتظار: {_requests.Count(r => r.Status == RequestStatus.Pending)}\n" +
                $"الطلبات المعتمدة: {_requests.Count(r => r.Status == RequestStatus.Approved)}\n" +
                $"البلاغات السرية المسجلة: {_requests.Count(r => r.IsAnonymous)}\n";

            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(reportContent);
            return File(fileBytes, "text/plain; charset=utf-8", $"Executive_HR_Report_{DateTime.Now:yyyyMMdd}.txt");
        }
    }
}