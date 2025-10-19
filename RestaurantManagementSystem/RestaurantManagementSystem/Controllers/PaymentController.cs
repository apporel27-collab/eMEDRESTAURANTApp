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
    public partial class PaymentController : Controller
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
                Amount = paymentViewModel.RemainingAmount, // Default to remaining amount
                Subtotal = paymentViewModel.Subtotal, // base amount before GST
                GSTPercentage = paymentViewModel.GSTPercentage // dynamic GST %
            };
            
            using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                connection.Open();

                // Ensure UPI and Complementary methods exist
                using (var ensureCmd = new Microsoft.Data.SqlClient.SqlCommand(@"
-- Ensure UPI method exists
IF NOT EXISTS (SELECT 1 FROM PaymentMethods WHERE Name='UPI')
BEGIN
    INSERT INTO PaymentMethods (Name, DisplayName, IsActive, RequiresCardInfo, RequiresCardPresent, RequiresApproval)
    VALUES ('UPI','UPI',1,0,0,0);
END

-- Ensure Complementary method exists
IF NOT EXISTS (SELECT 1 FROM PaymentMethods WHERE Name='Complementary')
BEGIN
    INSERT INTO PaymentMethods (Name, DisplayName, IsActive, RequiresCardInfo, RequiresCardPresent, RequiresApproval)
    VALUES ('Complementary','Complementary (100% Discount)',1,0,0,1);
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
                    
                    // Read approval settings to decide if discounts or card payments need approval
                    bool discountApprovalRequired = false;
                    bool cardPaymentApprovalRequired = false;
                    try
                    {
                        using (var settingsConn = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                        {
                            settingsConn.Open();
                            using (var settingsCmd = new Microsoft.Data.SqlClient.SqlCommand(@"SELECT TOP 1 IsDiscountApprovalRequired, IsCardPaymentApprovalRequired FROM dbo.RestaurantSettings ORDER BY Id DESC", settingsConn))
                            {
                                using (var rs = settingsCmd.ExecuteReader())
                                {
                                    if (rs.Read())
                                    {
                                        if (!rs.IsDBNull(0)) discountApprovalRequired = rs.GetBoolean(0);
                                        if (!rs.IsDBNull(1)) cardPaymentApprovalRequired = rs.GetBoolean(1);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // If settings read fails, default to existing behavior (no extra approvals)
                        discountApprovalRequired = false;
                        cardPaymentApprovalRequired = false;
                    }

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
                    
                    // Calculate GST information before processing payment
                    decimal gstPercentage = 5.0m; // Default
                    decimal subtotal = 0m;
                    decimal gstAmount = 0m;
                    decimal cgstAmount = 0m;
                    decimal sgstAmount = 0m;
                    decimal amountExclGST = 0m;
                    
                    // Get GST percentage and order subtotal
                    using (Microsoft.Data.SqlClient.SqlConnection gstConnection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        gstConnection.Open();
                        
                        // Get GST percentage from settings
                        using (Microsoft.Data.SqlClient.SqlCommand gstCmd = new Microsoft.Data.SqlClient.SqlCommand(
                            "SELECT DefaultGSTPercentage FROM dbo.RestaurantSettings", gstConnection))
                        {
                            var result = gstCmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                gstPercentage = Convert.ToDecimal(result);
                            }
                        }
                        
                        // Get order subtotal for GST calculation
                        using (Microsoft.Data.SqlClient.SqlCommand subtotalCmd = new Microsoft.Data.SqlClient.SqlCommand(
                            "SELECT Subtotal FROM Orders WHERE Id = @OrderId", gstConnection))
                        {
                            subtotalCmd.Parameters.AddWithValue("@OrderId", model.OrderId);
                            var subtotalResult = subtotalCmd.ExecuteScalar();
                            if (subtotalResult != null && subtotalResult != DBNull.Value)
                            {
                                subtotal = Convert.ToDecimal(subtotalResult);
                            }
                        }
                    }
                    
                    // Get GST percentage and order details
                    decimal paymentGstPercentage = 5.0m; // Default fallback
                    decimal orderSubtotal = 0m;
                    using (var gstConnection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        gstConnection.Open();
                        
                        // Get GST percentage from settings
                        using (var gstCmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT DefaultGSTPercentage FROM dbo.RestaurantSettings", gstConnection))
                        {
                            var result = gstCmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                paymentGstPercentage = Convert.ToDecimal(result);
                            }
                        }
                        
                        // Get order subtotal (amount before GST)
                        using (var subtotalCmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT Subtotal FROM Orders WHERE Id = @OrderId", gstConnection))
                        {
                            subtotalCmd.Parameters.AddWithValue("@OrderId", model.OrderId);
                            var subtotalResult = subtotalCmd.ExecuteScalar();
                            if (subtotalResult != null && subtotalResult != DBNull.Value)
                            {
                                orderSubtotal = Convert.ToDecimal(subtotalResult);
                            }
                        }
                    }
                    
                    // NEW CORRECT PROCESS: Apply discount on subtotal, then recalculate GST
                    // Step 1: Apply discount on original subtotal (excludes GST)
                    decimal discountAmount = model.DiscountAmount;
                    decimal discountedSubtotal = orderSubtotal - discountAmount;
                    
                    // Step 2: Calculate GST on the discounted subtotal
                    decimal paymentGstAmount = Math.Round(discountedSubtotal * paymentGstPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                    
                    // Step 3: Calculate final amounts
                    decimal paymentAmountExclGST = discountedSubtotal; // This is the subtotal after discount
                    decimal totalPaymentAmountWithGST = discountedSubtotal + paymentGstAmount; // Final amount customer pays
                    
                    // Check for Complementary payment method - ensure discount is properly set
                    if (paymentMethodName.Equals("Complementary", StringComparison.OrdinalIgnoreCase))
                    {
                        // For Complementary, ensure 100% discount is applied
                        discountAmount = orderSubtotal; // Full subtotal amount as discount
                        discountedSubtotal = 0; // After 100% discount, subtotal is 0
                        paymentGstAmount = 0; // No GST on a zero subtotal
                        paymentAmountExclGST = 0; // Zero subtotal after discount
                        totalPaymentAmountWithGST = 0; // Zero total to pay
                        
                        // If the model didn't already have the discount set to full amount
                        model.DiscountAmount = discountAmount;
                    }
                    
                    // Step 4: Split GST into CGST and SGST (equal split)
                    decimal paymentCgstPercentage = paymentGstPercentage / 2m;
                    decimal paymentSgstPercentage = paymentGstPercentage / 2m;
                    decimal paymentCgstAmount = Math.Round(paymentGstAmount / 2m, 2, MidpointRounding.AwayFromZero);
                    decimal paymentSgstAmount = paymentGstAmount - paymentCgstAmount; // Ensures total adds up exactly
                    
                    using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand("[dbo].[usp_ProcessPayment]", connection))
                        {
                            command.CommandType = CommandType.StoredProcedure;
                            
                            command.Parameters.AddWithValue("@OrderId", model.OrderId);
                            command.Parameters.AddWithValue("@PaymentMethodId", model.PaymentMethodId);
                            command.Parameters.AddWithValue("@Amount", totalPaymentAmountWithGST); // Store exact payment amount (discounted subtotal + GST)
                            command.Parameters.AddWithValue("@TipAmount", model.TipAmount);
                            command.Parameters.AddWithValue("@ReferenceNumber", string.IsNullOrEmpty(model.ReferenceNumber) ? (object)DBNull.Value : model.ReferenceNumber);
                            command.Parameters.AddWithValue("@LastFourDigits", string.IsNullOrEmpty(model.LastFourDigits) ? (object)DBNull.Value : model.LastFourDigits);
                            command.Parameters.AddWithValue("@CardType", string.IsNullOrEmpty(model.CardType) ? (object)DBNull.Value : model.CardType);
                            command.Parameters.AddWithValue("@AuthorizationCode", string.IsNullOrEmpty(model.AuthorizationCode) ? (object)DBNull.Value : model.AuthorizationCode);
                            command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(model.Notes) ? (object)DBNull.Value : model.Notes);
                            command.Parameters.AddWithValue("@ProcessedBy", GetCurrentUserId());
                            command.Parameters.AddWithValue("@ProcessedByName", GetCurrentUserName());
                            
                            // Add GST-related parameters
                            command.Parameters.AddWithValue("@GSTAmount", paymentGstAmount);
                            command.Parameters.AddWithValue("@CGSTAmount", paymentCgstAmount);
                            command.Parameters.AddWithValue("@SGSTAmount", paymentSgstAmount);
                            command.Parameters.AddWithValue("@DiscAmount", model.DiscountAmount);
                            command.Parameters.AddWithValue("@GST_Perc", paymentGstPercentage);
                            command.Parameters.AddWithValue("@CGST_Perc", paymentCgstPercentage);
                            command.Parameters.AddWithValue("@SGST_Perc", paymentSgstPercentage);
                            command.Parameters.AddWithValue("@Amount_ExclGST", paymentAmountExclGST); // Amount excluding GST
                            
                            // Note: ForceApproval will be handled after payment creation if discount is applied
                            
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
                                        // Decide whether this payment should be pending based on settings and payment details
                                        bool needsApproval = false;

                                        // If a discount was applied and discount approvals are required
                                        if (model.DiscountAmount > 0 && discountApprovalRequired)
                                        {
                                            needsApproval = true;
                                        }

                                        // If payment method requires card info (a card payment) and card approvals are required
                                        if (requiresCardInfo && cardPaymentApprovalRequired)
                                        {
                                            needsApproval = true;
                                        }

                                        // If needsApproval is true, ensure the payment is pending (Status = 0)
                                        // Otherwise, ensure the payment is approved (Status = 1)
                                        if (needsApproval)
                                        {
                                            if (paymentStatus == 1)
                                            {
                                                // Update DB to mark pending
                                                reader.Close();
                                                using (var forceApprovalCmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                                                    UPDATE Payments 
                                                    SET Status = 0, 
                                                        UpdatedAt = GETDATE(),
                                                        Notes = CASE 
                                                            WHEN Notes IS NULL OR Notes = '' THEN @Note
                                                            ELSE Notes + ' | ' + @Note
                                                        END
                                                    WHERE Id = @PaymentId", connection))
                                                {
                                                    string note = "Requires approval";
                                                    if (model.DiscountAmount > 0) note = $"Discount applied - requires approval";
                                                    forceApprovalCmd.Parameters.AddWithValue("@PaymentId", paymentId);
                                                    forceApprovalCmd.Parameters.AddWithValue("@Note", note);
                                                    forceApprovalCmd.ExecuteNonQuery();
                                                }
                                                paymentStatus = 0; // Update local variable to reflect pending status
                                            }
                                        }
                                        else
                                        {
                                            // Ensure payment is approved if it was created as pending by payment method or DB defaults
                                            if (paymentStatus == 0)
                                            {
                                                reader.Close();
                                                using (var approveCmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                                                    UPDATE Payments 
                                                    SET Status = 1, 
                                                        UpdatedAt = GETDATE()
                                                    WHERE Id = @PaymentId", connection))
                                                {
                                                    approveCmd.Parameters.AddWithValue("@PaymentId", paymentId);
                                                    approveCmd.ExecuteNonQuery();
                                                }
                                                paymentStatus = 1;
                                            }
                                        }

                                        if (paymentStatus == 1) // Approved
                                        {
                                            TempData["SuccessMessage"] = "Payment processed successfully.";
                                        }
                                        else // Pending
                                        {
                                            if (model.DiscountAmount > 0 && discountApprovalRequired)
                                            {
                                                TempData["InfoMessage"] = $"Payment with discount of ₹{model.DiscountAmount:F2} requires approval. It has been saved as pending.";
                                            }
                                            else if (requiresCardInfo && cardPaymentApprovalRequired)
                                            {
                                                TempData["InfoMessage"] = "Card payment requires approval. It has been saved as pending.";
                                            }
                                            else
                                            {
                                                TempData["InfoMessage"] = "Payment requires approval. It has been saved as pending.";
                                            }
                                        }

                                        // If discount provided update order with proper GST recalculation
                                        if (model.DiscountAmount > 0)
                                        {
                                            if (!reader.IsClosed) reader.Close();
                                            using (var discountCmd = new Microsoft.Data.SqlClient.SqlCommand(@"
                                                -- Get current order values
                                                DECLARE @CurrentDiscount DECIMAL(18,2);
                                                DECLARE @CurrentSubtotal DECIMAL(18,2);
                                                DECLARE @CurrentTipAmount DECIMAL(18,2);
                                                
                                                SELECT @CurrentDiscount = ISNULL(DiscountAmount, 0),
                                                       @CurrentSubtotal = Subtotal,
                                                       @CurrentTipAmount = ISNULL(TipAmount, 0)
                                                FROM Orders 
                                                WHERE Id = @OrderId;
                                                
                                                -- Calculate new values based on discount applied to subtotal
                                                DECLARE @NewDiscountAmount DECIMAL(18,2) = @CurrentDiscount + @Disc;
                                                DECLARE @NetSubtotal DECIMAL(18,2) = @CurrentSubtotal - @NewDiscountAmount;
                                                DECLARE @NewGSTAmount DECIMAL(18,2) = ROUND(@NetSubtotal * @GSTPerc / 100, 2);
                                                DECLARE @NewTotalAmount DECIMAL(18,2) = @NetSubtotal + @NewGSTAmount + @CurrentTipAmount;
                                                
                                                -- Update order with recalculated amounts
                                                UPDATE Orders 
                                                SET DiscountAmount = @NewDiscountAmount, 
                                                    UpdatedAt = GETDATE(),
                                                    TaxAmount = @NewGSTAmount,
                                                    TotalAmount = @NewTotalAmount
                                                WHERE Id = @OrderId", connection))
                                            {
                                                discountCmd.Parameters.AddWithValue("@Disc", model.DiscountAmount);
                                                discountCmd.Parameters.AddWithValue("@OrderId", model.OrderId);
                                                discountCmd.Parameters.AddWithValue("@GSTPerc", paymentGstPercentage);
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
        
        // Approve Payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApprovePayment(int id)
        {
            try
            {
                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        UPDATE Payments 
                        SET Status = 1, 
                            UpdatedAt = GETDATE(),
                            ProcessedBy = @ProcessedBy,
                            ProcessedByName = @ProcessedByName
                        WHERE Id = @PaymentId AND Status = 0;
                        
                        SELECT @@ROWCOUNT AS RowsAffected, OrderId FROM Payments WHERE Id = @PaymentId;", connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", id);
                        command.Parameters.AddWithValue("@ProcessedBy", GetCurrentUserId());
                        command.Parameters.AddWithValue("@ProcessedByName", GetCurrentUserName());
                        
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = reader.GetInt32("RowsAffected");
                                int orderId = reader.GetInt32("OrderId");
                                
                                if (rowsAffected > 0)
                                {
                                    TempData["SuccessMessage"] = "Payment approved successfully.";
                                    
                                    // Check if this was from dashboard
                                    string returnUrl = Request.Headers["Referer"].ToString();
                                    if (returnUrl.Contains("/Payment/Dashboard"))
                                    {
                                        return RedirectToAction("Dashboard");
                                    }
                                    return RedirectToAction("Index", new { id = orderId });
                                }
                                else
                                {
                                    TempData["ErrorMessage"] = "Payment not found or already processed.";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payment {PaymentId}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the payment.";
            }
            
            return RedirectToAction("Dashboard");
        }
        
        // Reject Payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectPayment(int id, string reason = null)
        {
            try
            {
                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        UPDATE Payments 
                        SET Status = 2, 
                            UpdatedAt = GETDATE(),
                            ProcessedBy = @ProcessedBy,
                            ProcessedByName = @ProcessedByName,
                            Notes = CASE WHEN @Reason IS NOT NULL THEN 
                                CASE WHEN Notes IS NOT NULL THEN Notes + ' | Rejected: ' + @Reason 
                                ELSE 'Rejected: ' + @Reason END 
                                ELSE Notes END
                        WHERE Id = @PaymentId AND Status = 0;
                        
                        SELECT @@ROWCOUNT AS RowsAffected, OrderId FROM Payments WHERE Id = @PaymentId;", connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", id);
                        command.Parameters.AddWithValue("@ProcessedBy", GetCurrentUserId());
                        command.Parameters.AddWithValue("@ProcessedByName", GetCurrentUserName());
                        command.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(reason) ? (object)DBNull.Value : reason);
                        
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = reader.GetInt32("RowsAffected");
                                int orderId = reader.GetInt32("OrderId");
                                
                                if (rowsAffected > 0)
                                {
                                    TempData["SuccessMessage"] = "Payment rejected successfully.";
                                    
                                    // Check if this was from dashboard
                                    string returnUrl = Request.Headers["Referer"].ToString();
                                    if (returnUrl.Contains("/Payment/Dashboard"))
                                    {
                                        return RedirectToAction("Dashboard");
                                    }
                                    return RedirectToAction("Index", new { id = orderId });
                                }
                                else
                                {
                                    TempData["ErrorMessage"] = "Payment not found or already processed.";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment {PaymentId}", id);
                TempData["ErrorMessage"] = "An error occurred while rejecting the payment.";
            }
            
            return RedirectToAction("Dashboard");
        }
        
        // Approve Payment AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApprovePaymentAjax(int id)
        {
            try
            {
                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        UPDATE Payments 
                        SET Status = 1, 
                            UpdatedAt = GETDATE(),
                            ProcessedBy = @ProcessedBy,
                            ProcessedByName = @ProcessedByName
                        WHERE Id = @PaymentId AND Status = 0;
                        
                        SELECT @@ROWCOUNT AS RowsAffected, OrderId, 
                               (SELECT OrderNumber FROM Orders WHERE Id = OrderId) AS OrderNumber
                        FROM Payments WHERE Id = @PaymentId;", connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", id);
                        command.Parameters.AddWithValue("@ProcessedBy", GetCurrentUserId());
                        command.Parameters.AddWithValue("@ProcessedByName", GetCurrentUserName());
                        
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = reader.GetInt32("RowsAffected");
                                int orderId = reader.GetInt32("OrderId");
                                string orderNumber = reader["OrderNumber"].ToString();
                                
                                if (rowsAffected > 0)
                                {
                                    return Json(new { 
                                        success = true, 
                                        message = $"Payment for order #{orderNumber} approved successfully.",
                                        orderId = orderId,
                                        orderNumber = orderNumber
                                    });
                                }
                                else
                                {
                                    return Json(new { 
                                        success = false, 
                                        message = "Payment not found or already processed." 
                                    });
                                }
                            }
                            else
                            {
                                return Json(new { 
                                    success = false, 
                                    message = "Payment not found." 
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving payment {PaymentId}", id);
                return Json(new { 
                    success = false, 
                    message = "An error occurred while approving the payment." 
                });
            }
        }
        
        // Reject Payment AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectPaymentAjax(int id, string reason = null)
        {
            try
            {
                using (Microsoft.Data.SqlClient.SqlConnection connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                        UPDATE Payments 
                        SET Status = 2, 
                            UpdatedAt = GETDATE(),
                            ProcessedBy = @ProcessedBy,
                            ProcessedByName = @ProcessedByName,
                            Notes = CASE WHEN @Reason IS NOT NULL THEN 
                                CASE WHEN Notes IS NOT NULL THEN Notes + ' | Rejected: ' + @Reason 
                                ELSE 'Rejected: ' + @Reason END 
                                ELSE Notes END
                        WHERE Id = @PaymentId AND Status = 0;
                        
                        SELECT @@ROWCOUNT AS RowsAffected, OrderId, 
                               (SELECT OrderNumber FROM Orders WHERE Id = OrderId) AS OrderNumber
                        FROM Payments WHERE Id = @PaymentId;", connection))
                    {
                        command.Parameters.AddWithValue("@PaymentId", id);
                        command.Parameters.AddWithValue("@ProcessedBy", GetCurrentUserId());
                        command.Parameters.AddWithValue("@ProcessedByName", GetCurrentUserName());
                        command.Parameters.AddWithValue("@Reason", string.IsNullOrEmpty(reason) ? (object)DBNull.Value : reason);
                        
                        using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int rowsAffected = reader.GetInt32("RowsAffected");
                                int orderId = reader.GetInt32("OrderId");
                                string orderNumber = reader["OrderNumber"].ToString();
                                
                                if (rowsAffected > 0)
                                {
                                    return Json(new { 
                                        success = true, 
                                        message = $"Payment for order #{orderNumber} rejected successfully.",
                                        orderId = orderId,
                                        orderNumber = orderNumber
                                    });
                                }
                                else
                                {
                                    return Json(new { 
                                        success = false, 
                                        message = "Payment not found or already processed." 
                                    });
                                }
                            }
                            else
                            {
                                return Json(new { 
                                    success = false, 
                                    message = "Payment not found." 
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment {PaymentId}", id);
                return Json(new { 
                    success = false, 
                    message = "An error occurred while rejecting the payment." 
                });
            }
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
                        -- Sum of GST amounts from all approved payments
                        ISNULL(SUM(p.GSTAmount), 0) AS GSTAmount,
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
                                GSTAmount = Convert.ToDecimal(reader["GSTAmount"]),
                                PaymentDate = reader.GetDateTime("PaymentDate"),
                                OrderStatus = reader.GetInt32("OrderStatus"),
                                OrderStatusDisplay = reader.GetString("OrderStatusDisplay")
                            });
                        }
                    }
                }

                // Get pending payments that need approval
                using (Microsoft.Data.SqlClient.SqlCommand command = new Microsoft.Data.SqlClient.SqlCommand(@"
                    SELECT 
                        p.Id AS PaymentId,
                        p.OrderId,
                        o.OrderNumber,
                        ISNULL(t.TableName, 'Takeout/Delivery') AS TableName,
                        pm.Name AS PaymentMethodName,
                        pm.DisplayName AS PaymentMethodDisplay,
                        p.Amount,
                        p.TipAmount,
                        p.CreatedAt,
                        p.ProcessedByName,
                        p.ReferenceNumber,
                        p.LastFourDigits,
                        p.CardType,
                        p.Notes,
                        ISNULL(p.DiscAmount, 0) AS DiscountAmount,
                        (p.Amount + ISNULL(p.DiscAmount, 0)) AS OriginalAmount
                    FROM Payments p
                    INNER JOIN Orders o ON p.OrderId = o.Id
                    INNER JOIN PaymentMethods pm ON p.PaymentMethodId = pm.Id
                    LEFT JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id
                    LEFT JOIN Tables t ON tt.TableId = t.Id
                    WHERE p.Status = 0 -- Pending approval
                    ORDER BY p.CreatedAt ASC", connection))
                {
                    using (Microsoft.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.PendingPayments.Add(new PendingPaymentItem
                            {
                                PaymentId = reader.GetInt32("PaymentId"),
                                OrderId = reader.GetInt32("OrderId"),
                                OrderNumber = reader.GetString("OrderNumber"),
                                TableName = GetMergedTableDisplayName((int)reader["OrderId"], reader.GetString("TableName")),
                                PaymentMethodName = reader.GetString("PaymentMethodName"),
                                PaymentMethodDisplay = reader.GetString("PaymentMethodDisplay"),
                                Amount = reader.GetDecimal("Amount"),
                                TipAmount = reader.GetDecimal("TipAmount"),
                                CreatedAt = reader.GetDateTime("CreatedAt"),
                                ProcessedByName = reader.IsDBNull("ProcessedByName") ? "" : reader.GetString("ProcessedByName"),
                                ReferenceNumber = reader.IsDBNull("ReferenceNumber") ? "" : reader.GetString("ReferenceNumber"),
                                LastFourDigits = reader.IsDBNull("LastFourDigits") ? "" : reader.GetString("LastFourDigits"),
                                CardType = reader.IsDBNull("CardType") ? "" : reader.GetString("CardType"),
                                Notes = reader.IsDBNull("Notes") ? "" : reader.GetString("Notes"),
                                DiscountAmount = reader.GetDecimal("DiscountAmount"),
                                OriginalAmount = reader.GetDecimal("OriginalAmount")
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
                        
                        // Variables to store GST information from the most recent payment
                        decimal totalGSTFromPayments = 0m;
                        decimal totalCGSTFromPayments = 0m;
                        decimal totalSGSTFromPayments = 0m;
                        decimal gstPercentageFromPayments = 5.0m; // Default fallback
                        
                        while (reader.Read())
                        {
                            var payment = new Payment
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
                                CreatedAt = reader.GetDateTime(13),
                                // Read GST information from database
                                GSTAmount = reader.IsDBNull(14) ? null : reader.GetDecimal(14),
                                CGSTAmount = reader.IsDBNull(15) ? null : reader.GetDecimal(15),
                                SGSTAmount = reader.IsDBNull(16) ? null : reader.GetDecimal(16),
                                DiscAmount = reader.IsDBNull(17) ? null : reader.GetDecimal(17),
                                GST_Perc = reader.IsDBNull(18) ? null : reader.GetDecimal(18),
                                CGST_Perc = reader.IsDBNull(19) ? null : reader.GetDecimal(19),
                                SGST_Perc = reader.IsDBNull(20) ? null : reader.GetDecimal(20),
                                Amount_ExclGST = reader.IsDBNull(21) ? null : reader.GetDecimal(21)
                            };
                            
                            model.Payments.Add(payment);
                            
                            // If this is an approved payment with GST data, accumulate GST information
                            if (payment.Status == 1 && payment.GSTAmount.HasValue)
                            {
                                totalGSTFromPayments += payment.GSTAmount.Value;
                                totalCGSTFromPayments += payment.CGSTAmount ?? 0m;
                                totalSGSTFromPayments += payment.SGSTAmount ?? 0m;
                                if (payment.GST_Perc.HasValue)
                                {
                                    gstPercentageFromPayments = payment.GST_Perc.Value;
                                }
                            }
                        }
                        
                        // Set GST information from payments data if available, otherwise calculate
                        if (totalGSTFromPayments > 0)
                        {
                            model.GSTPercentage = gstPercentageFromPayments;
                            model.CGSTAmount = totalCGSTFromPayments;
                            model.SGSTAmount = totalSGSTFromPayments;
                            
                            // Update TaxAmount to match total GST from payments
                            if (model.TaxAmount == 0)
                            {
                                model.TaxAmount = totalGSTFromPayments;
                                // Recalculate total amount to include GST
                                model.TotalAmount = model.Subtotal + model.TaxAmount - model.DiscountAmount + model.TipAmount;
                                model.RemainingAmount = model.TotalAmount - model.PaidAmount;
                            }
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
                
                // Fallback GST calculation if no payment GST data available
                if (model.GSTPercentage == 0 || (model.CGSTAmount == 0 && model.SGSTAmount == 0))
                {
                    try
                    {
                        using (Microsoft.Data.SqlClient.SqlCommand gstCmd = new Microsoft.Data.SqlClient.SqlCommand(
                            "SELECT DefaultGSTPercentage FROM dbo.RestaurantSettings", connection))
                        {
                            var result = gstCmd.ExecuteScalar();
                            if (result != null && result != DBNull.Value)
                            {
                                model.GSTPercentage = Convert.ToDecimal(result);
                            }
                            else
                            {
                                model.GSTPercentage = 5.0m;
                            }
                        }
                        
                        decimal gstAmount = model.TaxAmount > 0 ? model.TaxAmount : 
                            Math.Round(model.Subtotal * model.GSTPercentage / 100m, 2, MidpointRounding.AwayFromZero);
                        
                        // Update TaxAmount if it was 0 (calculated GST)
                        if (model.TaxAmount == 0 && gstAmount > 0)
                        {
                            model.TaxAmount = gstAmount;
                        }
                        
                        model.CGSTAmount = Math.Round(gstAmount / 2m, 2, MidpointRounding.AwayFromZero);
                        model.SGSTAmount = gstAmount - model.CGSTAmount;
                        
                        // Recalculate total amount to include GST
                        model.TotalAmount = model.Subtotal + model.TaxAmount - model.DiscountAmount + model.TipAmount;
                        model.RemainingAmount = model.TotalAmount - model.PaidAmount;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error calculating fallback GST for order {OrderId}", model.OrderId);
                        model.GSTPercentage = 5.0m;
                        decimal fallbackGst = Math.Round(model.Subtotal * 0.05m, 2, MidpointRounding.AwayFromZero);
                        
                        // Update TaxAmount with calculated GST
                        if (model.TaxAmount == 0)
                        {
                            model.TaxAmount = fallbackGst;
                        }
                        
                        model.CGSTAmount = Math.Round(fallbackGst / 2m, 2, MidpointRounding.AwayFromZero);
                        model.SGSTAmount = fallbackGst - model.CGSTAmount;
                        
                        // Recalculate total amount to include GST
                        model.TotalAmount = model.Subtotal + model.TaxAmount - model.DiscountAmount + model.TipAmount;
                        model.RemainingAmount = model.TotalAmount - model.PaidAmount;
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
                
                // Get restaurant settings for bill header
                RestaurantSettings settings = null;
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand("SELECT * FROM dbo.RestaurantSettings", connection))
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
