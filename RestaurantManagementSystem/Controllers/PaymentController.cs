using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using RestaurantManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace RestaurantManagementSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        
        public PaymentController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }
        
        // Payment Dashboard
        public IActionResult Index(int id)
        {
            var model = GetPaymentViewModel(id);
            
            if (model == null)
            {
                return NotFound();
            }
            
            return View(model);
        }
        
        // Process Payment
        public IActionResult ProcessPayment(int orderId)
        {
            var model = new ProcessPaymentViewModel
            {
                OrderId = orderId
            };
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        o.OrderNumber, 
                        o.TotalAmount, 
                        (o.TotalAmount - ISNULL(SUM(p.Amount + p.TipAmount), 0)) AS RemainingAmount
                    FROM Orders o
                    LEFT JOIN Payments p ON o.Id = p.OrderId AND p.Status = 1 -- Approved payments only
                    WHERE o.Id = @OrderId
                    GROUP BY o.OrderNumber, o.TotalAmount", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OrderNumber = reader.GetString(0);
                            model.TotalAmount = reader.GetDecimal(1);
                            model.RemainingAmount = reader.GetDecimal(2);
                            model.Amount = model.RemainingAmount; // Default to remaining amount
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
                
                // Get available payment methods
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT Id, Name, DisplayName, RequiresCardInfo
                    FROM PaymentMethods
                    WHERE IsActive = 1
                    ORDER BY DisplayName", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailablePaymentMethods.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(2)
                            });
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        [HttpPostAttribute]
        [ValidateAntiForgeryTokenAttribute]
        public IActionResult ProcessPayment(ProcessPaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate payment method requires card info
                    bool requiresCardInfo = false;
                    
                    using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                            SELECT RequiresCardInfo FROM PaymentMethods WHERE Id = @PaymentMethodId", connection))
                        {
                            command.Parameters.AddWithValue("@PaymentMethodId", model.PaymentMethodId);
                            requiresCardInfo = (bool)command.ExecuteScalar();
                        }
                    }
                    
                    // Validate card info if required
                    if (requiresCardInfo)
                    {
                        if (string.IsNullOrEmpty(model.LastFourDigits))
                        {
                            ModelState.AddModelError("LastFourDigits", "Last four digits of card are required for this payment method.");
                            return View(model);
                        }
                        
                        if (string.IsNullOrEmpty(model.CardType))
                        {
                            ModelState.AddModelError("CardType", "Card type is required for this payment method.");
                            return View(model);
                        }
                    }
                    
                    using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand("usp_ProcessPayment", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            
                            command.Parameters.AddWithValue("@OrderId", model.OrderId);
                            command.Parameters.AddWithValue("@PaymentMethodId", model.PaymentMethodId);
                            command.Parameters.AddWithValue("@Amount", model.Amount);
                            command.Parameters.AddWithValue("@TipAmount", model.TipAmount);
                            command.Parameters.AddWithValue("@ReferenceNumber", string.IsNullOrEmpty(model.ReferenceNumber) ? (object)DBNull.Value : model.ReferenceNumber);
                            command.Parameters.AddWithValue("@LastFourDigits", string.IsNullOrEmpty(model.LastFourDigits) ? (object)DBNull.Value : model.LastFourDigits);
                            command.Parameters.AddWithValue("@CardType", string.IsNullOrEmpty(model.CardType) ? (object)DBNull.Value : model.CardType);
                            command.Parameters.AddWithValue("@AuthorizationCode", string.IsNullOrEmpty(model.AuthorizationCode) ? (object)DBNull.Value : model.AuthorizationCode);
                            command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(model.Notes) ? (object)DBNull.Value : model.Notes);
                            command.Parameters.AddWithValue("@ProcessedBy", GetCurrentUserId());
                            command.Parameters.AddWithValue("@ProcessedByName", GetCurrentUserName());
                            
                            using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int paymentId = reader.GetInt32(0);
                                    int paymentStatus = reader.GetInt32(1);
                                    string message = reader.GetString(2);
                                    
                                    if (paymentId > 0)
                                    {
                                        if (paymentStatus == 1) // Approved
                                        {
                                            TempData["SuccessMessage"] = "Payment processed successfully.";
                                        }
                                        else // Pending
                                        {
                                            TempData["InfoMessage"] = "Payment requires approval. It has been saved as pending.";
                                        }
                                        return RedirectToAction("Index", new { id = model.OrderId });
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("", message);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("", "Failed to process payment.");
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
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details again
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        o.OrderNumber, 
                        o.TotalAmount, 
                        (o.TotalAmount - ISNULL(SUM(p.Amount + p.TipAmount), 0)) AS RemainingAmount
                    FROM Orders o
                    LEFT JOIN Payments p ON o.Id = p.OrderId AND p.Status = 1 -- Approved payments only
                    WHERE o.Id = @OrderId
                    GROUP BY o.OrderNumber, o.TotalAmount", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", model.OrderId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OrderNumber = reader.GetString(0);
                            model.TotalAmount = reader.GetDecimal(1);
                            model.RemainingAmount = reader.GetDecimal(2);
                        }
                    }
                }
                
                // Get available payment methods
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT Id, Name, DisplayName, RequiresCardInfo
                    FROM PaymentMethods
                    WHERE IsActive = 1
                    ORDER BY DisplayName", connection))
                {
                    model.AvailablePaymentMethods.Clear();
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailablePaymentMethods.Add(new SelectListItem
                            {
                                Value = reader.GetInt32(0).ToString(),
                                Text = reader.GetString(2),
                                Selected = reader.GetInt32(0) == model.PaymentMethodId
                            });
                            
                            if (reader.GetInt32(0) == model.PaymentMethodId)
                            {
                                model.IsCardPayment = reader.GetBoolean(3);
                            }
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        // Void Payment
        public IActionResult VoidPayment(int id)
        {
            var model = new VoidPaymentViewModel
            {
                PaymentId = id
            };
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        p.Id,
                        p.OrderId,
                        o.OrderNumber,
                        p.Amount,
                        p.TipAmount,
                        pm.DisplayName,
                        p.CreatedAt
                    FROM 
                        Payments p
                    INNER JOIN 
                        Orders o ON p.OrderId = o.Id
                    INNER JOIN
                        PaymentMethods pm ON p.PaymentMethodId = pm.Id
                    WHERE 
                        p.Id = @PaymentId", connection))
                {
                    command.Parameters.AddWithValue("@PaymentId", id);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OrderId = reader.GetInt32(1);
                            model.OrderNumber = reader.GetString(2);
                            model.PaymentAmount = reader.GetDecimal(3);
                            model.TipAmount = reader.GetDecimal(4);
                            model.PaymentMethodDisplay = reader.GetString(5);
                            model.PaymentDate = reader.GetDateTime(6);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        [HttpPostAttribute]
        [ValidateAntiForgeryTokenAttribute]
        public IActionResult VoidPayment(VoidPaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand("usp_VoidPayment", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            
                            command.Parameters.AddWithValue("@PaymentId", model.PaymentId);
                            command.Parameters.AddWithValue("@Reason", model.Reason);
                            command.Parameters.AddWithValue("@ProcessedBy", GetCurrentUserId());
                            command.Parameters.AddWithValue("@ProcessedByName", GetCurrentUserName());
                            
                            using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int result = reader.GetInt32(0);
                                    string message = reader.GetString(1);
                                    
                                    if (result > 0)
                                    {
                                        TempData["SuccessMessage"] = "Payment voided successfully.";
                                        return RedirectToAction("Index", new { id = model.OrderId });
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("", message);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("", "Failed to void payment.");
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
            
            return View(model);
        }
        
        // Split Bill
        public IActionResult SplitBill(int orderId)
        {
            var model = new CreateSplitBillViewModel
            {
                OrderId = orderId
            };
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        o.OrderNumber, 
                        o.Subtotal,
                        o.TaxAmount,
                        o.TotalAmount
                    FROM Orders o
                    WHERE o.Id = @OrderId", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OrderNumber = reader.GetString(0);
                            model.Subtotal = reader.GetDecimal(1);
                            model.TaxAmount = reader.GetDecimal(2);
                            model.TotalAmount = reader.GetDecimal(3);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
                
                // Get order items that are not fully split yet
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        oi.Id,
                        mi.Name,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.Subtotal,
                        -- Calculate already split quantities
                        ISNULL((
                            SELECT SUM(sbi.Quantity)
                            FROM SplitBillItems sbi
                            INNER JOIN SplitBills sb ON sbi.SplitBillId = sb.Id
                            WHERE sbi.OrderItemId = oi.Id AND sb.Status != 2 -- Not voided
                        ), 0) AS SplitQuantity
                    FROM 
                        OrderItems oi
                    INNER JOIN 
                        MenuItems mi ON oi.MenuItemId = mi.Id
                    WHERE 
                        oi.OrderId = @OrderId
                        AND oi.Status != 5 -- Not cancelled
                    ORDER BY
                        oi.Id", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            int quantity = reader.GetInt32(2);
                            decimal unitPrice = reader.GetDecimal(3);
                            decimal subtotal = reader.GetDecimal(4);
                            int splitQuantity = reader.GetInt32(5);
                            
                            int availableQuantity = quantity - splitQuantity;
                            
                            if (availableQuantity > 0)
                            {
                                model.AvailableItems.Add(new SplitBillItemViewModel
                                {
                                    OrderItemId = id,
                                    Name = name,
                                    Quantity = quantity,
                                    AvailableQuantity = availableQuantity,
                                    UnitPrice = unitPrice,
                                    Subtotal = subtotal,
                                    TaxAmount = subtotal * (model.TaxAmount / model.Subtotal) // Proportional tax
                                });
                            }
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        [HttpPostAttribute]
        [ValidateAntiForgeryTokenAttribute]
        public IActionResult SplitBill(CreateSplitBillViewModel model, int[] selectedItems, int[] itemQuantities)
        {
            if (ModelState.IsValid)
            {
                if (selectedItems == null || selectedItems.Length == 0)
                {
                    ModelState.AddModelError("", "Please select at least one item for the split bill.");
                    return View(model);
                }
                
                try
                {
                    // Build items string for stored procedure
                    string itemsString = "";
                    
                    for (int i = 0; i < selectedItems.Length; i++)
                    {
                        int orderItemId = selectedItems[i];
                        int quantity = itemQuantities[i];
                        
                        if (quantity <= 0)
                        {
                            continue; // Skip items with zero quantity
                        }
                        
                        // Get price from model's available items
                        var item = model.AvailableItems.FirstOrDefault(x => x.OrderItemId == orderItemId);
                        
                        if (item != null)
                        {
                            decimal amount = item.UnitPrice * quantity;
                            
                            itemsString += $"{orderItemId},{quantity},{amount};";
                        }
                    }
                    
                    // Remove trailing semicolon
                    if (itemsString.EndsWith(";"))
                    {
                        itemsString = itemsString.Substring(0, itemsString.Length - 1);
                    }
                    
                    using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand("usp_CreateSplitBill", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            
                            command.Parameters.AddWithValue("@OrderId", model.OrderId);
                            command.Parameters.AddWithValue("@Items", itemsString);
                            command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(model.Notes) ? (object)DBNull.Value : model.Notes);
                            command.Parameters.AddWithValue("@CreatedBy", GetCurrentUserId());
                            command.Parameters.AddWithValue("@CreatedByName", GetCurrentUserName());
                            
                            using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int splitBillId = reader.GetInt32(0);
                                    decimal amount = reader.GetDecimal(1);
                                    decimal taxAmount = reader.GetDecimal(2);
                                    decimal totalAmount = reader.GetDecimal(3);
                                    string message = reader.GetString(4);
                                    
                                    if (splitBillId > 0)
                                    {
                                        TempData["SuccessMessage"] = $"Split bill created successfully for ${totalAmount:F2}.";
                                        return RedirectToAction("Index", new { id = model.OrderId });
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("", message);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError("", "Failed to create split bill.");
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
            
            // If we get here, repopulate the model
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        o.OrderNumber, 
                        o.Subtotal,
                        o.TaxAmount,
                        o.TotalAmount
                    FROM Orders o
                    WHERE o.Id = @OrderId", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", model.OrderId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.OrderNumber = reader.GetString(0);
                            model.Subtotal = reader.GetDecimal(1);
                            model.TaxAmount = reader.GetDecimal(2);
                            model.TotalAmount = reader.GetDecimal(3);
                        }
                    }
                }
                
                // Get order items that are not fully split yet
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        oi.Id,
                        mi.Name,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.Subtotal,
                        -- Calculate already split quantities
                        ISNULL((
                            SELECT SUM(sbi.Quantity)
                            FROM SplitBillItems sbi
                            INNER JOIN SplitBills sb ON sbi.SplitBillId = sb.Id
                            WHERE sbi.OrderItemId = oi.Id AND sb.Status != 2 -- Not voided
                        ), 0) AS SplitQuantity
                    FROM 
                        OrderItems oi
                    INNER JOIN 
                        MenuItems mi ON oi.MenuItemId = mi.Id
                    WHERE 
                        oi.OrderId = @OrderId
                        AND oi.Status != 5 -- Not cancelled
                    ORDER BY
                        oi.Id", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", model.OrderId);
                    model.AvailableItems.Clear();
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);
                            int quantity = reader.GetInt32(2);
                            decimal unitPrice = reader.GetDecimal(3);
                            decimal subtotal = reader.GetDecimal(4);
                            int splitQuantity = reader.GetInt32(5);
                            
                            int availableQuantity = quantity - splitQuantity;
                            
                            if (availableQuantity > 0)
                            {
                                var item = new SplitBillItemViewModel
                                {
                                    OrderItemId = id,
                                    Name = name,
                                    Quantity = quantity,
                                    AvailableQuantity = availableQuantity,
                                    UnitPrice = unitPrice,
                                    Subtotal = subtotal,
                                    TaxAmount = subtotal * (model.TaxAmount / model.Subtotal) // Proportional tax
                                };
                                
                                // Set selected state if item was selected in form
                                if (selectedItems != null && selectedItems.Contains(id))
                                {
                                    int index = Array.IndexOf(selectedItems, id);
                                    item.IsSelected = true;
                                    item.SelectedQuantity = itemQuantities[index];
                                }
                                
                                model.AvailableItems.Add(item);
                            }
                        }
                    }
                }
            }
            
            return View(model);
        }
        
        // Helper methods
        private PaymentViewModel GetPaymentViewModel(int orderId)
        {
            var model = new PaymentViewModel
            {
                OrderId = orderId
            };
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();
                
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand("usp_GetOrderPaymentInfo", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        // First result set: Order details
                        if (reader.Read())
                        {
                            model.OrderNumber = reader.GetString(1);
                            model.Subtotal = reader.GetDecimal(2);
                            model.TaxAmount = reader.GetDecimal(3);
                            model.TipAmount = reader.GetDecimal(4);
                            model.DiscountAmount = reader.GetDecimal(5);
                            model.TotalAmount = reader.GetDecimal(6);
                            model.PaidAmount = reader.GetDecimal(7);
                            model.RemainingAmount = reader.GetDecimal(8);
                            model.TableName = reader.GetString(9);
                            model.OrderStatus = reader.GetInt32(10);
                            model.OrderStatusDisplay = model.OrderStatus switch
                            {
                                0 => "Open",
                                1 => "In Progress",
                                2 => "Ready",
                                3 => "Completed",
                                4 => "Cancelled",
                                _ => "Unknown"
                            };
                        }
                        else
                        {
                            return null; // Order not found
                        }
                        
                        // Move to next result set: Order items
                        reader.NextResult();
                        
                        while (reader.Read())
                        {
                            model.OrderItems.Add(new OrderItemViewModel
                            {
                                Id = reader.GetInt32(0),
                                MenuItemId = reader.GetInt32(1),
                                MenuItemName = reader.GetString(2),
                                Quantity = reader.GetInt32(3),
                                UnitPrice = reader.GetDecimal(4),
                                Subtotal = reader.GetDecimal(5)
                            });
                        }
                        
                        // Move to next result set: Payments
                        reader.NextResult();
                        
                        while (reader.Read())
                        {
                            model.Payments.Add(new Payment
                            {
                                Id = reader.GetInt32(0),
                                PaymentMethodId = reader.GetInt32(1),
                                PaymentMethodName = reader.GetString(2),
                                PaymentMethodDisplay = reader.GetString(3),
                                Amount = reader.GetDecimal(4),
                                TipAmount = reader.GetDecimal(5),
                                Status = reader.GetInt32(6),
                                ReferenceNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                                LastFourDigits = reader.IsDBNull(8) ? null : reader.GetString(8),
                                CardType = reader.IsDBNull(9) ? null : reader.GetString(9),
                                AuthorizationCode = reader.IsDBNull(10) ? null : reader.GetString(10),
                                Notes = reader.IsDBNull(11) ? null : reader.GetString(11),
                                ProcessedByName = reader.IsDBNull(12) ? null : reader.GetString(12),
                                CreatedAt = reader.GetDateTime(13)
                            });
                        }
                        
                        // Move to next result set: Available payment methods
                        reader.NextResult();
                        
                        while (reader.Read())
                        {
                            model.AvailablePaymentMethods.Add(new PaymentMethodViewModel
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                DisplayName = reader.GetString(2),
                                RequiresCardInfo = reader.GetBoolean(3),
                                RequiresCardPresent = reader.GetBoolean(4),
                                RequiresApproval = reader.GetBoolean(5)
                            });
                        }
                    }
                }
                
                // Get split bills
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        sb.Id,
                        sb.Amount,
                        sb.TaxAmount,
                        sb.Status,
                        sb.Notes,
                        sb.CreatedByName,
                        sb.CreatedAt
                    FROM SplitBills sb
                    WHERE sb.OrderId = @OrderId
                    ORDER BY sb.CreatedAt DESC", connection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.SplitBills.Add(new SplitBill
                            {
                                Id = reader.GetInt32(0),
                                OrderId = orderId,
                                Amount = reader.GetDecimal(1),
                                TaxAmount = reader.GetDecimal(2),
                                Status = reader.GetInt32(3),
                                Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                                CreatedByName = reader.IsDBNull(5) ? null : reader.GetString(5),
                                CreatedAt = reader.GetDateTime(6)
                            });
                        }
                    }
                }
            }
            
            return model;
        }
        
        private int GetCurrentUserId()
        {
            // In a real app, this would come from authentication
            return 1;
        }
        
        private string GetCurrentUserName()
        {
            // In a real app, this would come from authentication
            return "System Admin";
        }
        
        // GET: Payment/PrintBill
        public IActionResult PrintBill(int orderId)
        {
            try
            {
                var model = GetPaymentViewModel(orderId);
                
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("Index", "Order");
                }
                
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading bill for printing: {ex.Message}";
                return RedirectToAction("Index", new { id = orderId });
            }
        }
    }
}
