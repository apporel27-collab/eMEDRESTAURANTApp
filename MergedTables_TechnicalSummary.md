# Merged Tables Implementation - Technical Summary

## Overview
The restaurant management system now supports **merged tables** functionality, allowing multiple tables to be assigned to a single order. This enables combining tables for large parties or special events while maintaining proper order tracking and display throughout the application.

## Data Model Changes

### New Table: `OrderTables`
```sql
CREATE TABLE OrderTables (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    TableId INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_OrderTables_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    CONSTRAINT FK_OrderTables_Tables FOREIGN KEY (TableId) REFERENCES Tables(Id),
    CONSTRAINT UQ_OrderTables_Order_Table UNIQUE (OrderId, TableId)
);
```

### New View: `vw_OrderMergedTables`
Aggregates table names for orders with merged tables using `STRING_AGG` for easy querying.

### Optional Function: `fn_GetOrderMergedTables`
Scalar function returning merged table names as a concatenated string (e.g., "A1 + B2 + C3").

## UI/UX Changes

### Order Creation (`/Order/Create`)
- **Primary Table Selection**: Existing dropdown for main table or turnover
- **Merge Additional Tables**: New checkbox section allowing selection of multiple additional tables
- **Auto-hide Logic**: Merge section only visible for Dine-In orders
- **Combined Capacity**: Visual indication that capacities are combined

### Display Format
- **Single Table**: "Table A1"
- **Merged Tables**: "A1 + B2 + C3"
- **Mixed**: "A1 + B2" (primary + merged)
- **Badge Indicator**: Small "Merged" badge appears when `+` is detected

## Application Integration Points

### Kitchen Dashboard (`/Kitchen/Dashboard`)
- All KOT cards show merged table names in the order metadata line
- Fallback logic ensures compatibility with existing tickets
- "Merged" badge automatically appears for multi-table orders

### Order Management
- **Order Dashboard**: All order summaries show merged table names
- **Order Details**: Individual order pages display complete merged table information
- **Recent Orders**: Home dashboard recent orders include merged table display

### Payment Processing
- Payment screens show merged table names for proper identification
- Payment history includes merged table information

### Reporting & Analytics
- Recent orders stored procedure updated to include merged table aggregation
- All table-related queries now support merged table display

## Data Persistence

### Order Creation Flow
1. User selects primary table (optional) and additional merge tables
2. Order created normally with primary `TableTurnoverId` (if applicable)
3. Additional tables inserted into `OrderTables` with duplicate prevention
4. Kitchen tickets generated with merged table information

### Backwards Compatibility
- Existing orders without merged tables continue to work normally
- Single-table orders can be displayed consistently
- Optional backfill script available for historical data

## Helper Methods

### C# Integration
```csharp
private string GetMergedTableDisplayName(int orderId, string existingTableName)
```
- Used in `OrderController`, `PaymentController`, and `KitchenController`
- Aggregates merged table names via SQL query
- Handles combining primary and merged tables without duplication
- Graceful fallback on errors

### SQL Integration
- Updated stored procedures: `usp_GetRecentOrdersForDashboard`
- Enhanced view queries in controllers
- Kitchen ticket fallback logic in `GetTableNameForOrder`

## Migration & Deployment

### Required SQL Scripts
1. **`OrderTables_Merge_Setup.sql`**: Creates table and view
2. **`MergedTables_Functions.sql`**: Creates function and enhanced view
3. **Update `usp_GetRecentOrdersForDashboard.sql`**: Enhanced stored procedure

### Optional Backfill
```sql
-- Backfill existing single-table dine-in orders into OrderTables
INSERT INTO OrderTables (OrderId, TableId)
SELECT o.Id, t.Id
FROM Orders o
INNER JOIN TableTurnovers tt ON o.TableTurnoverId = tt.Id  
INNER JOIN Tables t ON tt.TableId = t.Id
LEFT JOIN OrderTables ot ON ot.OrderId = o.Id AND ot.TableId = t.Id
WHERE o.OrderType = 0 AND ot.Id IS NULL;
```

### Deployment Order
1. Backup database
2. Run `OrderTables_Merge_Setup.sql`
3. Run `MergedTables_Functions.sql` (if using functions)
4. Update stored procedure via `usp_GetRecentOrdersForDashboard.sql`
5. Deploy application code
6. Optionally run backfill script
7. Test merged table creation and display

## Performance Considerations

### Query Optimization
- `OrderTables` has clustered index on `(OrderId, TableId)`
- `STRING_AGG` operations are efficient for small table counts (typically 2-4 tables per order)
- Helper method uses parameterized queries and connection pooling

### Caching Strategy
- Consider caching merged table names for frequently accessed orders
- View materialization possible for high-volume scenarios

## Business Rules

### Validation
- Prevent merging the same table twice to one order
- Ensure merged tables are available/compatible
- Maintain table capacity and guest count logic

### Display Rules
- Always show tables in alphabetical order when merged
- Use " + " as separator for merged table names
- Show "Merged" badge only when multiple tables detected
- Fallback gracefully when merged table data unavailable

## Future Enhancements

### Potential Features
- **Capacity Validation**: Prevent over-seating across merged tables
- **Split Orders**: Allow splitting items across different tables in merge
- **Table Management**: Edit/remove tables from existing merged orders
- **Reporting**: Analytics on merged table usage and efficiency
- **Auto-Merge**: Suggest table merging based on party size and availability

### API Extensions
- REST endpoints for managing merged tables
- Real-time notifications when merged tables are updated
- Integration with table management systems

## Troubleshooting

### Common Issues
- **Missing Table Names**: Check `OrderTables` data and `Tables.TableName` population
- **Duplicate Display**: Verify unique constraint on `OrderTables`
- **Performance**: Monitor `STRING_AGG` query performance with large datasets

### Debug Queries
```sql
-- Check merged tables for specific order
SELECT * FROM OrderTables WHERE OrderId = ?;

-- Verify aggregation logic
SELECT * FROM vw_OrderMergedTables WHERE OrderId = ?;

-- Test function (if implemented)
SELECT dbo.fn_GetOrderMergedTables(?);
```

---
*Implementation completed: October 2025*  
*Compatible with: .NET 9.0, SQL Server 2019+*