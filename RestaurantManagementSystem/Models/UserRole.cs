using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementSystem.Models
{
    public class UserRole
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int RoleId { get; set; }
        
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [ForeignKey("RoleId")]
        public Role Role { get; set; }
    }
    
    // Keep the enum for backward compatibility during transition
    public enum RoleType
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
