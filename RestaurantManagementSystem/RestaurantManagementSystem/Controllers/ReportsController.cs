using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using RestaurantManagementSystem.Models;
using System.Data;

namespace RestaurantManagementSystem.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ReportsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpGet]
        public async Task<IActionResult> Sales()
        {
            ViewData["Title"] = "Sales Reports";
            
            var viewModel = new SalesReportViewModel();
            
            // Load available users for the filter dropdown
            await LoadAvailableUsersAsync(viewModel);
            
            // Load default report (last 30 days)
            await LoadSalesReportDataAsync(viewModel);
            
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Sales(SalesReportFilter filter)
        {
            ViewData["Title"] = "Sales Reports";
            
            var viewModel = new SalesReportViewModel
            {
                Filter = filter
            };
            
            // Load available users for the filter dropdown
            await LoadAvailableUsersAsync(viewModel);
            
            // Load report data based on filter
            await LoadSalesReportDataAsync(viewModel);
            
            return View(viewModel);
        }

        public IActionResult Orders()
        {
            ViewData["Title"] = "Order Reports";
            return View();
        }

        public IActionResult Menu()
        {
            ViewData["Title"] = "Menu Analysis";
            return View();
        }

        public IActionResult Customers()
        {
            ViewData["Title"] = "Customer Reports";
            return View();
        }

        public IActionResult Financial()
        {
            ViewData["Title"] = "Financial Summary";
            return View();
        }

        private async Task LoadAvailableUsersAsync(SalesReportViewModel viewModel)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand(@"
                    SELECT DISTINCT u.Id, u.FirstName, u.LastName, u.Username 
                    FROM Users u 
                    INNER JOIN Orders o ON u.Id = o.UserId 
                    WHERE u.FirstName IS NOT NULL AND u.LastName IS NOT NULL
                    ORDER BY u.FirstName, u.LastName", connection);
                
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    viewModel.AvailableUsers.Add(new UserSelectItem
                    {
                        Id = reader.GetInt32("Id"),
                        Name = $"{reader.GetString("FirstName")} {reader.GetString("LastName")}",
                        Username = reader.GetString("Username")
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error and continue with empty user list
                Console.WriteLine($"Error loading users: {ex.Message}");
            }
        }

        private async Task LoadSalesReportDataAsync(SalesReportViewModel viewModel)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                
                using var command = new SqlCommand("usp_GetSalesReport", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                
                // Add parameters
                command.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.Date) 
                { 
                    Value = viewModel.Filter.StartDate?.Date ?? DateTime.Today.AddDays(-30) 
                });
                command.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.Date) 
                { 
                    Value = viewModel.Filter.EndDate?.Date ?? DateTime.Today 
                });
                command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int) 
                { 
                    Value = viewModel.Filter.UserId.HasValue ? viewModel.Filter.UserId.Value : DBNull.Value 
                });
                
                using var reader = await command.ExecuteReaderAsync();
                
                // Read Summary Statistics (First Result Set)
                if (await reader.ReadAsync())
                {
                    viewModel.Summary = new SalesReportSummary
                    {
                        TotalOrders = reader.GetInt32("TotalOrders"),
                        TotalSales = reader.GetDecimal("TotalSales"),
                        AverageOrderValue = reader.GetDecimal("AverageOrderValue"),
                        TotalSubtotal = reader.GetDecimal("TotalSubtotal"),
                        TotalTax = reader.GetDecimal("TotalTax"),
                        TotalTips = reader.GetDecimal("TotalTips"),
                        TotalDiscounts = reader.GetDecimal("TotalDiscounts"),
                        CompletedOrders = reader.GetInt32("CompletedOrders"),
                        CancelledOrders = reader.GetInt32("CancelledOrders")
                    };
                }
                
                // Read Daily Sales (Second Result Set)
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        viewModel.DailySales.Add(new DailySalesData
                        {
                            SalesDate = reader.GetDateTime("SalesDate"),
                            OrderCount = reader.GetInt32("OrderCount"),
                            DailySales = reader.GetDecimal("DailySales"),
                            AvgOrderValue = reader.GetDecimal("AvgOrderValue")
                        });
                    }
                }
                
                // Read Top Menu Items (Third Result Set)
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        viewModel.TopMenuItems.Add(new TopMenuItem
                        {
                            ItemName = reader.GetString("ItemName"),
                            MenuItemId = reader.GetInt32("MenuItemId"),
                            TotalQuantitySold = reader.GetInt32("TotalQuantitySold"),
                            TotalRevenue = reader.GetDecimal("TotalRevenue"),
                            AveragePrice = reader.GetDecimal("AveragePrice"),
                            OrderCount = reader.GetInt32("OrderCount")
                        });
                    }
                }
                
                // Read Server Performance (Fourth Result Set)
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        viewModel.ServerPerformance.Add(new ServerPerformance
                        {
                            ServerName = reader.GetString("ServerName"),
                            Username = reader.GetString("Username"),
                            UserId = reader.IsDBNull("UserId") ? null : reader.GetInt32("UserId"),
                            OrderCount = reader.GetInt32("OrderCount"),
                            TotalSales = reader.GetDecimal("TotalSales"),
                            AvgOrderValue = reader.GetDecimal("AvgOrderValue"),
                            TotalTips = reader.GetDecimal("TotalTips")
                        });
                    }
                }
                
                // Read Order Status Data (Fifth Result Set)
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        viewModel.OrderStatusData.Add(new OrderStatusData
                        {
                            OrderStatus = reader.GetString("OrderStatus"),
                            OrderCount = reader.GetInt32("OrderCount"),
                            TotalAmount = reader.GetDecimal("TotalAmount"),
                            Percentage = reader.GetDecimal("Percentage")
                        });
                    }
                }
                
                // Read Hourly Sales Pattern (Sixth Result Set)
                if (await reader.NextResultAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        viewModel.HourlySalesPattern.Add(new HourlySalesData
                        {
                            HourOfDay = reader.GetInt32("HourOfDay"),
                            OrderCount = reader.GetInt32("OrderCount"),
                            HourlySales = reader.GetDecimal("HourlySales"),
                            AvgOrderValue = reader.GetDecimal("AvgOrderValue")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and provide default empty data
                Console.WriteLine($"Error loading sales report data: {ex.Message}");
                viewModel.Summary = new SalesReportSummary();
            }
        }
    }
}