using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantManagementSystem.Controllers
{
    public class OrderControllerFix
    {
        // Method to demonstrate the correct implementation of UpdateMultipleOrderItems
        // This is just for documentation - the actual implementation should be in the OrderController
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMultipleOrderItems(int orderId, [FromBody] List<OrderItemUpdateModel> items)
        {
            // Input validation
            if (items == null || !items.Any())
            {
                return new JsonResult(new { success = false, message = "No items to update." });
            }
            
            try
            {
                // Open database connection and begin transaction
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection("YOUR_CONNECTION_STRING"))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Separate existing and new items
                            var existingItems = items.Where(i => !i.IsNew).ToList();
                            var newItems = items.Where(i => i.IsNew).ToList();
                            
                            // Process existing items first
                            foreach (var item in existingItems)
                            {
                                // Validate quantity
                                if (item.Quantity < 1)
                                {
                                    transaction.Rollback();
                                    return new JsonResult(new { 
                                        success = false, 
                                        message = $"Item #{item.OrderItemId}: Quantity must be at least 1." 
                                    });
                                }
                                
                                // Update the item in the database
                                using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                    UPDATE OrderItems 
                                    SET Quantity = @Quantity, 
                                        Subtotal = UnitPrice * @Quantity, 
                                        SpecialInstructions = @SpecialInstructions 
                                    WHERE Id = @OrderItemId", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    command.Parameters.AddWithValue("@OrderItemId", item.OrderItemId);
                                    command.Parameters.AddWithValue("@SpecialInstructions", 
                                        string.IsNullOrEmpty(item.SpecialInstructions) ? 
                                        DBNull.Value : (object)item.SpecialInstructions);
                                    
                                    command.ExecuteNonQuery();
                                }
                            }
                            
                            // Now process new items
                            foreach (var item in newItems)
                            {
                                // Validate item data
                                if (item.Quantity < 1 || !item.MenuItemId.HasValue)
                                {
                                    transaction.Rollback();
                                    return new JsonResult(new { 
                                        success = false, 
                                        message = "Invalid new item data." 
                                    });
                                }
                                
                                // Get menu item price
                                decimal unitPrice = 0;
                                using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                                    "SELECT Price FROM MenuItems WHERE Id = @MenuItemId", 
                                    connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@MenuItemId", item.MenuItemId.Value);
                                    var result = command.ExecuteScalar();
                                    if (result != null)
                                    {
                                        unitPrice = Convert.ToDecimal(result);
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        return new JsonResult(new { 
                                            success = false, 
                                            message = $"Menu item {item.MenuItemId} not found." 
                                        });
                                    }
                                }
                                
                                // Insert new order item
                                using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                    INSERT INTO OrderItems 
                                    (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, SpecialInstructions, CreatedAt) 
                                    VALUES 
                                    (@OrderId, @MenuItemId, @Quantity, @UnitPrice, @Subtotal, 0, @SpecialInstructions, GETDATE());
                                    
                                    SELECT SCOPE_IDENTITY();", connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@OrderId", orderId);
                                    command.Parameters.AddWithValue("@MenuItemId", item.MenuItemId.Value);
                                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                                    command.Parameters.AddWithValue("@Subtotal", unitPrice * item.Quantity);
                                    command.Parameters.AddWithValue("@SpecialInstructions", 
                                        string.IsNullOrEmpty(item.SpecialInstructions) ? 
                                        DBNull.Value : (object)item.SpecialInstructions);
                                    
                                    // Get the new item ID
                                    var newItemId = Convert.ToInt32(command.ExecuteScalar());
                                    item.OrderItemId = newItemId; // Update the model with the real ID
                                }
                            }
                            
                            // Update order totals
                            using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                UPDATE Orders
                                SET Subtotal = (SELECT SUM(Subtotal) FROM OrderItems WHERE OrderId = @OrderId),
                                    TotalAmount = (SELECT SUM(Subtotal) FROM OrderItems WHERE OrderId = @OrderId) + 
                                                 ISNULL(TaxAmount,0) + ISNULL(TipAmount,0) - ISNULL(DiscountAmount,0)
                                WHERE Id = @OrderId", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                command.ExecuteNonQuery();
                            }
                            
                            // Commit transaction
                            transaction.Commit();
                            return new JsonResult(new { success = true, message = "All items updated successfully." });
                        }
                        catch (Exception ex)
                        {
                            // Rollback on error
                            transaction.Rollback();
                            return new JsonResult(new { success = false, message = "Error updating items: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Error updating items: " + ex.Message });
            }
        }
    }

    // Model class for bulk updates
    public class OrderItemUpdateModel
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
        public string SpecialInstructions { get; set; }
        public bool IsNew { get; set; }
        public int? MenuItemId { get; set; }  // For new items
        public int? TempId { get; set; }      // For tracking new items client-side
    }
}