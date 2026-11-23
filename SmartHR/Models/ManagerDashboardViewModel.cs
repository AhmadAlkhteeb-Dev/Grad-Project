namespace SmartHR_Platform.Models
{
    public class ManagerDashboardViewModel
    {
        public string ManagerName { get; set; } = "م. خالد العمري";
        public int TeamSize { get; set; } = 12;
        public List<string> PendingTeamRequests { get; set; } = new(); // طلبات فريقه فقط
    }
}