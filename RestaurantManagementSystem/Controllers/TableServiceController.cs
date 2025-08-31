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
    public class TableServiceController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        
        public TableServiceController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }
        
        // Dashboard for Table Service
        public IActionResult Dashboard()
        {
            var model = GetTableServiceDashboardViewModel();
            return View(model);
        }
        
        // View for Seating a Guest (from reservation or waitlist)
        public IActionResult SeatGuest(int? reservationId = null, int? waitlistId = null)
        {
            var model = new SeatGuestViewModel
            {
                ReservationId = reservationId,
                WaitlistId = waitlistId,
                AvailableTables = GetAvailableTables(),
                Servers = GetAvailableServers()
            };
            
            // If coming from reservation, pre-populate data
            if (reservationId.HasValue)
            {
                var reservation = GetReservationById(reservationId.Value);
                if (reservation != null)
                {
                    model.GuestName = reservation.CustomerName;
                    model.PartySize = reservation.PartySize;
                    model.Notes = reservation.SpecialRequests;
                }
            }
            
            // If coming from waitlist, pre-populate data
            if (waitlistId.HasValue)
            {
                var waitlist = GetWaitlistEntryById(waitlistId.Value);
                if (waitlist != null)
                {
                    model.GuestName = waitlist.CustomerName;
                    model.PartySize = waitlist.PartySize;
                    model.Notes = waitlist.SpecialRequests;
                }
            }
            
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SeatGuest(SeatGuestViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // First, start table turnover
                    var turnoverResult = StartTableTurnover(
                        model.TableId,
                        model.ReservationId,
                        model.WaitlistId,
                        model.GuestName,
                        model.PartySize,
                        model.Notes,
                        model.TargetTurnTime
                    );
                    
                    // If turnover started successfully, assign server
                    if (turnoverResult.Success)
                    {
                        var assignResult = AssignServerToTable(model.TableId, model.ServerId, GetCurrentUserId());
                        
                        if (assignResult.Success)
                        {
                            TempData["SuccessMessage"] = "Guest seated and server assigned successfully.";
                            return RedirectToAction(nameof(Dashboard));
                        }
                        else
                        {
                            ModelState.AddModelError("", $"Error assigning server: {assignResult.Message}");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Error seating guest: {turnoverResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            
            // If we reach here, there was an error - repopulate dropdowns
            model.AvailableTables = GetAvailableTables();
            model.Servers = GetAvailableServers();
            return View(model);
        }
        
        // View Active Tables
        public IActionResult ActiveTables()
        {
            var model = GetActiveTables();
            return View(model);
        }
        
        // Update Table Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateTableStatus(int turnoverId, int newStatus)
        {
            try
            {
                var result = UpdateTableTurnoverStatus(turnoverId, newStatus);
                
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Table status updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error updating table status: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }
            
            return RedirectToAction(nameof(ActiveTables));
        }
        
        // Helper Methods
        private TableServiceDashboardViewModel GetTableServiceDashboardViewModel()
        {
            var model = new TableServiceDashboardViewModel
            {
                AvailableTables = 0,
                OccupiedTables = 0,
                DirtyTables = 0,
                ReservationCount = 0,
                WaitlistCount = 0,
                CurrentTurnovers = new List<ActiveTableViewModel>()
            };
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get table counts
                using (SqlCommand command = new SqlCommand(@"
                    SELECT
                        SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS AvailableCount,
                        SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS OccupiedCount,
                        SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS DirtyCount
                    FROM Tables", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.AvailableTables = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            model.OccupiedTables = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            model.DirtyTables = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        }
                    }
                }
                
                // Get today's pending reservation count
                using (SqlCommand command = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM Reservations
                    WHERE CAST(ReservationTime AS DATE) = CAST(GETDATE() AS DATE)
                    AND Status = 0", connection)) // 0 = Pending
                {
                    model.ReservationCount = (int)command.ExecuteScalar();
                }
                
                // Get active waitlist count
                using (SqlCommand command = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM Waitlist
                    WHERE Status = 0", connection)) // 0 = Waiting
                {
                    model.WaitlistCount = (int)command.ExecuteScalar();
                }
                
                // Get current active turnovers
                using (SqlCommand command = new SqlCommand(@"
                    SELECT TOP 5
                        t.Id,
                        tb.Id AS TableId,
                        tb.TableName,
                        t.GuestName,
                        t.PartySize,
                        t.SeatedAt,
                        t.Status,
                        u.FullName AS ServerName,
                        DATEDIFF(MINUTE, t.SeatedAt, GETDATE()) AS MinutesSinceSeated
                    FROM TableTurnovers t
                    INNER JOIN Tables tb ON t.TableId = tb.Id
                    LEFT JOIN ServerAssignments sa ON tb.Id = sa.TableId AND sa.IsActive = 1
                    LEFT JOIN Users u ON sa.ServerId = u.Id
                    WHERE t.Status < 5 -- Not departed
                    ORDER BY t.SeatedAt DESC", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.CurrentTurnovers.Add(new ActiveTableViewModel
                            {
                                TurnoverId = reader.GetInt32(0),
                                TableId = reader.GetInt32(1),
                                TableName = reader.GetString(2),
                                GuestName = reader.GetString(3),
                                PartySize = reader.GetInt32(4),
                                SeatedAt = reader.GetDateTime(5),
                                Status = reader.GetInt32(6),
                                ServerName = reader.IsDBNull(7) ? "Unassigned" : reader.GetString(7),
                                Duration = reader.GetInt32(8)
                            });
                        }
                    }
                }
            }
            
            return model;
        }
        
        private List<SelectListItem> GetAvailableTables()
        {
            var tables = new List<SelectListItem>();
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, TableName, Capacity, Status
                    FROM Tables
                    WHERE Status = 0 -- Available
                    ORDER BY TableName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = $"{reader.GetString(1)} (Seats {reader.GetInt32(2)})"
                            });
                        }
                    }
                }
            }
            
            return tables;
        }
        
        private List<SelectListItem> GetAvailableServers()
        {
            var servers = new List<SelectListItem>();
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, FullName
                    FROM Users
                    WHERE Role = 2 -- Server role
                    AND IsActive = 1
                    ORDER BY FullName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            servers.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            
            return servers;
        }
        
        private Reservation GetReservationById(int id)
        {
            Reservation reservation = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, CustomerName, PhoneNumber, Email, PartySize, ReservationTime, SpecialRequests, Status
                    FROM Reservations
                    WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            reservation = new Reservation
                            {
                                Id = reader.GetInt32(0),
                                CustomerName = reader.GetString(1),
                                PhoneNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                PartySize = reader.GetInt32(4),
                                ReservationTime = reader.GetDateTime(5),
                                SpecialRequests = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Status = reader.GetInt32(7)
                            };
                        }
                    }
                }
            }
            
            return reservation;
        }
        
        private WaitlistEntry GetWaitlistEntryById(int id)
        {
            WaitlistEntry waitlist = null;
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"
                    SELECT Id, CustomerName, PhoneNumber, PartySize, CheckInTime, EstimatedWaitMinutes, SpecialRequests, Status
                    FROM Waitlist
                    WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            waitlist = new WaitlistEntry
                            {
                                Id = reader.GetInt32(0),
                                CustomerName = reader.GetString(1),
                                PhoneNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                                PartySize = reader.GetInt32(3),
                                CheckInTime = reader.GetDateTime(4),
                                EstimatedWaitMinutes = reader.GetInt32(5),
                                SpecialRequests = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Status = reader.GetInt32(7)
                            };
                        }
                    }
                }
            }
            
            return waitlist;
        }
        
        private List<ActiveTableViewModel> GetActiveTables()
        {
            var activeTables = new List<ActiveTableViewModel>();
            
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(@"
                    SELECT
                        t.Id AS TurnoverId,
                        tb.Id AS TableId,
                        tb.TableName,
                        t.GuestName,
                        t.PartySize,
                        t.SeatedAt,
                        t.StartedServiceAt,
                        t.CompletedAt,
                        t.Status,
                        u.FullName AS ServerName,
                        u.Id AS ServerId,
                        DATEDIFF(MINUTE, t.SeatedAt, GETDATE()) AS MinutesSinceSeated,
                        t.TargetTurnTimeMinutes
                    FROM TableTurnovers t
                    INNER JOIN Tables tb ON t.TableId = tb.Id
                    LEFT JOIN ServerAssignments sa ON tb.Id = sa.TableId AND sa.IsActive = 1
                    LEFT JOIN Users u ON sa.ServerId = u.Id
                    WHERE t.Status < 5 -- Not departed
                    ORDER BY tb.TableName", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            activeTables.Add(new ActiveTableViewModel
                            {
                                TurnoverId = reader.GetInt32(0),
                                TableId = reader.GetInt32(1),
                                TableName = reader.GetString(2),
                                GuestName = reader.GetString(3),
                                PartySize = reader.GetInt32(4),
                                SeatedAt = reader.GetDateTime(5),
                                StartedServiceAt = reader.IsDBNull(6) ? null : (DateTime?)reader.GetDateTime(6),
                                CompletedAt = reader.IsDBNull(7) ? null : (DateTime?)reader.GetDateTime(7),
                                Status = reader.GetInt32(8),
                                ServerName = reader.IsDBNull(9) ? "Unassigned" : reader.GetString(9),
                                ServerId = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                                Duration = reader.GetInt32(11),
                                TargetTurnTime = reader.GetInt32(12)
                            });
                        }
                    }
                }
            }
            
            return activeTables;
        }
        
        private (bool Success, string Message) StartTableTurnover(int tableId, int? reservationId, int? waitlistId, string guestName, int partySize, string notes, int targetTurnTime)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("usp_StartTableTurnover", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@TableId", tableId);
                        command.Parameters.AddWithValue("@ReservationId", reservationId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@WaitlistId", waitlistId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GuestName", guestName);
                        command.Parameters.AddWithValue("@PartySize", partySize);
                        command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                        command.Parameters.AddWithValue("@TargetTurnTimeMinutes", targetTurnTime);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string message = reader.GetString(0);
                                
                                // If message doesn't contain "error" keyword, consider it success
                                bool success = !message.ToLower().Contains("error");
                                
                                return (success, message);
                            }
                        }
                    }
                }
                
                return (false, "Unknown error occurred.");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }
        
        private (bool Success, string Message) AssignServerToTable(int tableId, int serverId, int assignedById)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("usp_AssignServerToTable", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@TableId", tableId);
                        command.Parameters.AddWithValue("@ServerId", serverId);
                        command.Parameters.AddWithValue("@AssignedById", assignedById);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string message = reader.GetString(0);
                                
                                // If message doesn't contain "error" keyword, consider it success
                                bool success = !message.ToLower().Contains("error") && 
                                               !message.ToLower().Contains("invalid") && 
                                               !message.ToLower().Contains("does not exist");
                                
                                return (success, message);
                            }
                        }
                    }
                }
                
                return (false, "Unknown error occurred.");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }
        
        private (bool Success, string Message) UpdateTableTurnoverStatus(int turnoverId, int newStatus)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("usp_UpdateTableTurnoverStatus", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@TurnoverId", turnoverId);
                        command.Parameters.AddWithValue("@NewStatus", newStatus);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string message = reader.GetString(0);
                                
                                // If message doesn't contain "error" keyword, consider it success
                                bool success = !message.ToLower().Contains("error");
                                
                                return (success, message);
                            }
                        }
                    }
                }
                
                return (false, "Unknown error occurred.");
            }
            catch (Exception ex)
            {
                return (false, $"Exception: {ex.Message}");
            }
        }
        
        private int GetCurrentUserId()
        {
            // In a real application, get this from authentication
            // For now, hardcode to 1 (assuming ID 1 is an admin/host user)
            return 1;
        }
    }
}
