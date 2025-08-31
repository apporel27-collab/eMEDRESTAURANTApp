using System;

namespace RestaurantManagementSystem.Models
{
    public enum UserRole
    {
        Guest,                  // Guest/Customer
        Host,                   // Manages seating and reservations
        Server,                 // Waiter/Waitress who takes orders
        Cashier,                // Handles payments
        StationChef,            // Chef responsible for specific station
        Expeditor,              // Coordinates food delivery from kitchen to servers
        InventoryClerk,         // Manages inventory
        PurchasingManager,      // Handles purchasing of supplies
        RestaurantManager,      // Overall management
        DeliveryRider,          // Delivery personnel
        Accountant,             // Handles finances
        SystemAdmin,            // System administrator
        
        // The following are system integrations rather than human users
        // but included for completeness:
        CRMMarketing,           // Marketing system
        PaymentGateway,         // Payment processing
        Aggregator,             // Third-party order aggregator
        ERPAccounting,          // ERP/Accounting system
        MessagingProvider,      // SMS/WhatsApp provider
        BIAnalytics             // Business Intelligence/Analytics
    }
}
