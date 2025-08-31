using System;
using System.Collections.Generic;

namespace RestaurantManagementSystem.Models
{
    public class DashboardViewModel
    {
        public decimal TodaySales { get; set; }
        public int TodayOrders { get; set; }
        public int ActiveTables { get; set; }
        public int UpcomingReservations { get; set; }
        public List<DashboardOrderViewModel> RecentOrders { get; set; } = new List<DashboardOrderViewModel>();
        public List<InventoryItemViewModel> LowInventoryItems { get; set; } = new List<InventoryItemViewModel>();
        public List<MenuItemPopularityViewModel> PopularMenuItems { get; set; } = new List<MenuItemPopularityViewModel>();
        public List<SalesDataViewModel> SalesData { get; set; } = new List<SalesDataViewModel>();
        public List<CustomersByTimeViewModel> CustomersByTime { get; set; } = new List<CustomersByTimeViewModel>();
    }

    public class DashboardOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TableNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OrderTime { get; set; } = string.Empty;
    }

    public class InventoryItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class MenuItemPopularityViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int OrderCount { get; set; }
    }

    public class SalesDataViewModel
    {
        public string Day { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CustomersByTimeViewModel
    {
        public int Hour { get; set; }
        public int CustomerCount { get; set; }
        
        public string TimeDisplay => $"{Hour} {(Hour < 12 ? "AM" : "PM")}";
    }
}
