using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    // Models for UC-006: Manage Online Order (Ordering Hub)
    
    /// <summary>
    /// Represents an order source (website, mobile app, or delivery platform)
    /// </summary>
    public class OrderSource
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        [StringLength(100)]
        public string ApiKey { get; set; }
        
        [StringLength(100)]
        public string ApiSecret { get; set; }
        
        [StringLength(255)]
        public string WebhookUrl { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public List<OrderSourceConfiguration> Configurations { get; set; } = new List<OrderSourceConfiguration>();
        
        public List<ExternalMenuItemMapping> MenuItemMappings { get; set; } = new List<ExternalMenuItemMapping>();
    }
    
    /// <summary>
    /// Configuration settings for an order source
    /// </summary>
    public class OrderSourceConfiguration
    {
        public int Id { get; set; }
        
        public int OrderSourceId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ConfigKey { get; set; }
        
        public string ConfigValue { get; set; }
        
        public bool IsEncrypted { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public OrderSource OrderSource { get; set; }
    }
    
    /// <summary>
    /// Represents the status of an online order
    /// </summary>
    public class OnlineOrderStatus
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        [StringLength(20)]
        public string Color { get; set; }
        
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// Represents an order from an external source
    /// </summary>
    public class OnlineOrder
    {
        public int Id { get; set; }
        
        public int OrderSourceId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ExternalOrderId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; }
        
        public int? CustomerId { get; set; }
        
        public int OrderStatusId { get; set; }
        
        [Required]
        public decimal OrderTotal { get; set; }
        
        [Required]
        public decimal TaxAmount { get; set; }
        
        public decimal DeliveryFee { get; set; }
        
        public decimal Tip { get; set; }
        
        public decimal ServiceFee { get; set; }
        
        public decimal DiscountAmount { get; set; }
        
        [StringLength(50)]
        public string CouponCode { get; set; }
        
        [StringLength(50)]
        public string PaymentMethod { get; set; }
        
        [StringLength(20)]
        public string PaymentStatus { get; set; }
        
        public bool IsDelivery { get; set; }
        
        public bool IsPreOrder { get; set; }
        
        public DateTime? RequestedDeliveryTime { get; set; }
        
        public DateTime? ActualDeliveryTime { get; set; }
        
        [StringLength(255)]
        public string DeliveryAddress { get; set; }
        
        [StringLength(500)]
        public string DeliveryNotes { get; set; }
        
        [StringLength(100)]
        public string DeliveryDriverName { get; set; }
        
        [StringLength(20)]
        public string DeliveryDriverPhone { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }
        
        [StringLength(20)]
        public string CustomerPhone { get; set; }
        
        [StringLength(100)]
        public string CustomerEmail { get; set; }
        
        [StringLength(500)]
        public string SpecialInstructions { get; set; }
        
        public string SourceData { get; set; }
        
        public int SyncStatus { get; set; } // 0=New, 1=Synced, 2=Error
        
        public string ErrorDetails { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public int? SyncedToLocalOrderId { get; set; }
        
        public OrderSource OrderSource { get; set; }
        
        public OnlineOrderStatus Status { get; set; }
        
        public Customer Customer { get; set; }
        
        public List<OnlineOrderItem> Items { get; set; } = new List<OnlineOrderItem>();
    }
    
    /// <summary>
    /// Represents an item in an online order
    /// </summary>
    public class OnlineOrderItem
    {
        public int Id { get; set; }
        
        public int OnlineOrderId { get; set; }
        
        [StringLength(50)]
        public string ExternalProductId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; }
        
        public int? MenuItemId { get; set; }
        
        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        public decimal TotalPrice { get; set; }
        
        [StringLength(500)]
        public string SpecialInstructions { get; set; }
        
        public string SourceData { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public OnlineOrder Order { get; set; }
        
        public MenuItem MenuItem { get; set; }
        
        public List<OnlineOrderItemModifier> Modifiers { get; set; } = new List<OnlineOrderItemModifier>();
    }
    
    /// <summary>
    /// Represents a modifier for an online order item
    /// </summary>
    public class OnlineOrderItemModifier
    {
        public int Id { get; set; }
        
        public int OnlineOrderItemId { get; set; }
        
        [StringLength(50)]
        public string ExternalModifierId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ModifierName { get; set; }
        
        public int? MenuItemModifierId { get; set; }
        
        public int Quantity { get; set; }
        
        public decimal UnitPrice { get; set; }
        
        public decimal TotalPrice { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public OnlineOrderItem OrderItem { get; set; }
        
        public MenuItemModifier MenuItemModifier { get; set; }
    }
    
    /// <summary>
    /// Represents a webhook event received from an external source
    /// </summary>
    public class WebhookEvent
    {
        public int Id { get; set; }
        
        public int OrderSourceId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string EventType { get; set; }
        
        [Required]
        public string Payload { get; set; }
        
        public int ProcessStatus { get; set; } // 0=New, 1=Processed, 2=Error
        
        public string ErrorDetails { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? ProcessedAt { get; set; }
        
        public OrderSource OrderSource { get; set; }
    }
    
    /// <summary>
    /// Maps menu items to external platform items
    /// </summary>
    public class ExternalMenuItemMapping
    {
        public int Id { get; set; }
        
        public int MenuItemId { get; set; }
        
        public int OrderSourceId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ExternalItemId { get; set; }
        
        [StringLength(100)]
        public string ExternalItemName { get; set; }
        
        public decimal? ExternalPrice { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime? LastSyncedAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public MenuItem MenuItem { get; set; }
        
        public OrderSource OrderSource { get; set; }
    }
    
    /// <summary>
    /// Maps modifiers to external platform modifiers
    /// </summary>
    public class ExternalModifierMapping
    {
        public int Id { get; set; }
        
        public int MenuItemModifierId { get; set; }
        
        public int OrderSourceId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ExternalModifierId { get; set; }
        
        [StringLength(100)]
        public string ExternalModifierName { get; set; }
        
        public decimal? ExternalPrice { get; set; }
        
        public bool IsActive { get; set; }
        
        public DateTime? LastSyncedAt { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public MenuItemModifier MenuItemModifier { get; set; }
        
        public OrderSource OrderSource { get; set; }
    }
    
    /// <summary>
    /// Logs API calls to external services
    /// </summary>
    public class ApiCallLog
    {
        public int Id { get; set; }
        
        public int OrderSourceId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string EndpointUrl { get; set; }
        
        [Required]
        [StringLength(10)]
        public string RequestMethod { get; set; }
        
        public string RequestHeaders { get; set; }
        
        public string RequestBody { get; set; }
        
        public int? ResponseCode { get; set; }
        
        public string ResponseBody { get; set; }
        
        public int? ExecutionTimeMs { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public OrderSource OrderSource { get; set; }
    }
}
