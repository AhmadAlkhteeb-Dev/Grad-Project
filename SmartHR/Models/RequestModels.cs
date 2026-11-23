using System;

namespace SmartHR_Platform.Models
{
    public enum RequestType
    {
        AnnualLeave,              // طلب إجازة
        SalaryAdvance,            // طلب سلفة
        ExitPermission,           // إذن خروج
        SalaryRaise,              // طلب زيادة راتب
        BranchTransfer,           // نقل فرع
        Complaint,                // ملاحظة إدارية
        AnonymousWhistleblowing,  // بلاغ سري
        Resignation               // استقالة
    }

    public enum RequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class EmployeeRequest
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public RequestType Type { get; set; }
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public string? ManagerNote { get; set; }
        public bool IsAnonymous { get; set; }

        // حقول الإجازة وإذن الخروج والسلفة والمرفقات وزيادة الراتب
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ExitTime { get; set; }
        public decimal? AdvanceAmount { get; set; }
        public decimal? CurrentSalary { get; set; }
        public decimal? RequestedRaiseAmount { get; set; }
        public string? AttachmentPath { get; set; }
        public string? AttachmentName { get; set; }
    }

    public class AuditLogEntry
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Details { get; set; } = string.Empty;
    }
}