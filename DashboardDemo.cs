using System;

namespace RestaurantManagementSystem.ConsoleDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Restaurant Management Dashboard Demo");
            Console.WriteLine("====================================");
            Console.WriteLine();
            
            var dashboardData = GetSampleDashboardData();
            
            Console.WriteLine($"Today's Sales: ${dashboardData.TodaySales:N2}");
            Console.WriteLine($"Today's Orders: {dashboardData.TodayOrders}");
            Console.WriteLine($"Active Tables: {dashboardData.ActiveTables}");
            Console.WriteLine($"Upcoming Reservations: {dashboardData.UpcomingReservations}");
            
            Console.WriteLine();
            Console.WriteLine("Recent Orders:");
            foreach (var order in dashboardData.RecentOrders)
            {
                Console.WriteLine($"  Order #{order.OrderId}: {order.CustomerName}, Table {order.TableNumber}, ${order.TotalAmount:N2}, Status: {order.Status}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Low Inventory Items:");
            foreach (var item in dashboardData.LowInventoryItems)
            {
                Console.WriteLine($"  {item.Name}: {item.CurrentStock} {item.Unit} (Min: {item.MinimumStock} {item.Unit})");
            }
            
            Console.WriteLine();
            Console.WriteLine("Popular Menu Items:");
            foreach (var item in dashboardData.PopularMenuItems)
            {
                Console.WriteLine($"  {item.Name}: {item.OrderCount} orders");
            }
        }
        
        static DashboardViewModel GetSampleDashboardData()
        {
            return new DashboardViewModel
            {
                TodaySales = 4890.50m,
                TodayOrders = 78,
                ActiveTables = 12,
                UpcomingReservations = 24,
                RecentOrders = new List<DashboardOrderViewModel>
                {
                    new DashboardOrderViewModel { OrderId = 1001, CustomerName = "John Smith", TableNumber = 5, TotalAmount = 78.50m, Status = "Completed", OrderTime = "7:30 PM" },
                    new DashboardOrderViewModel { OrderId = 1002, CustomerName = "Mary Johnson", TableNumber = 8, TotalAmount = 124.00m, Status = "In Progress", OrderTime = "7:45 PM" },
                    new DashboardOrderViewModel { OrderId = 1003, CustomerName = "Walk-in", TableNumber = 3, TotalAmount = 56.75m, Status = "Completed", OrderTime = "8:00 PM" },
                    new DashboardOrderViewModel { OrderId = 1004, CustomerName = "Robert Wilson", TableNumber = 2, TotalAmount = 95.20m, Status = "Completed", OrderTime = "8:15 PM" },
                    new DashboardOrderViewModel { OrderId = 1005, CustomerName = "Lisa Garcia", TableNumber = 10, TotalAmount = 145.60m, Status = "In Progress", OrderTime = "8:30 PM" }
                },
                LowInventoryItems = new List<InventoryItemViewModel>
                {
                    new InventoryItemViewModel { Name = "Tomatoes", CurrentStock = 5, MinimumStock = 10, Unit = "kg" },
                    new InventoryItemViewModel { Name = "Chicken", CurrentStock = 3, MinimumStock = 5, Unit = "kg" },
                    new InventoryItemViewModel { Name = "Basil", CurrentStock = 0.5m, MinimumStock = 1, Unit = "kg" },
                    new InventoryItemViewModel { Name = "Olive Oil", CurrentStock = 1, MinimumStock = 2, Unit = "liters" },
                    new InventoryItemViewModel { Name = "Parmesan", CurrentStock = 0.8m, MinimumStock = 1, Unit = "kg" }
                },
                PopularMenuItems = new List<MenuItemPopularityViewModel>
                {
                    new MenuItemPopularityViewModel { Name = "Butter Chicken", OrderCount = 25 },
                    new MenuItemPopularityViewModel { Name = "Pasta Alfredo", OrderCount = 20 },
                    new MenuItemPopularityViewModel { Name = "Pizza Margherita", OrderCount = 18 },
                    new MenuItemPopularityViewModel { Name = "Chicken Burrito", OrderCount = 15 },
                    new MenuItemPopularityViewModel { Name = "Fried Rice", OrderCount = 12 }
                },
                SalesData = new List<SalesDataViewModel>
                {
                    new SalesDataViewModel { Day = "Monday", Amount = 3200 },
                    new SalesDataViewModel { Day = "Tuesday", Amount = 2800 },
                    new SalesDataViewModel { Day = "Wednesday", Amount = 4100 },
                    new SalesDataViewModel { Day = "Thursday", Amount = 3600 },
                    new SalesDataViewModel { Day = "Friday", Amount = 4800 },
                    new SalesDataViewModel { Day = "Saturday", Amount = 5200 },
                    new SalesDataViewModel { Day = "Sunday", Amount = 4890 }
                },
                CustomersByTime = new List<CustomersByTimeViewModel>
                {
                    new CustomersByTimeViewModel { Hour = 11, CustomerCount = 15 },
                    new CustomersByTimeViewModel { Hour = 12, CustomerCount = 28 },
                    new CustomersByTimeViewModel { Hour = 13, CustomerCount = 32 },
                    new CustomersByTimeViewModel { Hour = 14, CustomerCount = 18 },
                    new CustomersByTimeViewModel { Hour = 15, CustomerCount = 12 },
                    new CustomersByTimeViewModel { Hour = 17, CustomerCount = 22 },
                    new CustomersByTimeViewModel { Hour = 18, CustomerCount = 38 },
                    new CustomersByTimeViewModel { Hour = 19, CustomerCount = 45 },
                    new CustomersByTimeViewModel { Hour = 20, CustomerCount = 36 },
                    new CustomersByTimeViewModel { Hour = 21, CustomerCount = 24 }
                }
            };
        }
    }
    
    // Simplified ViewModels for the demo
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
    }
}
