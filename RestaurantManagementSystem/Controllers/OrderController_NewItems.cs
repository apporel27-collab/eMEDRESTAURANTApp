using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace RestaurantManagementSystem.Controllers
{
    public partial class OrderController : Controller
    {
        // Model for new items added from the frontend
        public class NewOrderItemModel
        {
            public int MenuItemId { get; set; }
            public string MenuItemName { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string SpecialInstructions { get; set; }
        }
        
        // Deprecated duplicate of JSON-based UpdateMultipleOrderItems in OrderController.cs.
        // Retained only temporarily to show previous form+new items mixed implementation.
        // Marked as NonAction to prevent routing conflicts that caused 500 errors when
        // the framework attempted to bind JSON payload to this signature.
        [NonAction]
        public IActionResult UpdateMultipleOrderItems_Deprecated(int orderId, List<NewOrderItemModel> NewItems)
        {
            // Error checking
            if (orderId <= 0)
            {
                return Json(new { success = false, message = "Invalid order ID." });
            }
            
            try
            {
                // Check if the order exists
                bool orderExists = false;
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT COUNT(1) FROM Orders WHERE Id = @OrderId", connection))
                    {
                        command.Parameters.AddWithValue("@OrderId", orderId);
                        orderExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                    
                    if (!orderExists)
                    {
                        return Json(new { success = false, message = "Order not found." });
                    }
                    
                    // Process the form submission
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Process existing items (submitted through standard form fields)
                            // We need to find all the existing items from the form submission
                            var formCollection = Request.Form;
                            var existingItems = new List<OrderItemUpdateModel>();
                            
                            // Look for hidden form fields with pattern orderItemId[], quantity[], etc.
                            foreach (var key in formCollection.Keys)
                            {
                                if (key.StartsWith("orderItem_") && key.EndsWith("_Id"))
                                {
                                    // Extract the index part from the key (e.g., "orderItem_0_Id" -> "0")
                                    var indexStr = key.Replace("orderItem_", "").Replace("_Id", "");
                                    
                                    if (int.TryParse(formCollection[key].ToString(), out int orderItemId))
                                    {
                                        // Now find the corresponding quantity and instructions using the same index
                                        var quantityKey = $"orderItem_{indexStr}_Quantity";
                                        var instructionsKey = $"orderItem_{indexStr}_SpecialInstructions";
                                        
                                        if (formCollection.ContainsKey(quantityKey) &&
                                            int.TryParse(formCollection[quantityKey].ToString(), out int quantity))
                                        {
                                            var specialInstructions = formCollection.ContainsKey(instructionsKey) ?
                                                                     formCollection[instructionsKey].ToString() : string.Empty;
                                            
                                            existingItems.Add(new OrderItemUpdateModel
                                            {
                                                OrderItemId = orderItemId,
                                                Quantity = quantity,
                                                SpecialInstructions = specialInstructions,
                                                IsNew = false
                                            });
                                        }
                                    }
                                }
                            }
                            
                            // Process existing items first
                            foreach (var item in existingItems)
                            {
                                if (item.Quantity < 1)
                                {
                                    // Instead of rolling back, we'll just skip invalid items
                                    continue;
                                }
                                
                                using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                    UPDATE OrderItems 
                                    SET Quantity = @Quantity, 
                                        Subtotal = UnitPrice * @Quantity, 
                                        SpecialInstructions = @SpecialInstructions 
                                    WHERE Id = @OrderItemId AND OrderId = @OrderId", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@OrderId", orderId);
                                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    command.Parameters.AddWithValue("@OrderItemId", item.OrderItemId);
                                    command.Parameters.AddWithValue("@SpecialInstructions", 
                                        string.IsNullOrEmpty(item.SpecialInstructions) ? DBNull.Value : (object)item.SpecialInstructions);
                                    command.ExecuteNonQuery();
                                }
                            }
                            
                            // Process new items from the NewItems collection
                            if (NewItems != null && NewItems.Count > 0)
                            {
                                foreach (var item in NewItems)
                                {
                                    if (item.Quantity < 1 || item.MenuItemId <= 0)
                                    {
                                        // Skip invalid items
                                        continue;
                                    }
                                    
                                    // Check if this menu item exists
                                    bool menuItemExists = false;
                                    decimal actualPrice = 0;
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                                        "SELECT Price FROM MenuItems WHERE Id = @MenuItemId", connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@MenuItemId", item.MenuItemId);
                                        var result = command.ExecuteScalar();
                                        if (result != null)
                                        {
                                            menuItemExists = true;
                                            actualPrice = Convert.ToDecimal(result);
                                        }
                                    }
                                    
                                    if (!menuItemExists)
                                    {
                                        // Skip if menu item doesn't exist
                                        continue;
                                    }
                                    
                                    // Insert the new order item
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                        INSERT INTO OrderItems 
                                        (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, SpecialInstructions, CreatedAt) 
                                        VALUES 
                                        (@OrderId, @MenuItemId, @Quantity, @UnitPrice, @Subtotal, 0, @SpecialInstructions, GETDATE());", 
                                        connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@OrderId", orderId);
                                        command.Parameters.AddWithValue("@MenuItemId", item.MenuItemId);
                                        command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                        command.Parameters.AddWithValue("@UnitPrice", actualPrice);
                                        command.Parameters.AddWithValue("@Subtotal", actualPrice * item.Quantity);
                                        command.Parameters.AddWithValue("@SpecialInstructions", 
                                            string.IsNullOrEmpty(item.SpecialInstructions) ? DBNull.Value : (object)item.SpecialInstructions);
                                        
                                        command.ExecuteNonQuery();
                                    }
                                }
                            }
                            
                            // Update order totals
                            using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                UPDATE Orders
                                SET Subtotal = (SELECT ISNULL(SUM(Subtotal), 0) FROM OrderItems WHERE OrderId = @OrderId),
                                    TotalAmount = (SELECT ISNULL(SUM(Subtotal), 0) FROM OrderItems WHERE OrderId = @OrderId) + 
                                                 ISNULL(TaxAmount, 0) + ISNULL(TipAmount, 0) - ISNULL(DiscountAmount, 0),
                                    LastModifiedAt = GETDATE()
                                WHERE Id = @OrderId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.ExecuteNonQuery();
                            }
                            
                            transaction.Commit();
                            
                            // Return success and redirect to the details page
                            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            {
                                return Json(new { success = true, message = "Order updated successfully." });
                            }
                            else
                            {
                                TempData["SuccessMessage"] = "Order updated successfully.";
                                return RedirectToAction("Details", new { id = orderId });
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            
                            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            {
                                return Json(new { success = false, message = "Error updating order: " + ex.Message });
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Error updating order: " + ex.Message;
                                return RedirectToAction("Details", new { id = orderId });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Error processing request: " + ex.Message });
                }
                else
                {
                    TempData["ErrorMessage"] = "Error processing request: " + ex.Message;
                    return RedirectToAction("Details", new { id = orderId });
                }
            }
        }
    }
}