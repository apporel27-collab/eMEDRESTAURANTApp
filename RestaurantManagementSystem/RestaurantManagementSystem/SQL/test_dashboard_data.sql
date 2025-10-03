-- Test script to verify Dashboard data is working correctly
-- Run these queries to check if the stored procedures return data

PRINT 'Testing Home Dashboard Stored Procedures'
PRINT '========================================'

PRINT 'Executing usp_GetHomeDashboardStats:'
EXEC usp_GetHomeDashboardStats

PRINT ''
PRINT 'Executing usp_GetRecentOrdersForDashboard:'
EXEC usp_GetRecentOrdersForDashboard @OrderCount = 5

PRINT ''
PRINT 'Additional checks - Sample data queries:'
PRINT '--------------------------------------'

PRINT 'Total Orders in database:'
SELECT COUNT(*) as TotalOrders FROM Orders

PRINT 'Total Payments in database:'
SELECT COUNT(*) as TotalPayments FROM Payments

PRINT 'Total Tables in database:'
SELECT COUNT(*) as TotalTables FROM Tables

PRINT 'Total Reservations in database:'
SELECT COUNT(*) as TotalReservations FROM Reservations