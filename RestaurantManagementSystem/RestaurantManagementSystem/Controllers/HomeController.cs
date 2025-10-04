using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RestaurantManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagementSystem.Controllers
{
    [AuthorizeAttribute]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            // Get current user information
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.FindFirstValue(ClaimTypes.Name);
            var userFirstName = User.FindFirstValue(ClaimTypes.GivenName) ?? "User";
            var userLastName = User.FindFirstValue(ClaimTypes.Surname) ?? "";
            var userFullName = $"{userFirstName} {userLastName}".Trim();
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            
            // Get user's permissions
            var userPermissions = User.FindAll("Permission").Select(c => c.Value).ToList();
            
            // Get live dashboard data from database
            var dashboardStats = await GetDashboardStatsAsync();
            var recentOrders = await GetRecentOrdersAsync();
            
            // Create a dashboard view model with live data
            var model = new DashboardViewModel
            {
                UserName = userName,
                UserFullName = userFullName,
                UserEmail = userEmail,
                UserRoles = userRoles,
                UserPermissions = userPermissions,
                LastLoginDate = DateTime.Now, // In a real app, get this from the database
                TodaySales = dashboardStats.TodaySales,
                TodayOrders = dashboardStats.TodayOrders,
                ActiveTables = dashboardStats.ActiveTables,
                UpcomingReservations = dashboardStats.UpcomingReservations,
                RecentOrders = recentOrders,
                LowInventoryItems = new List<InventoryItemViewModel>
                {
                    new InventoryItemViewModel { Name = "Fresh Tomatoes", CurrentStock = 2.5m, MinimumStock = 5.0m, Unit = "kg" },
                    new InventoryItemViewModel { Name = "Olive Oil", CurrentStock = 1.0m, MinimumStock = 2.0m, Unit = "L" }
                },
                PopularMenuItems = new List<MenuItemPopularityViewModel>
                {
                    new MenuItemPopularityViewModel { Name = "Margherita Pizza", OrderCount = 32 },
                    new MenuItemPopularityViewModel { Name = "Chicken Parmesan", OrderCount = 28 },
                    new MenuItemPopularityViewModel { Name = "Caesar Salad", OrderCount = 24 },
                    new MenuItemPopularityViewModel { Name = "Tiramisu", OrderCount = 18 }
                },
                SalesData = new List<SalesDataViewModel>
                {
                    new SalesDataViewModel { Day = "Monday", Amount = 850.00m },
                    new SalesDataViewModel { Day = "Tuesday", Amount = 920.50m },
                    new SalesDataViewModel { Day = "Wednesday", Amount = 1100.25m },
                    new SalesDataViewModel { Day = "Thursday", Amount = 980.75m },
                    new SalesDataViewModel { Day = "Friday", Amount = 1450.00m },
                    new SalesDataViewModel { Day = "Saturday", Amount = 1750.50m },
                    new SalesDataViewModel { Day = "Sunday", Amount = 1200.25m }
                },
                CustomersByTime = new List<CustomersByTimeViewModel>
                {
                    new CustomersByTimeViewModel { Hour = 11, CustomerCount = 15 },
                    new CustomersByTimeViewModel { Hour = 12, CustomerCount = 25 },
                    new CustomersByTimeViewModel { Hour = 13, CustomerCount = 30 },
                    new CustomersByTimeViewModel { Hour = 14, CustomerCount = 20 },
                    new CustomersByTimeViewModel { Hour = 18, CustomerCount = 35 },
                    new CustomersByTimeViewModel { Hour = 19, CustomerCount = 40 },
                    new CustomersByTimeViewModel { Hour = 20, CustomerCount = 30 }
                }
            };
            
            return View(model);
        }

        private async Task<(decimal TodaySales, int TodayOrders, int ActiveTables, int UpcomingReservations)> GetDashboardStatsAsync()
        {
            // Prefer stored procedure for plan reuse and centralized logic. Using usp_GetHomeDashboardStatsEnhanced if present else fallback.
            var procedureNamePrimary = "usp_GetHomeDashboardStatsEnhanced";
            var procedureNameFallback = "usp_GetHomeDashboardStats";
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Try enhanced first
                foreach (var proc in new [] { procedureNamePrimary, procedureNameFallback })
                {
                    try
                    {
                        using var command = new SqlCommand(proc, connection) { CommandType = System.Data.CommandType.StoredProcedure };
                        using var reader = await command.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            // Enhanced version returns extra columns; we only map required if they exist.
                            decimal todaySales = SafeGetDecimal(reader, "TodaySales");
                            int todayOrders = SafeGetInt(reader, "TodayOrders");
                            int activeTables = SafeGetInt(reader, "ActiveTables");
                            int upcomingRes = SafeGetInt(reader, "UpcomingReservations");
                            return (todaySales, todayOrders, activeTables, upcomingRes);
                        }
                    }
                    catch (SqlException sqlEx)
                    {
                        _logger?.LogWarning(sqlEx, "Stored procedure {Proc} failed, will attempt fallback if available", proc);
                        // continue to next proc in sequence
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting dashboard stats via stored procedure");
            }
            return (0m, 0, 0, 0);
        }

        private static decimal SafeGetDecimal(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
        }

        private static int SafeGetInt(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }

        private async Task<List<DashboardOrderViewModel>> GetRecentOrdersAsync()
        {
            var orders = new List<DashboardOrderViewModel>();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("usp_GetRecentOrdersForDashboard", connection))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@OrderCount", 5);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                orders.Add(new DashboardOrderViewModel
                                {
                                    OrderId = reader.GetInt32("OrderId"),
                                    OrderNumber = reader.GetString("OrderNumber"),
                                    CustomerName = reader.GetString("CustomerName"),
                                    TableNumber = reader.GetString("TableNumber"),
                                    TotalAmount = reader.GetDecimal("TotalAmount"),
                                    Status = reader.GetString("Status"),
                                    OrderTime = reader.GetString("OrderTime")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting recent orders for dashboard");
                // Return empty list on error - no fallback data
            }
            
            return orders;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCacheAttribute(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
