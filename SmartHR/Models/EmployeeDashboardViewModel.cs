namespace SmartHR_Platform.Models
{
    public class EmployeeDashboardViewModel
    {
        public string EmployeeName { get; set; } = "سارة العلي";
        public string Department { get; set; } = "تطوير البرمجيات";
        public decimal Salary { get; set; } // موظف عادي لا يرى سوى راتبه فقط
        public List<string> MyRequests { get; set; } = new();
    }
}