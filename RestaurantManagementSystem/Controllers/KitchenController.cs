using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Models;
using RestaurantManagementSystem.ViewModels;

namespace RestaurantManagementSystem.Controllers
{
    public class KitchenController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public KitchenController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        // GET: Kitchen/Dashboard
        public IActionResult Dashboard(int? stationId)
        {
            var viewModel = new KitchenDashboardViewModel();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get all kitchen stations
                viewModel.Stations = GetKitchenStations(connection);
                
                // Set the selected station
                if (stationId.HasValue && stationId.Value > 0)
                {
                    viewModel.SelectedStationId = stationId.Value;
                    viewModel.SelectedStationName = viewModel.Stations.FirstOrDefault(s => s.Id == stationId.Value)?.Name ?? "All Stations";
                }
                else
                {
                    viewModel.SelectedStationName = "All Stations";
                }
                
                // Get tickets by status and station
                viewModel.NewTickets = GetTicketsByStatus(connection, 0, stationId);
                viewModel.InProgressTickets = GetTicketsByStatus(connection, 1, stationId);
                viewModel.ReadyTickets = GetTicketsByStatus(connection, 2, stationId);
                
                // Get dashboard statistics
                viewModel.Stats = GetKitchenStats(connection, stationId);
            }
            
            return View(viewModel);
        }
        
        // GET: Kitchen/Tickets
        public IActionResult Tickets(KitchenStationFilterViewModel filter)
        {
            var viewModel = new KitchenTicketsViewModel
            {
                Filter = filter ?? new KitchenStationFilterViewModel()
            };
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get all kitchen stations for the filter
                viewModel.Filter.Stations = GetKitchenStations(connection);
                
                // Get tickets based on filter
                viewModel.Tickets = GetFilteredTickets(connection, filter);
                
                // Get dashboard statistics
                viewModel.Stats = GetKitchenStats(connection, filter.StationId);
            }
            
            return View(viewModel);
        }
        
        // GET: Kitchen/TicketDetails/{id}
        public IActionResult TicketDetails(int id)
        {
            var viewModel = new KitchenTicketDetailsViewModel();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get ticket details
                using (var command = new SqlCommand("GetKitchenTicketDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@TicketId", id);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            viewModel.Ticket = new KitchenTicket
                            {
                                Id = (int)reader["Id"],
                                TicketNumber = reader["TicketNumber"].ToString(),
                                OrderId = (int)reader["OrderId"],
                                OrderNumber = reader["OrderNumber"].ToString(),
                                KitchenStationId = reader["KitchenStationId"] as int?,
                                StationName = reader["StationName"].ToString(),
                                TableName = reader["TableName"].ToString(),
                                Status = (int)reader["Status"],
                                CreatedAt = (DateTime)reader["CreatedAt"],
                                CompletedAt = reader["CompletedAt"] as DateTime?,
                                MinutesSinceCreated = (int)reader["MinutesSinceCreated"]
                            };
                            
                            // Safely access OrderNotes column, if it exists
                            try 
                            {
                                // Safely handle OrderNotes column which may not exist in all database schemas
                            try
                            {
                                viewModel.OrderNotes = reader["OrderNotes"].ToString();
                            }
                            catch (IndexOutOfRangeException)
                            {
                                // OrderNotes column not found - set to empty string
                                viewModel.OrderNotes = string.Empty;
                            }
                            }
                            catch (IndexOutOfRangeException)
                            {
                                // OrderNotes column does not exist in this schema
                                viewModel.OrderNotes = string.Empty;
                            }
                        }
                        
                        reader.NextResult();
                        
                        while (reader.Read())
                        {
                            var item = new KitchenTicketItem
                            {
                                Id = (int)reader["Id"],
                                KitchenTicketId = (int)reader["KitchenTicketId"],
                                OrderItemId = (int)reader["OrderItemId"],
                                MenuItemName = reader["MenuItemName"].ToString(),
                                Quantity = (int)reader["Quantity"],
                                SpecialInstructions = reader["SpecialInstructions"].ToString(),
                                Status = (int)reader["Status"],
                                StartTime = reader["StartTime"] as DateTime?,
                                CompletionTime = reader["CompletionTime"] as DateTime?,
                                Notes = reader["Notes"].ToString(),
                                MinutesCooking = (int)reader["MinutesCooking"],
                                KitchenStationId = reader["KitchenStationId"] as int?,
                                StationName = reader["StationName"].ToString(),
                                PrepTime = (int)reader["PrepTime"]
                            };
                            
                            viewModel.Items.Add(item);
                        }
                        
                        // Read modifiers for each item
                        reader.NextResult();
                        
                        while (reader.Read())
                        {
                            int itemId = (int)reader["KitchenTicketItemId"];
                            string modifierText = reader["ModifierText"].ToString();
                            
                            var item = viewModel.Items.FirstOrDefault(i => i.Id == itemId);
                            if (item != null)
                            {
                                item.Modifiers.Add(modifierText);
                            }
                        }
                    }
                }
                
                // Check if ticket can be updated (not delivered or cancelled)
                viewModel.CanUpdateStatus = viewModel.Ticket.Status < 3;
            }
            
            return View(viewModel);
        }
        
        // POST: Kitchen/UpdateTicketStatus
        [HttpPost]
        public IActionResult UpdateTicketStatus(KitchenStatusUpdateModel model)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var command = new SqlCommand("UpdateKitchenTicketStatus", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@TicketId", model.TicketId);
                    command.Parameters.AddWithValue("@Status", model.Status);
                    
                    command.ExecuteNonQuery();
                }
            }
            
            return RedirectToAction("TicketDetails", new { id = model.TicketId });
        }
        
        // POST: Kitchen/UpdateItemStatus
        [HttpPost]
        public IActionResult UpdateItemStatus(KitchenItemStatusUpdateModel model, int ticketId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var command = new SqlCommand("UpdateKitchenTicketItemStatus", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ItemId", model.ItemId);
                    command.Parameters.AddWithValue("@Status", model.Status);
                    
                    command.ExecuteNonQuery();
                }
            }
            
            return RedirectToAction("TicketDetails", new { id = ticketId });
        }
        
        // GET: Kitchen/Stations
        public IActionResult Stations()
        {
            var stations = new List<KitchenStation>();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                stations = GetKitchenStations(connection);
            }
            
            return View(stations);
        }
        
        // GET: Kitchen/StationForm
        public IActionResult StationForm(int? id)
        {
            var viewModel = new KitchenStationViewModel();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                if (id.HasValue && id.Value > 0)
                {
                    // Edit existing station
                    using (var command = new SqlCommand("GetKitchenStationById", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StationId", id.Value);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                viewModel.Id = (int)reader["Id"];
                                viewModel.Name = reader["Name"].ToString();
                                viewModel.Description = reader["Description"].ToString();
                                viewModel.IsActive = (bool)reader["IsActive"];
                            }
                            
                            reader.NextResult();
                            
                            // Get assigned menu items
                            while (reader.Read())
                            {
                                int menuItemId = (int)reader["MenuItemId"];
                                viewModel.AssignedMenuItemIds.Add(menuItemId);
                            }
                        }
                    }
                }
                
                // Get all menu items for assignment
                using (var command = new SqlCommand("GetAllMenuItems", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var menuItem = new MenuItemOption
                            {
                                Id = (int)reader["Id"],
                                Name = reader["Name"].ToString(),
                                Category = reader["CategoryName"].ToString(),
                                IsAssigned = viewModel.AssignedMenuItemIds.Contains((int)reader["Id"]),
                                IsPrimary = true // Default primary for assigned items
                            };
                            
                            viewModel.AvailableMenuItems.Add(menuItem);
                        }
                    }
                }
            }
            
            return View(viewModel);
        }
        
        // POST: Kitchen/SaveStation
        [HttpPost]
        public IActionResult SaveStation(KitchenStationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Get all menu items again to repopulate the form
                    using (var command = new SqlCommand("GetAllMenuItems", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var menuItem = new MenuItemOption
                                {
                                    Id = (int)reader["Id"],
                                    Name = reader["Name"].ToString(),
                                    Category = reader["CategoryName"].ToString(),
                                    IsAssigned = model.AssignedMenuItemIds?.Contains((int)reader["Id"]) ?? false,
                                    IsPrimary = true
                                };
                                
                                model.AvailableMenuItems.Add(menuItem);
                            }
                        }
                    }
                }
                
                return View("StationForm", model);
            }
            
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        int stationId;
                        
                        if (model.Id > 0)
                        {
                            // Update existing station
                            using (var command = new SqlCommand("UpdateKitchenStation", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@StationId", model.Id);
                                command.Parameters.AddWithValue("@Name", model.Name);
                                command.Parameters.AddWithValue("@Description", model.Description ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@IsActive", model.IsActive);
                                
                                command.ExecuteNonQuery();
                                stationId = model.Id;
                            }
                            
                            // Delete existing menu item assignments
                            using (var command = new SqlCommand("DeleteKitchenStationMenuItems", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@StationId", stationId);
                                
                                command.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Create new station
                            using (var command = new SqlCommand("CreateKitchenStation", connection, transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@Name", model.Name);
                                command.Parameters.AddWithValue("@Description", model.Description ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@IsActive", model.IsActive);
                                
                                var stationIdParam = new SqlParameter("@StationId", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                command.Parameters.Add(stationIdParam);
                                
                                command.ExecuteNonQuery();
                                stationId = (int)stationIdParam.Value;
                            }
                        }
                        
                        // Assign menu items to station
                        if (model.AssignedMenuItemIds != null && model.AssignedMenuItemIds.Any())
                        {
                            foreach (var menuItemId in model.AssignedMenuItemIds)
                            {
                                using (var command = new SqlCommand("AssignMenuItemToKitchenStation", connection, transaction))
                                {
                                    command.CommandType = CommandType.StoredProcedure;
                                    command.Parameters.AddWithValue("@StationId", stationId);
                                    command.Parameters.AddWithValue("@MenuItemId", menuItemId);
                                    command.Parameters.AddWithValue("@IsPrimary", true); // Default to primary assignment
                                    
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                        
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            
            return RedirectToAction("Stations");
        }

        // POST: Kitchen/DeleteStation/{id}
        [HttpPost]
        public IActionResult DeleteStation(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var command = new SqlCommand("DeleteKitchenStation", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@StationId", id);
                    
                    command.ExecuteNonQuery();
                }
            }
            
            return RedirectToAction("Stations");
        }
        
        // GET: Kitchen/MarkAllReady/{stationId?}
        public IActionResult MarkAllReady(int? stationId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (var command = new SqlCommand("MarkAllTicketsReady", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    
                    if (stationId.HasValue && stationId.Value > 0)
                    {
                        command.Parameters.AddWithValue("@StationId", stationId.Value);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@StationId", DBNull.Value);
                    }
                    
                    command.ExecuteNonQuery();
                }
            }
            
            return RedirectToAction("Dashboard", new { stationId });
        }
        
        // Private helper methods
        private List<KitchenStation> GetKitchenStations(SqlConnection connection)
        {
            var stations = new List<KitchenStation>();
            
            using (var command = new SqlCommand("GetAllKitchenStations", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stations.Add(new KitchenStation
                        {
                            Id = (int)reader["Id"],
                            Name = reader["Name"].ToString(),
                            Description = reader["Description"].ToString(),
                            IsActive = (bool)reader["IsActive"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            UpdatedAt = (DateTime)reader["UpdatedAt"]
                        });
                    }
                }
            }
            
            return stations;
        }
        
        private List<KitchenTicket> GetTicketsByStatus(SqlConnection connection, int status, int? stationId)
        {
            var tickets = new List<KitchenTicket>();
            
            using (var command = new SqlCommand("GetKitchenTicketsByStatus", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@Status", status);
                
                if (stationId.HasValue && stationId.Value > 0)
                {
                    command.Parameters.AddWithValue("@StationId", stationId.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@StationId", DBNull.Value);
                }
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tickets.Add(new KitchenTicket
                        {
                            Id = (int)reader["Id"],
                            TicketNumber = reader["TicketNumber"].ToString(),
                            OrderId = (int)reader["OrderId"],
                            OrderNumber = reader["OrderNumber"].ToString(),
                            KitchenStationId = reader["KitchenStationId"] as int?,
                            StationName = reader["StationName"].ToString(),
                            TableName = reader["TableName"].ToString(),
                            Status = (int)reader["Status"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            CompletedAt = reader["CompletedAt"] as DateTime?,
                            MinutesSinceCreated = (int)reader["MinutesSinceCreated"]
                        });
                    }
                }
            }
            
            return tickets;
        }
        
        private List<KitchenTicket> GetFilteredTickets(SqlConnection connection, KitchenStationFilterViewModel filter)
        {
            var tickets = new List<KitchenTicket>();
            
            using (var command = new SqlCommand("GetFilteredKitchenTickets", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                
                if (filter.StationId.HasValue && filter.StationId.Value > 0)
                {
                    command.Parameters.AddWithValue("@StationId", filter.StationId.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@StationId", DBNull.Value);
                }
                
                if (filter.Status.HasValue)
                {
                    command.Parameters.AddWithValue("@Status", filter.Status.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@Status", DBNull.Value);
                }
                
                if (filter.DateFrom.HasValue)
                {
                    command.Parameters.AddWithValue("@DateFrom", filter.DateFrom.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@DateFrom", DBNull.Value);
                }
                
                if (filter.DateTo.HasValue)
                {
                    command.Parameters.AddWithValue("@DateTo", filter.DateTo.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@DateTo", DBNull.Value);
                }
                
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tickets.Add(new KitchenTicket
                        {
                            Id = (int)reader["Id"],
                            TicketNumber = reader["TicketNumber"].ToString(),
                            OrderId = (int)reader["OrderId"],
                            OrderNumber = reader["OrderNumber"].ToString(),
                            KitchenStationId = reader["KitchenStationId"] as int?,
                            StationName = reader["StationName"].ToString(),
                            TableName = reader["TableName"].ToString(),
                            Status = (int)reader["Status"],
                            CreatedAt = (DateTime)reader["CreatedAt"],
                            CompletedAt = reader["CompletedAt"] as DateTime?,
                            MinutesSinceCreated = (int)reader["MinutesSinceCreated"]
                        });
                    }
                }
            }
            
            return tickets;
        }
        
        private KitchenDashboardStats GetKitchenStats(SqlConnection connection, int? stationId)
        {
            var stats = new KitchenDashboardStats();
            
            using (var command = new SqlCommand("GetKitchenDashboardStats", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                
                if (stationId.HasValue && stationId.Value > 0)
                {
                    command.Parameters.AddWithValue("@StationId", stationId.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@StationId", DBNull.Value);
                }
                
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stats.NewTicketsCount = reader["NewTicketsCount"] != DBNull.Value ? Convert.ToInt32(reader["NewTicketsCount"]) : 0;
                        stats.InProgressTicketsCount = reader["InProgressTicketsCount"] != DBNull.Value ? Convert.ToInt32(reader["InProgressTicketsCount"]) : 0;
                        stats.ReadyTicketsCount = reader["ReadyTicketsCount"] != DBNull.Value ? Convert.ToInt32(reader["ReadyTicketsCount"]) : 0;
                        stats.PendingItemsCount = reader["PendingItemsCount"] != DBNull.Value ? Convert.ToInt32(reader["PendingItemsCount"]) : 0;
                        stats.ReadyItemsCount = reader["ReadyItemsCount"] != DBNull.Value ? Convert.ToInt32(reader["ReadyItemsCount"]) : 0;
                        stats.AvgPrepTimeMinutes = reader["AvgPrepTimeMinutes"] != DBNull.Value ? Convert.ToDouble(reader["AvgPrepTimeMinutes"]) : 0.0;
                    }
                }
            }
            
            return stats;
        }
    }
}
