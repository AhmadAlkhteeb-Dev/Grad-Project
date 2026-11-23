namespace SmartHR_Platform.Models
{
    public class HrDashboardViewModel
    {
        public int TotalEmployees { get; set; } = 240;
        public int CriticalAtRisk { get; set; } = 27; // الكفاءات المهددة
        public decimal TotalTurnoverCost { get; set; } = 3000000;
        public double CompanyStabilityIndex { get; set; } = 88.0;
    }
}