using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace RestaurantManagementSystem.Controllers
{
    public class OrderControllerDedupeFix
    {
        // Updated implementation of UpdateMultipleOrderItems that handles item deduplication
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
                            // Step 1: Get all existing items for this order
                            var existingMenuItemIds = new Dictionary<int, int>(); // MenuItemId -> OrderItemId
                            
                            using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                SELECT Id, MenuItemId
                                FROM OrderItems
                                WHERE OrderId = @OrderId AND Status < 5", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@OrderId", orderId);
                                
                                using (var reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int orderItemId = reader.GetInt32(0);
                                        int menuItemId = reader.GetInt32(1);
                                        existingMenuItemIds[menuItemId] = orderItemId;
                                    }
                                }
                            }
                            
                            // Step 2: Process existing items
                            var existingItems = items.Where(i => !i.IsNew).ToList();
                            
                            foreach (var item in existingItems)
                            {
                                if (item.Quantity < 1)
                                {
                                    transaction.Rollback();
                                    return new JsonResult(new { 
                                        success = false, 
                                        message = $"Item #{item.OrderItemId}: Quantity must be at least 1." 
                                    });
                                }
                                
                                // Update the item
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
                            
                            // Step 3: Process new items with deduplication
                            var newItems = items.Where(i => i.IsNew).ToList();
                            var itemsToAdd = new List<OrderItemUpdateModel>();
                            var itemsToUpdate = new List<OrderItemUpdateModel>();
                            
                            // Group by MenuItemId for deduplication
                            var groupedNewItems = newItems
                                .GroupBy(i => i.MenuItemId)
                                .Select(g => new {
                                    MenuItemId = g.Key,
                                    TotalQuantity = g.Sum(i => i.Quantity),
                                    // Use the first item's special instructions or combine them
                                    SpecialInstructions = string.Join(", ", g.Select(i => i.SpecialInstructions).Where(s => !string.IsNullOrEmpty(s)).Distinct())
                                })
                                .ToList();
                            
                            foreach (var groupedItem in groupedNewItems)
                            {
                                // Check if this menu item already exists in the order
                                if (existingMenuItemIds.TryGetValue(groupedItem.MenuItemId.Value, out int existingOrderItemId))
                                {
                                    // Update existing item instead of adding new one
                                    // First, get current quantity
                                    int currentQty = 0;
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                                        "SELECT Quantity FROM OrderItems WHERE Id = @OrderItemId", 
                                        connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@OrderItemId", existingOrderItemId);
                                        var result = command.ExecuteScalar();
                                        if (result != null)
                                        {
                                            currentQty = Convert.ToInt32(result);
                                        }
                                    }
                                    
                                    // Update quantity (add to existing)
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                        UPDATE OrderItems 
                                        SET Quantity = Quantity + @AdditionalQuantity, 
                                            Subtotal = UnitPrice * (Quantity + @AdditionalQuantity),
                                            SpecialInstructions = CASE 
                                                WHEN @SpecialInstructions = '' THEN SpecialInstructions 
                                                WHEN SpecialInstructions IS NULL OR SpecialInstructions = '' THEN @SpecialInstructions 
                                                ELSE SpecialInstructions + ', ' + @SpecialInstructions END
                                        WHERE Id = @OrderItemId", connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@AdditionalQuantity", groupedItem.TotalQuantity);
                                        command.Parameters.AddWithValue("@OrderItemId", existingOrderItemId);
                                        command.Parameters.AddWithValue("@SpecialInstructions", 
                                            string.IsNullOrEmpty(groupedItem.SpecialInstructions) ? 
                                            string.Empty : groupedItem.SpecialInstructions);
                                        
                                        command.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    // New item - get the price
                                    decimal unitPrice = 0;
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(
                                        "SELECT Price FROM MenuItems WHERE Id = @MenuItemId", 
                                        connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@MenuItemId", groupedItem.MenuItemId.Value);
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
                                                message = $"Menu item {groupedItem.MenuItemId} not found." 
                                            });
                                        }
                                    }
                                    
                                    // Insert new item
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(@"
                                        INSERT INTO OrderItems 
                                        (OrderId, MenuItemId, Quantity, UnitPrice, Subtotal, Status, SpecialInstructions, CreatedAt) 
                                        VALUES 
                                        (@OrderId, @MenuItemId, @Quantity, @UnitPrice, @Subtotal, 0, @SpecialInstructions, GETDATE());
                                        
                                        SELECT SCOPE_IDENTITY();", connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@OrderId", orderId);
                                        command.Parameters.AddWithValue("@MenuItemId", groupedItem.MenuItemId.Value);
                                        command.Parameters.AddWithValue("@Quantity", groupedItem.TotalQuantity);
                                        command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                                        command.Parameters.AddWithValue("@Subtotal", unitPrice * groupedItem.TotalQuantity);
                                        command.Parameters.AddWithValue("@SpecialInstructions", 
                                            string.IsNullOrEmpty(groupedItem.SpecialInstructions) ? 
                                            DBNull.Value : (object)groupedItem.SpecialInstructions);
                                        
                                        // Get the new item ID
                                        var newItemId = Convert.ToInt32(command.ExecuteScalar());
                                        
                                        // Add this to our existing items map in case we have more of the same item
                                        existingMenuItemIds[groupedItem.MenuItemId.Value] = newItemId;
                                    }
                                }
                            }
                            
                            // Step 4: Update order totals
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

    // The model class is the same as before
}