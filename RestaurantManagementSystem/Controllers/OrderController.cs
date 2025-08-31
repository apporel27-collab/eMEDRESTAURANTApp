using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagementSystem.Controllers
{
    public class OrderController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        
        public OrderController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }
        
        // Order Dashboard
        public IActionResult Dashboard()
        {
            var model = GetOrderDashboard();
            return View(model);
        }
        
        // Create New Order
        public IActionResult Create()
        {
            var model = new CreateOrderViewModel();
            
            // Get available tables
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get available tables
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, TableName, Capacity, Status
                    FROM Tables
                    WHERE Status = 0
                    ORDER BY TableName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailableTables.Add(new TableViewModel
                            {
                                Id = reader.GetInt32(0),
                                TableName = reader.GetString(1),
                                Capacity = reader.GetInt32(2),
                                Status = reader.GetInt32(3),
                                StatusDisplay = "Available"
                            });
                        }
                    }
                }
                
                // Get occupied tables with turnover info
                using (SqlCommand command = new SqlCommand(@"
                    SELECT tt.Id, t.Id, t.TableName, tt.GuestName, tt.PartySize, tt.Status
                    FROM TableTurnovers tt
                    INNER JOIN Tables t ON tt.TableId = t.Id
                    WHERE tt.Status < 5 -- Not departed
                    ORDER BY t.TableName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.OccupiedTables.Add(new ActiveTableViewModel
                            {
                                TurnoverId = reader.GetInt32(0),
                                TableId = reader.GetInt32(1),
                                TableName = reader.GetString(2),
                                GuestName = reader.GetString(3),
                                PartySize = reader.GetInt32(4),
                                Status = reader.GetInt32(5)
                            });
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateOrderViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (SqlCommand command = new SqlCommand("usp_CreateOrder", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            
                            command.Parameters.AddWithValue("@TableTurnoverId", model.TableTurnoverId ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@OrderType", model.OrderType);
                            command.Parameters.AddWithValue("@UserId", GetCurrentUserId());
                            command.Parameters.AddWithValue("@CustomerName", string.IsNullOrEmpty(model.CustomerName) ? (object)DBNull.Value : model.CustomerName);
                            command.Parameters.AddWithValue("@CustomerPhone", string.IsNullOrEmpty(model.CustomerPhone) ? (object)DBNull.Value : model.CustomerPhone);
                            command.Parameters.AddWithValue("@SpecialInstructions", string.IsNullOrEmpty(model.SpecialInstructions) ? (object)DBNull.Value : model.SpecialInstructions);
                            
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int orderId = reader.GetInt32(0);
                                    string orderNumber = reader.GetString(1);
                                    string message = reader.GetString(2);
                                    
                                    if (orderId > 0)
                                    {
                                        TempData["SuccessMessage"] = $"Order {orderNumber} created successfully.";
                                        return RedirectToAction("Details", new { id = orderId });
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("", message);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("", "Failed to create order.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            
            // If we get here, something went wrong - repopulate the model
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get available tables
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, TableName, Capacity, Status
                    FROM Tables
                    WHERE Status = 0
                    ORDER BY TableName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailableTables.Add(new TableViewModel
                            {
                                Id = reader.GetInt32(0),
                                TableName = reader.GetString(1),
                                Capacity = reader.GetInt32(2),
                                Status = reader.GetInt32(3),
                                StatusDisplay = "Available"
                            });
                        }
                    }
                }
                
                // Get occupied tables with turnover info
                using (SqlCommand command = new SqlCommand(@"
                    SELECT tt.Id, t.Id, t.TableName, tt.GuestName, tt.PartySize, tt.Status
                    FROM TableTurnovers tt
                    INNER JOIN Tables t ON tt.TableId = t.Id
                    WHERE tt.Status < 5 -- Not departed
                    ORDER BY t.TableName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.OccupiedTables.Add(new ActiveTableViewModel
                            {
                                TurnoverId = reader.GetInt32(0),
                                TableId = reader.GetInt32(1),
                                TableName = reader.GetString(2),
                                GuestName = reader.GetString(3),
                                PartySize = reader.GetInt32(4),
                                Status = reader.GetInt32(5)
                            });
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        // Order Details
        public IActionResult Details(int id)
        {
            var model = GetOrderDetails(id);
            
            if (model == null)
            {
                return NotFound();
            }
            
            return View(model);
        }
        
        // Add Item to Order
        public IActionResult AddItem(int orderId, int? menuItemId = null)
        {
            var model = new AddOrderItemViewModel
            {
                OrderId = orderId
            };
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details
                using (SqlCommand command = new SqlCommand(@"
                    SELECT o.OrderNumber, ISNULL(t.TableName, 'N/A') AS TableNumber
                    FROM Orders o
                    LEFT JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
                    LEFT JOIN Tables t ON tt.TableId = t.Id
                    WHERE o.Id = @OrderId", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OrderNumber = reader.GetString(0);
                            model.TableNumber = reader.GetString(1);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
                
                // Get available courses
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name
                    FROM CourseTypes
                    ORDER BY DisplayOrder", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailableCourses.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }
                
                // Get current order items for the order summary
                using (SqlCommand command = new SqlCommand(@"
                    SELECT oi.Id, oi.MenuItemId, oi.Quantity, oi.UnitPrice, oi.Subtotal, 
                           oi.SpecialInstructions, mi.Name
                    FROM OrderItems oi
                    INNER JOIN MenuItems mi ON oi.MenuItemId = mi.Id
                    WHERE oi.OrderId = @OrderId AND oi.Status < 5 -- Not cancelled
                    ORDER BY oi.CreatedAt DESC", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.CurrentOrderItems.Add(new OrderItemViewModel
                            {
                                Id = reader.GetInt32(0),
                                MenuItemId = reader.GetInt32(1),
                                Quantity = reader.GetInt32(2),
                                UnitPrice = reader.GetDecimal(3),
                                Subtotal = reader.GetDecimal(4),
                                SpecialInstructions = reader.IsDBNull(5) ? null : reader.GetString(5),
                                MenuItemName = reader.GetString(6),
                                TotalPrice = reader.GetDecimal(4) // Subtotal already includes quantity
                            });
                        }
                    }
                }
                
                // Calculate current order total
                model.CurrentOrderTotal = model.CurrentOrderItems.Sum(i => i.Subtotal);
                
                // If a specific menu item is selected, get its details and modifiers
                if (menuItemId.HasValue)
                {
                    model.MenuItemId = menuItemId.Value;
                    
                    // Get menu item details
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT Id, Name, Description, Price, CategoryId, ImagePath
                        FROM MenuItems
                        WHERE Id = @MenuItemId AND IsAvailable = 1", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId.Value);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.MenuItem = new MenuItem
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Price = reader.GetDecimal(3),
                                    CategoryId = reader.GetInt32(4),
                                    ImagePath = reader.IsDBNull(5) ? null : reader.GetString(5)
                                };
                                
                                // Set properties for the view
                                model.MenuItemName = model.MenuItem.Name;
                                model.MenuItemDescription = model.MenuItem.Description;
                                model.MenuItemPrice = model.MenuItem.Price;
                                model.MenuItemImagePath = model.MenuItem.ImagePath;
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }
                    
                    // Get available modifiers for the menu item
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT m.Id, m.Name, m.Price, m.IsDefault
                        FROM Modifiers m
                        INNER JOIN MenuItem_Modifiers mm ON m.Id = mm.ModifierId
                        WHERE mm.MenuItemId = @MenuItemId
                        ORDER BY m.Name", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId.Value);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var modifier = new ModifierViewModel
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Price = reader.GetDecimal(2),
                                    IsDefault = reader.GetBoolean(3),
                                    IsSelected = reader.GetBoolean(3), // Default selected if IsDefault is true
                                    ModifierId = reader.GetInt32(0)
                                };
                                
                                model.AvailableModifiers.Add(modifier);
                                
                                if (modifier.IsDefault)
                                {
                                    model.SelectedModifiers.Add(modifier.Id);
                                }
                            }
                        }
                    }
                    
                    // Get allergens for the menu item
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT a.Name
                        FROM Allergens a
                        INNER JOIN MenuItem_Allergens ma ON a.Id = ma.AllergenId
                        WHERE ma.MenuItemId = @MenuItemId
                        ORDER BY a.Name", connection))
                    {
                        command.Parameters.AddWithValue("@MenuItemId", menuItemId.Value);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                model.CommonAllergens.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddItem(AddOrderItemViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        // Convert selected modifiers to comma-separated string
                        string modifierIds = model.SelectedModifiers != null && model.SelectedModifiers.Any()
                            ? string.Join(",", model.SelectedModifiers)
                            : null;
                        
                        using (SqlCommand command = new SqlCommand("usp_AddOrderItem", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            
                            command.Parameters.AddWithValue("@OrderId", model.OrderId);
                            command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                            command.Parameters.AddWithValue("@Quantity", model.Quantity);
                            command.Parameters.AddWithValue("@SpecialInstructions", string.IsNullOrEmpty(model.SpecialInstructions) ? (object)DBNull.Value : model.SpecialInstructions);
                            command.Parameters.AddWithValue("@CourseId", model.CourseId.HasValue ? model.CourseId.Value : (object)DBNull.Value);
                            command.Parameters.AddWithValue("@ModifierIds", modifierIds ?? (object)DBNull.Value);
                            
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int orderItemId = reader.GetInt32(0);
                                    string message = reader.GetString(1);
                                    
                                    if (orderItemId > 0)
                                    {
                                        TempData["SuccessMessage"] = "Item added to order successfully.";
                                        return RedirectToAction("Details", new { id = model.OrderId });
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("", message);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("", "Failed to add item to order.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            
            // If we get here, something went wrong - repopulate the model
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get available courses
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name
                    FROM CourseTypes
                    ORDER BY DisplayOrder", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailableCourses.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }
                
                // Get menu item details
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name, Description, Price, CategoryId
                    FROM MenuItems
                    WHERE Id = @MenuItemId AND IsAvailable = 1", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.MenuItem = new MenuItem
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Price = reader.GetDecimal(3),
                                CategoryId = reader.GetInt32(4)
                            };
                        }
                    }
                }
                
                // Get available modifiers for the menu item
                using (SqlCommand command = new SqlCommand(@"
                    SELECT m.Id, m.Name, m.Price, m.IsDefault
                    FROM Modifiers m
                    INNER JOIN MenuItem_Modifiers mm ON m.Id = mm.ModifierId
                    WHERE mm.MenuItemId = @MenuItemId
                    ORDER BY m.Name", connection))
                {
                    command.Parameters.AddWithValue("@MenuItemId", model.MenuItemId);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailableModifiers.Add(new ModifierViewModel
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Price = reader.GetDecimal(2),
                                IsDefault = reader.GetBoolean(3),
                                IsSelected = model.SelectedModifiers?.Contains(reader.GetInt32(0)) ?? false
                            });
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        // Fire Items to Kitchen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult FireItems(FireOrderItemsViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        // Convert selected items to comma-separated string
                        string orderItemIds = null;
                        
                        if (!model.FireAll && model.SelectedItems != null && model.SelectedItems.Any())
                        {
                            orderItemIds = string.Join(",", model.SelectedItems);
                        }
                        
                        using (SqlCommand command = new SqlCommand("usp_FireOrderItems", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            
                            command.Parameters.AddWithValue("@OrderId", model.OrderId);
                            command.Parameters.AddWithValue("@OrderItemIds", orderItemIds ?? (object)DBNull.Value);
                            
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int kitchenTicketId = reader.GetInt32(0);
                                    string ticketNumber = reader.GetString(1);
                                    string message = reader.GetString(2);
                                    
                                    if (kitchenTicketId > 0)
                                    {
                                        TempData["SuccessMessage"] = $"Items fired to kitchen successfully. Ticket #{ticketNumber} created.";
                                    }
                                    else
                                    {
                                        TempData["ErrorMessage"] = message;
                                    }
                                }
                                else
                                {
                                    TempData["ErrorMessage"] = "Failed to fire items to kitchen.";
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                }
            }
            
            return RedirectToAction("Details", new { id = model.OrderId });
        }
        
        // Browse Menu Items
        public IActionResult BrowseMenu(int id)
        {
            var model = new OrderViewModel
            {
                Id = id,
                MenuCategories = new List<MenuCategoryViewModel>()
            };
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details
                using (SqlCommand command = new SqlCommand(@"
                    SELECT o.OrderNumber, ISNULL(t.TableName, 'N/A') AS TableName 
                    FROM Orders o
                    LEFT JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
                    LEFT JOIN Tables t ON tt.TableId = t.Id
                    WHERE o.Id = @OrderId", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OrderNumber = reader.GetString(0);
                            model.TableName = reader.GetString(1);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
                
                // Get all categories
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name
                    FROM Categories
                    ORDER BY Name", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.MenuCategories.Add(new MenuCategoryViewModel
                            {
                                CategoryId = reader.GetInt32(0),
                                CategoryName = reader.GetString(1),
                                MenuItems = new List<MenuItem>()
                            });
                        }
                    }
                }
                
                // Get menu items for each category
                foreach (var category in model.MenuCategories)
                {
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT Id, Name, Description, Price, IsAvailable, ImagePath
                        FROM MenuItems
                        WHERE CategoryId = @CategoryId AND IsAvailable = 1
                        ORDER BY Name", connection))
                    {
                        command.Parameters.AddWithValue("@CategoryId", category.CategoryId);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                category.MenuItems.Add(new MenuItem
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Price = reader.GetDecimal(3),
                                    IsAvailable = reader.GetBoolean(4),
                                    ImagePath = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    CategoryId = category.CategoryId,
                                    CategoryName = category.CategoryName
                                });
                            }
                        }
                    }
                }
                
                // Only keep categories that have menu items
                model.MenuCategories = model.MenuCategories.Where(c => c.MenuItems.Any()).ToList();
            }
            
            return View(model);
        }
        
        // Helper Methods
        private OrderDashboardViewModel GetOrderDashboard()
        {
            var model = new OrderDashboardViewModel
            {
                ActiveOrders = new List<OrderSummary>(),
                CompletedOrders = new List<OrderSummary>()
            };
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order counts and total sales for today
                using (SqlCommand command = new SqlCommand(@"
                    SELECT
                        SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS OpenCount,
                        SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS InProgressCount,
                        SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS ReadyCount,
                        SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS CompletedCount,
                        SUM(CASE WHEN Status = 3 THEN TotalAmount ELSE 0 END) AS TotalSales
                    FROM Orders
                    WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OpenOrdersCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            model.InProgressOrdersCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            model.ReadyOrdersCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            model.CompletedOrdersCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                            model.TotalSales = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4);
                        }
                    }
                }
                
                // Get active orders
                using (SqlCommand command = new SqlCommand(@"
                    SELECT 
                        o.Id,
                        o.OrderNumber,
                        o.OrderType,
                        o.Status,
                        CASE 
                            WHEN o.OrderType = 0 THEN t.TableName 
                            ELSE NULL 
                        END AS TableName,
                        CASE 
                            WHEN o.OrderType = 0 THEN tt.GuestName 
                            ELSE o.CustomerName 
                        END AS GuestName,
                        u.FullName AS ServerName,
                        (SELECT COUNT(1) FROM OrderItems WHERE OrderId = o.Id) AS ItemCount,
                        o.TotalAmount,
                        o.CreatedAt,
                        DATEDIFF(MINUTE, o.CreatedAt, GETDATE()) AS DurationMinutes
                    FROM Orders o
                    LEFT JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
                    LEFT JOIN Tables t ON tt.TableId = t.Id
                    LEFT JOIN Users u ON o.UserId = u.Id
                    WHERE o.Status < 3 -- Not completed
                    AND CAST(o.CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                    ORDER BY o.CreatedAt DESC", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var orderType = reader.GetInt32(2);
                            string orderTypeDisplay = orderType switch
                            {
                                0 => "Dine-In",
                                1 => "Takeout",
                                2 => "Delivery",
                                3 => "Online",
                                _ => "Unknown"
                            };
                            
                            var status = reader.GetInt32(3);
                            string statusDisplay = status switch
                            {
                                0 => "Open",
                                1 => "In Progress",
                                2 => "Ready",
                                3 => "Completed",
                                4 => "Cancelled",
                                _ => "Unknown"
                            };
                            
                            model.ActiveOrders.Add(new OrderSummary
                            {
                                Id = reader.GetInt32(0),
                                OrderNumber = reader.GetString(1),
                                OrderType = orderType,
                                OrderTypeDisplay = orderTypeDisplay,
                                Status = status,
                                StatusDisplay = statusDisplay,
                                TableName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                GuestName = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ServerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                                ItemCount = reader.GetInt32(7),
                                TotalAmount = reader.GetDecimal(8),
                                CreatedAt = reader.GetDateTime(9),
                                Duration = TimeSpan.FromMinutes(reader.GetInt32(10))
                            });
                        }
                    }
                }
                
                // Get completed orders for today
                using (SqlCommand command = new SqlCommand(@"
                    SELECT 
                        o.Id,
                        o.OrderNumber,
                        o.OrderType,
                        o.Status,
                        CASE 
                            WHEN o.OrderType = 0 THEN t.TableName 
                            ELSE NULL 
                        END AS TableName,
                        CASE 
                            WHEN o.OrderType = 0 THEN tt.GuestName 
                            ELSE o.CustomerName 
                        END AS GuestName,
                        u.FullName AS ServerName,
                        (SELECT COUNT(1) FROM OrderItems WHERE OrderId = o.Id) AS ItemCount,
                        o.TotalAmount,
                        o.CreatedAt,
                        DATEDIFF(MINUTE, o.CreatedAt, o.CompletedAt) AS DurationMinutes
                    FROM Orders o
                    LEFT JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
                    LEFT JOIN Tables t ON tt.TableId = t.Id
                    LEFT JOIN Users u ON o.UserId = u.Id
                    WHERE o.Status = 3 -- Completed
                    AND CAST(o.CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                    ORDER BY o.CompletedAt DESC", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var orderType = reader.GetInt32(2);
                            string orderTypeDisplay = orderType switch
                            {
                                0 => "Dine-In",
                                1 => "Takeout",
                                2 => "Delivery",
                                3 => "Online",
                                _ => "Unknown"
                            };
                            
                            model.CompletedOrders.Add(new OrderSummary
                            {
                                Id = reader.GetInt32(0),
                                OrderNumber = reader.GetString(1),
                                OrderType = orderType,
                                OrderTypeDisplay = orderTypeDisplay,
                                Status = 3, // Completed
                                StatusDisplay = "Completed",
                                TableName = reader.IsDBNull(4) ? null : reader.GetString(4),
                                GuestName = reader.IsDBNull(5) ? null : reader.GetString(5),
                                ServerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                                ItemCount = reader.GetInt32(7),
                                TotalAmount = reader.GetDecimal(8),
                                CreatedAt = reader.GetDateTime(9),
                                Duration = TimeSpan.FromMinutes(reader.IsDBNull(10) ? 0 : reader.GetInt32(10))
                            });
                        }
                    }
                }
            }
            
            return model;
        }
        
        private OrderViewModel GetOrderDetails(int id)
        {
            OrderViewModel order = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details
                using (SqlCommand command = new SqlCommand(@"
                    SELECT 
                        o.Id,
                        o.OrderNumber,
                        o.TableTurnoverId,
                        o.OrderType,
                        o.Status,
                        o.UserId,
                        u.FullName AS ServerName,
                        o.CustomerName,
                        o.CustomerPhone,
                        o.Subtotal,
                        o.TaxAmount,
                        o.TipAmount,
                        o.DiscountAmount,
                        o.TotalAmount,
                        o.SpecialInstructions,
                        o.CreatedAt,
                        o.UpdatedAt,
                        o.CompletedAt,
                        CASE 
                            WHEN o.TableTurnoverId IS NOT NULL THEN t.TableName 
                            ELSE NULL 
                        END AS TableName,
                        CASE 
                            WHEN o.TableTurnoverId IS NOT NULL THEN tt.GuestName 
                            ELSE o.CustomerName 
                        END AS GuestName
                    FROM Orders o
                    LEFT JOIN Users u ON o.UserId = u.Id
                    LEFT JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
                    LEFT JOIN Tables t ON tt.TableId = t.Id
                    WHERE o.Id = @OrderId", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var orderType = reader.GetInt32(3);
                            string orderTypeDisplay = orderType switch
                            {
                                0 => "Dine-In",
                                1 => "Takeout",
                                2 => "Delivery",
                                3 => "Online",
                                _ => "Unknown"
                            };
                            
                            var status = reader.GetInt32(4);
                            string statusDisplay = status switch
                            {
                                0 => "Open",
                                1 => "In Progress",
                                2 => "Ready",
                                3 => "Completed",
                                4 => "Cancelled",
                                _ => "Unknown"
                            };
                            
                            order = new OrderViewModel
                            {
                                Id = reader.GetInt32(0),
                                OrderNumber = reader.GetString(1),
                                TableTurnoverId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                                OrderType = orderType,
                                OrderTypeDisplay = orderTypeDisplay,
                                Status = status,
                                StatusDisplay = statusDisplay,
                                ServerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                                CustomerName = reader.IsDBNull(7) ? null : reader.GetString(7),
                                CustomerPhone = reader.IsDBNull(8) ? null : reader.GetString(8),
                                Subtotal = reader.GetDecimal(9),
                                TaxAmount = reader.GetDecimal(10),
                                TipAmount = reader.GetDecimal(11),
                                DiscountAmount = reader.GetDecimal(12),
                                TotalAmount = reader.GetDecimal(13),
                                SpecialInstructions = reader.IsDBNull(14) ? null : reader.GetString(14),
                                CreatedAt = reader.GetDateTime(15),
                                UpdatedAt = reader.GetDateTime(16),
                                CompletedAt = reader.IsDBNull(17) ? null : (DateTime?)reader.GetDateTime(17),
                                TableName = reader.IsDBNull(18) ? null : reader.GetString(18),
                                GuestName = reader.IsDBNull(19) ? null : reader.GetString(19),
                                Items = new List<OrderItemViewModel>(),
                                KitchenTickets = new List<KitchenTicketViewModel>()
                            };
                        }
                        else
                        {
                            return null; // Order not found
                        }
                    }
                }
                
                // Get order items
                using (SqlCommand command = new SqlCommand(@"
                    SELECT 
                        oi.Id,
                        oi.MenuItemId,
                        mi.Name AS MenuItemName,
                        mi.Description AS MenuItemDescription,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.Subtotal,
                        oi.SpecialInstructions,
                        oi.CourseId,
                        ct.Name AS CourseName,
                        oi.Status,
                        oi.FireTime,
                        oi.CompletionTime,
                        oi.DeliveryTime
                    FROM OrderItems oi
                    INNER JOIN MenuItems mi ON oi.MenuItemId = mi.Id
                    LEFT JOIN CourseTypes ct ON oi.CourseId = ct.Id
                    WHERE oi.OrderId = @OrderId
                    ORDER BY 
                        CASE WHEN oi.CourseId IS NULL THEN 999 ELSE oi.CourseId END,
                        oi.CreatedAt", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var status = reader.GetInt32(10);
                            string statusDisplay = status switch
                            {
                                0 => "New",
                                1 => "Fired",
                                2 => "Cooking",
                                3 => "Ready",
                                4 => "Delivered",
                                5 => "Cancelled",
                                _ => "Unknown"
                            };
                            
                            var orderItem = new OrderItemViewModel
                            {
                                Id = reader.GetInt32(0),
                                OrderId = id,
                                MenuItemId = reader.GetInt32(1),
                                MenuItemName = reader.GetString(2),
                                MenuItemDescription = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Quantity = reader.GetInt32(4),
                                UnitPrice = reader.GetDecimal(5),
                                Subtotal = reader.GetDecimal(6),
                                SpecialInstructions = reader.IsDBNull(7) ? null : reader.GetString(7),
                                CourseId = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8),
                                CourseName = reader.IsDBNull(9) ? null : reader.GetString(9),
                                Status = status,
                                StatusDisplay = statusDisplay,
                                FireTime = reader.IsDBNull(11) ? null : (DateTime?)reader.GetDateTime(11),
                                CompletionTime = reader.IsDBNull(12) ? null : (DateTime?)reader.GetDateTime(12),
                                DeliveryTime = reader.IsDBNull(13) ? null : (DateTime?)reader.GetDateTime(13),
                                Modifiers = new List<OrderItemModifierViewModel>()
                            };
                            
                            order.Items.Add(orderItem);
                        }
                    }
                }
                
                // Get order item modifiers
                foreach (var item in order.Items)
                {
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT 
                            oim.Id,
                            oim.ModifierId,
                            m.Name AS ModifierName,
                            oim.Price
                        FROM OrderItemModifiers oim
                        INNER JOIN Modifiers m ON oim.ModifierId = m.Id
                        WHERE oim.OrderItemId = @OrderItemId", connection))
                    {
                        command.Parameters.AddWithValue("@OrderItemId", item.Id);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                item.Modifiers.Add(new OrderItemModifierViewModel
                                {
                                    Id = reader.GetInt32(0),
                                    OrderItemId = item.Id,
                                    ModifierId = reader.GetInt32(1),
                                    ModifierName = reader.GetString(2),
                                    Price = reader.GetDecimal(3)
                                });
                            }
                        }
                    }
                }
                
                // Get kitchen tickets
                using (SqlCommand command = new SqlCommand(@"
                    SELECT 
                        kt.Id,
                        kt.TicketNumber,
                        kt.StationId,
                        kt.Status,
                        kt.CreatedAt,
                        kt.CompletedAt
                    FROM KitchenTickets kt
                    WHERE kt.OrderId = @OrderId
                    ORDER BY kt.CreatedAt DESC", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var status = reader.GetInt32(3);
                            string statusDisplay = status switch
                            {
                                0 => "New",
                                1 => "In Progress",
                                2 => "Ready",
                                3 => "Completed",
                                4 => "Cancelled",
                                _ => "Unknown"
                            };
                            
                            var kitchenTicket = new KitchenTicketViewModel
                            {
                                Id = reader.GetInt32(0),
                                TicketNumber = reader.GetString(1),
                                OrderId = id,
                                OrderNumber = order.OrderNumber,
                                StationId = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                                Status = status,
                                StatusDisplay = statusDisplay,
                                CreatedAt = reader.GetDateTime(4),
                                CompletedAt = reader.IsDBNull(5) ? null : (DateTime?)reader.GetDateTime(5),
                                Items = new List<KitchenTicketItemViewModel>()
                            };
                            
                            order.KitchenTickets.Add(kitchenTicket);
                        }
                    }
                }
                
                // Get kitchen ticket items
                foreach (var ticket in order.KitchenTickets)
                {
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT 
                            kti.Id,
                            kti.OrderItemId,
                            mi.Name AS MenuItemName,
                            oi.Quantity,
                            oi.SpecialInstructions,
                            kti.Status,
                            kti.StartTime,
                            kti.CompletionTime,
                            kti.Notes
                        FROM KitchenTicketItems kti
                        INNER JOIN OrderItems oi ON kti.OrderItemId = oi.Id
                        INNER JOIN MenuItems mi ON oi.MenuItemId = mi.Id
                        WHERE kti.KitchenTicketId = @KitchenTicketId", connection))
                    {
                        command.Parameters.AddWithValue("@KitchenTicketId", ticket.Id);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var status = reader.GetInt32(5);
                                string statusDisplay = status switch
                                {
                                    0 => "New",
                                    1 => "In Progress",
                                    2 => "Ready",
                                    3 => "Completed",
                                    4 => "Cancelled",
                                    _ => "Unknown"
                                };
                                
                                var ticketItem = new KitchenTicketItemViewModel
                                {
                                    Id = reader.GetInt32(0),
                                    KitchenTicketId = ticket.Id,
                                    OrderItemId = reader.GetInt32(1),
                                    MenuItemName = reader.GetString(2),
                                    Quantity = reader.GetInt32(3),
                                    SpecialInstructions = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    Status = status,
                                    StatusDisplay = statusDisplay,
                                    StartTime = reader.IsDBNull(6) ? null : (DateTime?)reader.GetDateTime(6),
                                    CompletionTime = reader.IsDBNull(7) ? null : (DateTime?)reader.GetDateTime(7),
                                    Notes = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    Modifiers = new List<string>()
                                };
                                
                                // Get modifiers for this ticket item
                                using (SqlCommand modifiersCommand = new SqlCommand(@"
                                    SELECT m.Name
                                    FROM OrderItemModifiers oim
                                    INNER JOIN Modifiers m ON oim.ModifierId = m.Id
                                    WHERE oim.OrderItemId = @OrderItemId", connection))
                                {
                                    modifiersCommand.Parameters.AddWithValue("@OrderItemId", ticketItem.OrderItemId);
                                    
                                    using (SqlDataReader modifiersReader = modifiersCommand.ExecuteReader())
                                    {
                                        while (modifiersReader.Read())
                                        {
                                            ticketItem.Modifiers.Add(modifiersReader.GetString(0));
                                        }
                                    }
                                }
                                
                                ticket.Items.Add(ticketItem);
                            }
                        }
                    }
                }
                
                // Get available courses for new items
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, Name
                    FROM CourseTypes
                    ORDER BY DisplayOrder", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            order.AvailableCourses.Add(new CourseType
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            
            return order;
        }
        
        private int GetCurrentUserId()
        {
            // In a real application, get this from authentication
            // For now, hardcode to 1 (assuming ID 1 is an admin/host user)
            return 1;
        }
    }
}
