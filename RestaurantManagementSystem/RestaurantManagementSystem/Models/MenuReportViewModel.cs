using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class MenuReportViewModel
    {
        public MenuReportFilter Filter { get; set; } = new MenuReportFilter();
        public MenuSummary Summary { get; set; } = new MenuSummary();
        public List<MenuTopItem> TopItems { get; set; } = new List<MenuTopItem>();
        public List<CategoryPerformance> CategoryPerformance { get; set; } = new List<CategoryPerformance>();
        public List<SeasonalTrend> SeasonalTrends { get; set; } = new List<SeasonalTrend>();
        public List<MenuRecommendation> Recommendations { get; set; } = new List<MenuRecommendation>();
    }

    public class MenuReportFilter
    {
        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? From { get; set; } = DateTime.Today.AddMonths(-1);

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? To { get; set; } = DateTime.Today;
    }

    public class MenuSummary
    {
        public int TotalItemsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal OverallGP { get; set; }
    }

    public class MenuTopItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal GP => Revenue > 0 ? (Profit * 100 / Revenue) : 0;
    }

    public class CategoryPerformance
    {
        public string Category { get; set; } = "";
        public int ItemsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageGP { get; set; }
    }

    public class SeasonalTrend
    {
        public string Period { get; set; } = ""; // e.g., Week, Month
        public int ItemsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class MenuRecommendation
    {
        public string Recommendation { get; set; } = "";
        public string Rationale { get; set; } = "";
    }
}
