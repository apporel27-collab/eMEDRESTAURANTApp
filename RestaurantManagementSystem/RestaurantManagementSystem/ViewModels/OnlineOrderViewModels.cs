using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.ViewModels
{
    // View Models for UC-006: Manage Online Order (Ordering Hub)
    
    /// <summary>
    /// Dashboard for online orders
    /// </summary>
    public class OnlineOrderDashboardViewModel
    {
        public OnlineOrderStats Stats { get; set; } = new OnlineOrderStats();
        public List<OnlineOrderViewModel> RecentOrders { get; set; } = new List<OnlineOrderViewModel>();
        public List<WebhookEventViewModel> RecentWebhookEvents { get; set; } = new List<WebhookEventViewModel>();
        public List<OrderSourceViewModel> OrderSources { get; set; } = new List<OrderSourceViewModel>();
    }
    
    /// <summary>
    /// Statistics for online orders dashboard
    /// </summary>
    public class OnlineOrderStats
    {
        public int NewOrdersCount { get; set; }
        public int AcknowledgedOrdersCount { get; set; }
        public int InPreparationOrdersCount { get; set; }
        public int ReadyForPickupOrdersCount { get; set; }
        public int OutForDeliveryOrdersCount { get; set; }
        public int TodayOrdersCount { get; set; }
        public decimal TodayOrdersTotal { get; set; }
        public int UnsyncedOrdersCount { get; set; }
        public int FailedSyncOrdersCount { get; set; }
    }
    
    /// <summary>
    /// Summary information about an online order
    /// </summary>
    public class OnlineOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string ExternalOrderId { get; set; }
        public string OrderSourceName { get; set; }
        public string StatusName { get; set; }
        public string StatusColor { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal OrderTotal { get; set; }
        public bool IsDelivery { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? RequestedDeliveryTime { get; set; }
        public int SyncStatus { get; set; } // 0=New, 1=Synced, 2=Error
        public int? SyncedToLocalOrderId { get; set; }
        
        public string FormattedOrderDate => OrderDate.ToString("MM/dd/yyyy h:mm tt");
        public string FormattedRequestedDeliveryTime => RequestedDeliveryTime?.ToString("MM/dd/yyyy h:mm tt");
        public string SyncStatusText => SyncStatus == 0 ? "Not Synced" : (SyncStatus == 1 ? "Synced" : "Failed");
    }
    
    /// <summary>
    /// Order list with filtering options
    /// </summary>
    public class OnlineOrderListViewModel
    {
        public List<OnlineOrderViewModel> Orders { get; set; } = new List<OnlineOrderViewModel>();
        public List<OrderSourceViewModel> OrderSources { get; set; } = new List<OrderSourceViewModel>();
        public List<OnlineOrderStatusViewModel> OrderStatuses { get; set; } = new List<OnlineOrderStatusViewModel>();
        
        public int? StatusId { get; set; }
        public int? OrderSourceId { get; set; }
        public int? SyncStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string SearchTerm { get; set; }
    }
    
    /// <summary>
    /// Detailed view of an online order
    /// </summary>
    public class OnlineOrderDetailsViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public string ExternalOrderId { get; set; }
        public int OrderSourceId { get; set; }
        public string OrderSourceName { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string StatusColor { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }
        public decimal OrderTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Tip { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string CouponCode { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public bool IsDelivery { get; set; }
        public bool IsPreOrder { get; set; }
        public DateTime? RequestedDeliveryTime { get; set; }
        public DateTime? ActualDeliveryTime { get; set; }
        public string DeliveryAddress { get; set; }
        public string DeliveryNotes { get; set; }
        public string DeliveryDriverName { get; set; }
        public string DeliveryDriverPhone { get; set; }
        public string SpecialInstructions { get; set; }
        public int SyncStatus { get; set; }
        public string ErrorDetails { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? SyncedToLocalOrderId { get; set; }
        
        public List<OnlineOrderItemViewModel> Items { get; set; } = new List<OnlineOrderItemViewModel>();
        public List<OnlineOrderStatusViewModel> AvailableStatuses { get; set; } = new List<OnlineOrderStatusViewModel>();
        
        public string FormattedOrderDate => OrderDate.ToString("MM/dd/yyyy h:mm tt");
        public string FormattedUpdateDate => UpdatedAt.ToString("MM/dd/yyyy h:mm tt");
        public string FormattedRequestedDeliveryTime => RequestedDeliveryTime?.ToString("MM/dd/yyyy h:mm tt");
        public string FormattedActualDeliveryTime => ActualDeliveryTime?.ToString("MM/dd/yyyy h:mm tt");
        public string SyncStatusText => SyncStatus == 0 ? "Not Synced" : (SyncStatus == 1 ? "Synced" : "Failed");
    }
    
    /// <summary>
    /// Represents an item in an online order
    /// </summary>
    public class OnlineOrderItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string ExternalProductId { get; set; }
        public int? MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string SpecialInstructions { get; set; }
        
        public List<OnlineOrderItemModifierViewModel> Modifiers { get; set; } = new List<OnlineOrderItemModifierViewModel>();
    }
    
    /// <summary>
    /// Represents a modifier for an online order item
    /// </summary>
    public class OnlineOrderItemModifierViewModel
    {
        public int Id { get; set; }
        public int OnlineOrderItemId { get; set; }
        public string ModifierName { get; set; }
        public string ExternalModifierId { get; set; }
        public int? MenuItemModifierId { get; set; }
        public string MenuModifierName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
    
    /// <summary>
    /// View model for updating order status
    /// </summary>
    public class UpdateOrderStatusViewModel
    {
        public int OrderId { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        public int StatusId { get; set; }
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; }
    }
    
    /// <summary>
    /// View model for order source
    /// </summary>
    public class OrderSourceViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// View model for order status
    /// </summary>
    public class OnlineOrderStatusViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// Detailed view model for order source with configuration
    /// </summary>
    public class OrderSourceDetailsViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        [Display(Name = "Order Source Name")]
        public string Name { get; set; }
        
        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string Description { get; set; }
        
        [StringLength(100, ErrorMessage = "API Key cannot exceed 100 characters")]
        [Display(Name = "API Key")]
        public string ApiKey { get; set; }
        
        [StringLength(100, ErrorMessage = "API Secret cannot exceed 100 characters")]
        [Display(Name = "API Secret")]
        public string ApiSecret { get; set; }
        
        [StringLength(255, ErrorMessage = "Webhook URL cannot exceed 255 characters")]
        [Display(Name = "Webhook URL")]
        public string WebhookUrl { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; }
        
        public List<OrderSourceConfigurationViewModel> Configurations { get; set; } = new List<OrderSourceConfigurationViewModel>();
        public List<WebhookEventViewModel> RecentEvents { get; set; } = new List<WebhookEventViewModel>();
    }
    
    /// <summary>
    /// View model for order source configuration
    /// </summary>
    public class OrderSourceConfigurationViewModel
    {
        public int Id { get; set; }
        public int OrderSourceId { get; set; }
        
        [Required(ErrorMessage = "Configuration key is required")]
        [StringLength(50, ErrorMessage = "Configuration key cannot exceed 50 characters")]
        [Display(Name = "Configuration Key")]
        public string ConfigKey { get; set; }
        
        [Display(Name = "Configuration Value")]
        public string ConfigValue { get; set; }
        
        [Display(Name = "Encrypted")]
        public bool IsEncrypted { get; set; }
    }
    
    /// <summary>
    /// View model for webhook events
    /// </summary>
    public class WebhookEventViewModel
    {
        public int Id { get; set; }
        public string OrderSourceName { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
        public int ProcessStatus { get; set; } // 0=New, 1=Processed, 2=Error
        public string ErrorDetails { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        
        public string StatusText => ProcessStatus == 0 ? "New" : (ProcessStatus == 1 ? "Processed" : "Error");
        public string FormattedCreatedAt => CreatedAt.ToString("MM/dd/yyyy h:mm:ss tt");
        public string FormattedProcessedAt => ProcessedAt?.ToString("MM/dd/yyyy h:mm:ss tt");
    }
    
    /// <summary>
    /// Menu item mapping for external platforms
    /// </summary>
    public class ExternalMenuItemMappingViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Menu Item is required")]
        [Display(Name = "Menu Item")]
        public int MenuItemId { get; set; }
        
        public string MenuItemName { get; set; }
        
        [Required(ErrorMessage = "Order Source is required")]
        [Display(Name = "Order Source")]
        public int OrderSourceId { get; set; }
        
        public string OrderSourceName { get; set; }
        
        [Required(ErrorMessage = "External Item ID is required")]
        [StringLength(50, ErrorMessage = "External Item ID cannot exceed 50 characters")]
        [Display(Name = "External Item ID")]
        public string ExternalItemId { get; set; }
        
        [StringLength(100, ErrorMessage = "External Item Name cannot exceed 100 characters")]
        [Display(Name = "External Item Name")]
        public string ExternalItemName { get; set; }
        
        [Display(Name = "External Price")]
        public decimal? ExternalPrice { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        
        public DateTime? LastSyncedAt { get; set; }
        
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
        public List<OrderSource> OrderSources { get; set; } = new List<OrderSource>();
    }
    
    /// <summary>
    /// View model for menu item mappings list
    /// </summary>
    public class MenuItemMappingsViewModel
    {
        public List<ExternalMenuItemMappingViewModel> Mappings { get; set; } = new List<ExternalMenuItemMappingViewModel>();
        public int? OrderSourceId { get; set; }
        public List<OrderSourceViewModel> OrderSources { get; set; } = new List<OrderSourceViewModel>();
    }
}
