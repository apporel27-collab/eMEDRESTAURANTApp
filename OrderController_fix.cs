using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace RestaurantManagementSystem.Controllers
{
    // This is a sample controller implementation of CancelOrderItem with improved error handling
    // that safely handles the case where the OrderItemModifiers table might not exist
    
    public class OrderControllerSample
    {
        private readonly string _connectionString;
        
        public OrderControllerSample(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        
        // Sample implementation of CancelOrderItem with robust table checking
        public IActionResult CancelOrderItem(int orderId, int orderItemId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // First check if the order item has already been sent to kitchen
                    using (SqlCommand checkCommand = new SqlCommand(@"
                        SELECT Status 
                        FROM OrderItems 
                        WHERE Id = @OrderItemId AND OrderId = @OrderId", connection))
                    {
                        checkCommand.Parameters.AddWithValue("@OrderItemId", orderItemId);
                        checkCommand.Parameters.AddWithValue("@OrderId", orderId);

                        var status = (int?)checkCommand.ExecuteScalar();

                        if (status == null)
                        {
                            // Return error - item not found
                            return new RedirectResult($"/Order/Details/{orderId}");
                        }

                        if (status > 0) // If already sent to kitchen
                        {
                            // Return error - item already sent to kitchen
                            return new RedirectResult($"/Order/Details/{orderId}");
                        }
                    }

                    // Begin transaction since we'll be updating multiple tables
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update order item status to cancelled
                            using (SqlCommand updateCommand = new SqlCommand(@"
                                UPDATE OrderItems 
                                SET Status = 5, -- 5 = Cancelled
                                    UpdatedAt = GETDATE() 
                                WHERE Id = @OrderItemId AND OrderId = @OrderId", connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@OrderItemId", orderItemId);
                                updateCommand.Parameters.AddWithValue("@OrderId", orderId);
                                updateCommand.ExecuteNonQuery();
                            }

                            // Check if OrderItemModifiers table exists before trying to delete from it
                            using (SqlCommand checkTableCommand = new SqlCommand(@"
                                SELECT 
                                    CASE 
                                        WHEN OBJECT_ID('OrderItemModifiers', 'U') IS NOT NULL THEN 1
                                        WHEN OBJECT_ID('OrderItem_Modifiers', 'U') IS NOT NULL THEN 2
                                        ELSE 0
                                    END", connection, transaction))
                            {
                                int tableCheck = Convert.ToInt32(checkTableCommand.ExecuteScalar());
                                
                                // Only try to delete if one of the tables exists
                                if (tableCheck > 0)
                                {
                                    string tableName = tableCheck == 1 ? "OrderItemModifiers" : "OrderItem_Modifiers";
                                    
                                    using (SqlCommand deleteModifiersCommand = new SqlCommand($@"
                                        DELETE FROM {tableName} 
                                        WHERE OrderItemId = @OrderItemId", connection, transaction))
                                    {
                                        deleteModifiersCommand.Parameters.AddWithValue("@OrderItemId", orderItemId);
                                        deleteModifiersCommand.ExecuteNonQuery();
                                    }
                                }
                            }

                            // Recalculate order totals
                            using (SqlCommand updateOrderCommand = new SqlCommand(@"
                                UPDATE o
                                SET o.Subtotal = (
                                        SELECT ISNULL(SUM(oi.Subtotal), 0)
                                        FROM OrderItems oi
                                        WHERE oi.OrderId = o.Id
                                          AND oi.Status != 5 -- Not cancelled
                                    ),
                                    o.TaxAmount = (
                                        SELECT ISNULL(SUM(oi.Subtotal), 0) * 0.10 -- 10% tax
                                        FROM OrderItems oi
                                        WHERE oi.OrderId = o.Id
                                          AND oi.Status != 5 -- Not cancelled
                                    ),
                                    o.UpdatedAt = GETDATE()
                                FROM Orders o
                                WHERE o.Id = @OrderId;

                                -- Update total amount
                                UPDATE Orders
                                SET TotalAmount = Subtotal + TaxAmount - DiscountAmount + TipAmount
                                WHERE Id = @OrderId;", connection, transaction))
                            {
                                updateOrderCommand.Parameters.AddWithValue("@OrderId", orderId);
                                updateOrderCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            // Success message
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Error cancelling order item: " + ex.Message);
                        }
                    }
                }

                return new RedirectResult($"/Order/Details/{orderId}");
            }
            catch (Exception ex)
            {
                // Error handling
                return new RedirectResult($"/Order/Details/{orderId}");
            }
        }

        // Dummy IActionResult for sample
        private class RedirectResult : IActionResult
        {
            public string Url { get; }
            public RedirectResult(string url) { Url = url; }
            public Task ExecuteResultAsync(ActionContext context) => Task.CompletedTask;
        }
    }
}
