#!/bin/bash
# This script replaces the problematic GetOrderDetails method with a fixed version
cat > /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/fixed_method.cs << 'EOL'
        private OrderViewModel GetOrderDetails(int id)
        {
            OrderViewModel order = null;
            
            // Use separate connections for different data readers to avoid nested DataReader issues
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get order details
                // First check if the UpdatedAt column exists in the Orders table
                bool hasUpdatedAtColumn = ColumnExistsInTable("Orders", "UpdatedAt");
                
                // Build the SQL query based on column existence
                string selectSql = hasUpdatedAtColumn 
                    ? @"SELECT 
                        o.Id,
                        o.OrderNumber,
                        o.TableTurnoverId,
                        o.OrderType,
                        o.Status,
                        o.UserId,
                        CONCAT(u.FirstName, ' ', ISNULL(u.LastName, '')) AS ServerName,
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
                        o.CompletedAt,"
                    : @"SELECT 
                        o.Id,
                        o.OrderNumber,
                        o.TableTurnoverId,
                        o.OrderType,
                        o.Status,
                        o.UserId,
                        CONCAT(u.FirstName, ' ', ISNULL(u.LastName, '')) AS ServerName,
                        o.CustomerName,
                        o.CustomerPhone,
                        o.Subtotal,
                        o.TaxAmount,
                        o.TipAmount,
                        o.DiscountAmount,
                        o.TotalAmount,
                        o.SpecialInstructions,
                        o.CreatedAt,
                        o.CreatedAt AS UpdatedAt, -- Use CreatedAt as a fallback
                        o.CompletedAt,";

                using (SqlCommand command = new SqlCommand(selectSql + @"
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
                                0 => "Dine In",
                                1 => "Take Out",
                                2 => "Delivery",
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
                                UpdatedAt = reader.GetDateTime(16), // We've handled this in the SQL query
                                CompletedAt = reader.IsDBNull(17) ? null : (DateTime?)reader.GetDateTime(17),
                                TableName = reader.IsDBNull(18) ? null : reader.GetString(18),
                                GuestName = reader.IsDBNull(19) ? null : reader.GetString(19),
                                Items = new List<OrderItemViewModel>(),
                                KitchenTickets = new List<KitchenTicketViewModel>(),
                                AvailableCourses = new List<CourseType>()
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
            }
                
            // Get order item modifiers using separate connections for each item
            foreach (var item in order.Items)
            {
                // Check which version of the table exists (with or without underscore)
                string orderItemModifiersTable = GetCorrectTableName("OrderItemModifiers", "OrderItem_Modifiers");
                
                if (!string.IsNullOrEmpty(orderItemModifiersTable))
                {
                    // Use a separate connection for modifiers to avoid DataReader issues
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        
                        string modifiersQuery = $@"
                            SELECT 
                                oim.Id,
                                oim.ModifierId,
                                m.Name AS ModifierName,
                                oim.Price
                            FROM {orderItemModifiersTable} oim
                            INNER JOIN Modifiers m ON oim.ModifierId = m.Id
                            WHERE oim.OrderItemId = @OrderItemId";
                            
                        using (SqlCommand command = new SqlCommand(modifiersQuery, connection))
                        {
                            command.Parameters.AddWithValue("@OrderItemId", item.Id);
                            
                            try
                            {
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
                            catch (Exception ex)
                            {
                                // Log the exception
                                Console.WriteLine($"Error getting modifiers for order item {item.Id}: {ex.Message}");
                            }
                        }
                    }
                }
            }
                
            // Get kitchen tickets using a separate connection
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                string stationIdFieldName = GetSafeStationIdFieldName();
                string kitchenTicketQuery = $@"
                    SELECT 
                        kt.Id,
                        kt.TicketNumber,
                        kt.{stationIdFieldName},
                        kt.Status,
                        kt.CreatedAt,
                        kt.CompletedAt
                    FROM KitchenTickets kt
                    WHERE kt.OrderId = @OrderId
                    ORDER BY kt.CreatedAt DESC";
                
                using (SqlCommand command = new SqlCommand(kitchenTicketQuery, connection))
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
            }
            
            // Use a new connection for kitchen ticket items to avoid DataReader issues
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Get kitchen ticket items
                foreach (var ticket in order.KitchenTickets)
                {
                    // Get the correct table name for kitchen ticket items
                    string kitchenTicketItemsTable = GetCorrectTableName("KitchenTicketItems", "Kitchen_TicketItems");
                    
                    string queryString;
                    if (kitchenTicketItemsTable == "KitchenTicketItems")
                    {
                        // Use direct field access because the schema might have changed
                        queryString = $@"
                            SELECT 
                                kti.Id,
                                kti.OrderItemId,
                                mi.Name,
                                oi.Quantity,
                                oi.SpecialInstructions,
                                kti.Status,
                                kti.StartTime,
                                kti.CompletionTime,
                                kti.Notes
                            FROM {kitchenTicketItemsTable} kti
                            INNER JOIN OrderItems oi ON kti.OrderItemId = oi.Id
                            INNER JOIN MenuItems mi ON oi.MenuItemId = mi.Id
                            WHERE kti.KitchenTicketId = @KitchenTicketId";
                    }
                    else
                    {
                        // Get field names for the alternate version of the table
                        queryString = $@"
                            SELECT 
                                kti.Id,
                                kti.OrderItemId,
                                mi.Name,
                                oi.Quantity,
                                oi.SpecialInstructions,
                                kti.Status,
                                kti.StartTime,
                                kti.CompletionTime,
                                kti.Notes
                            FROM {kitchenTicketItemsTable} kti
                            INNER JOIN OrderItems oi ON kti.OrderItemId = oi.Id
                            INNER JOIN MenuItems mi ON oi.MenuItemId = mi.Id
                            WHERE kti.KitchenTicketId = @KitchenTicketId";
                    }
                    
                    using (SqlCommand command = new SqlCommand(queryString, connection))
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
                                
                                // Get modifiers for this ticket item using a separate connection
                                string orderItemModifiersTable = GetCorrectTableName("OrderItemModifiers", "OrderItem_Modifiers");
                                
                                if (!string.IsNullOrEmpty(orderItemModifiersTable))
                                {
                                    using (SqlConnection modConnection = new SqlConnection(_connectionString))
                                    {
                                        modConnection.Open();
                                        string modifiersQuery = $@"
                                            SELECT m.Name
                                            FROM {orderItemModifiersTable} oim
                                            INNER JOIN Modifiers m ON oim.ModifierId = m.Id
                                            WHERE oim.OrderItemId = @OrderItemId";
                                            
                                        using (SqlCommand modifiersCommand = new SqlCommand(modifiersQuery, modConnection))
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
EOL

# Use sed to replace the entire method in the file
start_line=$(grep -n "private OrderViewModel GetOrderDetails" /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/Controllers/OrderController.cs | cut -d ":" -f1)
end_line=$(tail -n +$start_line /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/Controllers/OrderController.cs | grep -n "        private" | head -1 | cut -d ":" -f1)
end_line=$((start_line + end_line - 2))

# Create a temporary file with the fixed content
head -n $((start_line - 1)) /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/Controllers/OrderController.cs > /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/temp.cs
cat /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/fixed_method.cs >> /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/temp.cs
tail -n +$((end_line + 1)) /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/Controllers/OrderController.cs >> /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/temp.cs

# Replace the original file with the fixed one
mv /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/temp.cs /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/Controllers/OrderController.cs

# Clean up
rm /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem/fixed_method.cs

# Try to build
cd /Users/abhikporel/dev/Restaurantapp
dotnet build RestaurantManagementSystem/RestaurantManagementSystem.csproj
