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
        private readonly ILogger<PaymentController> _logger;

        // Helper to get merged table display name for an order
        private string GetMergedTableDisplayName(int orderId, string existingTableName)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                        SELECT STRING_AGG(t.TableName, ' + ') WITHIN GROUP (ORDER BY t.TableName)
                        FROM OrderTables ot
                        INNER JOIN Tables t ON ot.TableId = t.Id
                        WHERE ot.OrderId = @OrderId", connection);
                    cmd.Parameters.AddWithValue("@OrderId", orderId);
                    var aggregated = cmd.ExecuteScalar() as string;
                    
                    if (string.IsNullOrWhiteSpace(aggregated))
                        return existingTableName; // No merged tables, return original
                    
                    // If there's both a primary table and merged tables, combine without duplicates
                    if (!string.IsNullOrWhiteSpace(existingTableName) && !aggregated.Contains(existingTableName))
                        return existingTableName + " + " + aggregated;
                    
                    return aggregated; // Return merged table names
                }
            }
            catch
            {
                return existingTableName; // Fallback to existing if error
            }
        }

        public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
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
        
        // Fix fully paid orders that are stuck in active status
        public IActionResult FixPaidOrderStatus(int orderId)
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                        UPDATE Orders 
                        SET Status = 3, -- Completed
                            CompletedAt = GETDATE(),
                            UpdatedAt = GETDATE()
                        WHERE Id = @OrderId 
                        AND Status < 3 -- Not already completed
                        AND (
                            SELECT ISNULL(SUM(Amount + TipAmount), 0) 
                            FROM Payments 
                            WHERE OrderId = @OrderId AND Status = 1
                        ) >= TotalAmount", connection))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "Order status updated to Completed successfully.";
                        }
                        else
                        {
                            TempData["InfoMessage"] = "Order is either already completed or not fully paid.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating order status: {ex.Message}";
            }
            
            return RedirectToAction("Index", new { id = orderId });
        }
        
        // Fix all fully paid orders that are stuck in active status
        public IActionResult FixAllPaidOrders()
        {
            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                        UPDATE Orders 
                        SET Status = 3, -- Completed
                            CompletedAt = GETDATE(),
                            UpdatedAt = GETDATE()
                        WHERE Status < 3 -- Not already completed
                        AND Id IN (
                            SELECT o.Id
                            FROM Orders o
                            WHERE o.Status < 3
                            AND (
                                SELECT ISNULL(SUM(p.Amount + p.TipAmount), 0) 
                                FROM Payments p 
                                WHERE p.OrderId = o.Id AND p.Status = 1
                            ) >= o.TotalAmount
                        )", connection))
                    {
                        int rowsAffected = cmd.ExecuteNonQuery();
                        TempData["SuccessMessage"] = $"Fixed {rowsAffected} fully paid orders that were stuck in active status.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error fixing paid orders: {ex.Message}";
            }
            
            return RedirectToAction("Dashboard", "Order");
        }
        
        // Process Payment
        public IActionResult ProcessPayment(int orderId)
        {
            // Get payment view model with GST calculations
            var paymentViewModel = GetPaymentViewModel(orderId);
            if (paymentViewModel == null)
            {
                return NotFound();
            }
            
            var model = new ProcessPaymentViewModel
            {
                OrderId = orderId,
                OrderNumber = paymentViewModel.OrderNumber,
                TotalAmount = paymentViewModel.TotalAmount, // This now includes GST
                RemainingAmount = paymentViewModel.RemainingAmount, // This is Total - Paid (including GST)
                Amount = paymentViewModel.RemainingAmount // Default to remaining amount
            };
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();

                // Ensure UPI method exists
                using (var ensureCmd = new Microsoft.Data.SqlClient.SqlCommand(@"IF NOT EXISTS (SELECT 1 FROM PaymentMethods WHERE Name='UPI')
BEGIN
    INSERT INTO PaymentMethods (Name, DisplayName, IsActive, RequiresCardInfo, RequiresCardPresent, RequiresApproval)
    VALUES ('UPI','UPI',1,0,0,0);
END", connection))
                {
                    ensureCmd.ExecuteNonQuery();
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
                            if (reader.GetString(1).Equals("UPI", StringComparison.OrdinalIgnoreCase))
                            {
                                model.IsUPIPayment = true; // marker for JS (initial load none selected so not used yet)
                            }
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
                    string paymentMethodName = string.Empty;
                    
                    using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                            SELECT Name, RequiresCardInfo FROM PaymentMethods WHERE Id = @PaymentMethodId", connection))
                        {
                            command.Parameters.AddWithValue("@PaymentMethodId", model.PaymentMethodId);
                            using (var rdr = command.ExecuteReader())
                            {
                                if (rdr.Read())
                                {
                                    paymentMethodName = rdr.GetString(0);
                                    requiresCardInfo = rdr.GetBoolean(1);
                                }
                            }
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
                            // If UPI selected store reference in ReferenceNumber if not provided separately
                            if (paymentMethodName.Equals("UPI", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(model.UPIReference))
                            {
                                // override ReferenceNumber param value
                                command.Parameters["@ReferenceNumber"].Value = model.UPIReference;
                            }
                            
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

                                        // If discount provided update order
                                        if (model.DiscountAmount > 0)
                                        {
                                            reader.Close();
                                            using (var discountCmd = new Microsoft.Data.SqlClient.SqlCommand(@"UPDATE Orders SET DiscountAmount = DiscountAmount + @Disc, UpdatedAt = GETDATE(), TotalAmount = Subtotal + TaxAmount + TipAmount - (DiscountAmount + @Disc) WHERE Id = @OrderId", connection))
                                            {
                                                discountCmd.Parameters.AddWithValue("@Disc", model.DiscountAmount);
                                                discountCmd.Parameters.AddWithValue("@OrderId", model.OrderId);
                                                discountCmd.ExecuteNonQuery();
                                            }
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

        // Payment Dashboard
        public IActionResult Dashboard(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var model = new PaymentDashboardViewModel
            {
                FromDate = fromDate ?? DateTime.Today,
                ToDate = toDate ?? DateTime.Today
            };

            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();

                // Get today's analytics
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        ISNULL(SUM(p.Amount), 0) AS TotalPayments,
                        ISNULL(SUM(p.TipAmount), 0) AS TotalTips
                    FROM Payments p
                    WHERE p.Status = 1 -- Approved payments only
                        AND CAST(p.CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.TodayTotalPayments = reader.GetDecimal(0);
                            model.TodayTotalTips = reader.GetDecimal(1);
                        }
                    }
                }

                // Calculate today's GST from actual processed payments (Payment Amount - Order Subtotal)
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT ISNULL(SUM(p.Amount - o.Subtotal), 0) AS TotalGST
                    FROM Payments p
                    INNER JOIN Orders o ON p.OrderId = o.Id
                    WHERE p.Status = 1 -- Approved payments only
                        AND CAST(p.CreatedAt AS DATE) = CAST(GETDATE() AS DATE)", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.TodayTotalGST = Math.Max(0, reader.GetDecimal(0)); // Ensure GST is not negative
                        }
                    }
                }

                // Get today's payment method breakdown
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        pm.Id AS PaymentMethodId,
                        pm.Name AS PaymentMethodName,
                        pm.DisplayName AS PaymentMethodDisplayName,
                        ISNULL(SUM(p.Amount), 0) AS TotalAmount,
                        COUNT(p.Id) AS TransactionCount
                    FROM PaymentMethods pm
                    LEFT JOIN Payments p ON pm.Id = p.PaymentMethodId 
                        AND p.Status = 1 -- Approved payments only
                        AND CAST(p.CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                    WHERE pm.IsActive = 1
                    GROUP BY pm.Id, pm.Name, pm.DisplayName
                    ORDER BY TotalAmount DESC", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.PaymentMethodBreakdowns.Add(new PaymentMethodBreakdown
                            {
                                PaymentMethodId = reader.GetInt32("PaymentMethodId"),
                                PaymentMethodName = reader.GetString("PaymentMethodName"),
                                PaymentMethodDisplayName = reader.GetString("PaymentMethodDisplayName"),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                TransactionCount = reader.GetInt32("TransactionCount")
                            });
                        }
                    }
                }

                // Get payment history - showing actual processed payments with their real amounts
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        o.Id AS OrderId,
                        o.OrderNumber,
                        ISNULL(tt.TableName, 'Takeout/Delivery') AS TableName,
                        -- Total payable is the sum of all payment amounts for this order (includes GST as processed)
                        (SELECT ISNULL(SUM(p2.Amount), 0) FROM Payments p2 WHERE p2.OrderId = o.Id AND p2.Status = 1) AS TotalPayable,
                        ISNULL(SUM(p.Amount), 0) AS TotalPaid,
                        -- Due amount is 0 since we're only showing orders with payments
                        0 AS DueAmount,
                        MAX(p.CreatedAt) AS PaymentDate,
                        o.Status AS OrderStatus,
                        CASE o.Status 
                            WHEN 0 THEN 'Open'
                            WHEN 1 THEN 'In Progress'
                            WHEN 2 THEN 'Ready'
                            WHEN 3 THEN 'Completed'
                            WHEN 4 THEN 'Cancelled'
                            ELSE 'Unknown'
                        END AS OrderStatusDisplay
                    FROM Orders o
                    LEFT JOIN TableTurnovers tto ON o.TableTurnoverId = tto.Id
                    LEFT JOIN Tables tt ON tto.TableId = tt.Id
                    INNER JOIN Payments p ON o.Id = p.OrderId AND p.Status = 1 -- Only orders with approved payments
                    WHERE CAST(p.CreatedAt AS DATE) BETWEEN @FromDate AND @ToDate
                    GROUP BY o.Id, o.OrderNumber, tt.TableName, o.Status
                    ORDER BY MAX(p.CreatedAt) DESC", connection))
                {
                    command.Parameters.AddWithValue("@FromDate", model.FromDate.Date);
                    command.Parameters.AddWithValue("@ToDate", model.ToDate.Date);

                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.PaymentHistory.Add(new PaymentHistoryItem
                            {
                                OrderId = reader.GetInt32("OrderId"),
                                OrderNumber = reader.GetString("OrderNumber"),
                                TableName = GetMergedTableDisplayName((int)reader["OrderId"], reader.GetString("TableName")),
                                TotalPayable = Convert.ToDecimal(reader["TotalPayable"]),
                                TotalPaid = Convert.ToDecimal(reader["TotalPaid"]),
                                DueAmount = Convert.ToDecimal(reader["DueAmount"]),
                                PaymentDate = reader.GetDateTime("PaymentDate"),
                                OrderStatus = reader.GetInt32("OrderStatus"),
                                OrderStatusDisplay = reader.GetString("OrderStatusDisplay")
                            });
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
                            // Override with merged table names if available
                            model.TableName = GetMergedTableDisplayName(orderId, model.TableName);
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
                
                // Calculate GST breakdown
                try
                {
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT DefaultGSTPercentage FROM RestaurantSettings", connection))
                    {
                        var result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            model.GSTPercentage = Convert.ToDecimal(result);
                        }
                        else
                        {
                            model.GSTPercentage = 5.0m; // Default fallback to 5%
                        }
                    }
                    
                    // Calculate GST amount based on current TaxAmount from stored procedure
                    decimal gstAmount = model.TaxAmount;
                    
                    // If TaxAmount is 0 but we have a subtotal, calculate GST
                    if (gstAmount == 0 && model.Subtotal > 0)
                    {
                        gstAmount = Math.Round(model.Subtotal * model.GSTPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                        model.TaxAmount = gstAmount; // Update the TaxAmount
                    }
                    
                    // Split GST into CGST and SGST (equal split)
                    model.CGSTAmount = Math.Round(gstAmount / 2m, 2, MidpointRounding.AwayFromZero);
                    model.SGSTAmount = gstAmount - model.CGSTAmount; // Ensures total adds up exactly
                    
                    // Recalculate Total = Subtotal + GST - Discount + Tip
                    model.TotalAmount = model.Subtotal + gstAmount - model.DiscountAmount + model.TipAmount;
                    model.RemainingAmount = model.TotalAmount - model.PaidAmount;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error calculating GST for order {OrderId}", model.OrderId);
                    // Fallback values
                    model.GSTPercentage = 5.0m;
                    decimal fallbackGst = Math.Round(model.Subtotal * 0.05m, 2, MidpointRounding.AwayFromZero);
                    model.TaxAmount = fallbackGst;
                    model.CGSTAmount = Math.Round(fallbackGst / 2m, 2, MidpointRounding.AwayFromZero);
                    model.SGSTAmount = fallbackGst - model.CGSTAmount;
                    // Recalculate Total with fallback GST
                    model.TotalAmount = model.Subtotal + fallbackGst - model.DiscountAmount + model.TipAmount;
                    model.RemainingAmount = model.TotalAmount - model.PaidAmount;
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
                
                // Get restaurant settings for bill header
                RestaurantSettings settings = null;
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT * FROM RestaurantSettings", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                settings = new RestaurantSettings
                                {
                                    RestaurantName = reader["RestaurantName"].ToString(),
                                    StreetAddress = reader["StreetAddress"].ToString(),
                                    City = reader["City"].ToString(),
                                    State = reader["State"].ToString(),
                                    Pincode = reader["Pincode"].ToString(),
                                    Country = reader["Country"].ToString(),
                                    GSTCode = reader["GSTCode"].ToString(),
                                    PhoneNumber = reader["PhoneNumber"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Website = reader["Website"].ToString(),
                                    CurrencySymbol = reader["CurrencySymbol"].ToString(),
                                    DefaultGSTPercentage = reader["DefaultGSTPercentage"] != DBNull.Value 
                                        ? Convert.ToDecimal(reader["DefaultGSTPercentage"]) 
                                        : 0
                                };
                            }
                        }
                    }
                }
                
                ViewBag.RestaurantSettings = settings ?? new RestaurantSettings
                {
                    RestaurantName = "Restaurant Management System",
                    GSTCode = "Not Configured",
                    StreetAddress = "",
                    City = "",
                    State = "",
                    Pincode = "",
                    Country = "",
                    PhoneNumber = "",
                    Email = ""
                };
                
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
