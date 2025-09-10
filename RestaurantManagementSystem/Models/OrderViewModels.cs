using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    // View Models for UC-003: Capture Dine-In Order
    
    public class OrderDashboardViewModel
    {
        public List<OrderSummary> ActiveOrders { get; set; } = new List<OrderSummary>();
        public List<OrderSummary> CompletedOrders { get; set; } = new List<OrderSummary>();
        public int OpenOrdersCount { get; set; }
        public int InProgressOrdersCount { get; set; }
        public int ReadyOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public decimal TotalSales { get; set; }
    }
    
    public class OrderSummary
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int OrderType { get; set; } // 0=Dine-In, 1=Takeout, 2=Delivery, 3=Online
        public string OrderTypeDisplay { get; set; }
        public int Status { get; set; } // 0=Open, 1=In Progress, 2=Ready, 3=Completed, 4=Cancelled
        public string StatusDisplay { get; set; }
        public string TableName { get; set; }
        public string GuestName { get; set; }
        public string ServerName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public TimeSpan Duration { get; set; }
    }
    
    public class CreateOrderViewModel
    {
        public List<TableViewModel> AvailableTables { get; set; } = new List<TableViewModel>();
        public List<ActiveTableViewModel> OccupiedTables { get; set; } = new List<ActiveTableViewModel>();
        
        [Display(Name = "Table")]
        public int? TableTurnoverId { get; set; }
        
        [Required]
        [Display(Name = "Order Type")]
        public int OrderType { get; set; } = 0; // Default to Dine-In
        
        [Display(Name = "Customer Name")]
        [StringLength(100)]
        public string CustomerName { get; set; }
        
        [Display(Name = "Customer Phone")]
        [StringLength(20)]
        public string CustomerPhone { get; set; }
        
        [Display(Name = "Special Instructions")]
        [StringLength(500)]
        public string SpecialInstructions { get; set; }
    }
    
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int? TableTurnoverId { get; set; }
        public string TableName { get; set; }
        public string GuestName { get; set; }
        public int OrderType { get; set; }
        public string OrderTypeDisplay { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; }
        public string ServerName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TipAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string SpecialInstructions { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
        public List<KitchenTicketViewModel> KitchenTickets { get; set; } = new List<KitchenTicketViewModel>();
        
        // Properties for adding new items
        public List<MenuCategoryViewModel> MenuCategories { get; set; } = new List<MenuCategoryViewModel>();
        public List<CourseType> AvailableCourses { get; set; } = new List<CourseType>();
    }
    
    public class OrderItemViewModel
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public string MenuItemDescription { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalPrice { get; set; }
        public string Name { get; set; } // Added to resolve the error
        public string SpecialInstructions { get; set; }
        public int? CourseId { get; set; }
        public string CourseName { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; }
        public DateTime? FireTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public DateTime? DeliveryTime { get; set; }
        
        public List<OrderItemModifierViewModel> Modifiers { get; set; } = new List<OrderItemModifierViewModel>();
    }
    
    public class OrderItemModifierViewModel
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public int ModifierId { get; set; }
        public string ModifierName { get; set; }
        public decimal Price { get; set; }
    }
    
    public class KitchenTicketViewModel
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public int? StationId { get; set; } // Maps to KitchenStationId in the database
        public string StationName { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public List<KitchenTicketItemViewModel> Items { get; set; } = new List<KitchenTicketItemViewModel>();
    }
    
    public class KitchenTicketItemViewModel
    {
        public int Id { get; set; }
        public int KitchenTicketId { get; set; }
        public int OrderItemId { get; set; }
        public string MenuItemName { get; set; }
        public int Quantity { get; set; }
        public string SpecialInstructions { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public string Notes { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
    }
    
    public class AddOrderItemViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string TableNumber { get; set; }
        
        [Required]
        [Display(Name = "Menu Item")]
        public int MenuItemId { get; set; }
        
        // Menu item details
        public string MenuItemName { get; set; }
        public string MenuItemDescription { get; set; }
        public decimal MenuItemPrice { get; set; }
        public string MenuItemImagePath { get; set; }
        
        [Required]
        [Range(1, 100)]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;
        
        [Display(Name = "Special Instructions")]
        [StringLength(500)]
        public string SpecialInstructions { get; set; }
        
        [Display(Name = "Course")]
        public int? CourseId { get; set; }
        
        [Display(Name = "Modifiers")]
        public List<int> SelectedModifiers { get; set; } = new List<int>();
        
        // Order summary
        public List<OrderItemViewModel> CurrentOrderItems { get; set; } = new List<OrderItemViewModel>();
        public decimal CurrentOrderTotal { get; set; }
        
        // Navigation properties for dropdown population
        public MenuItem MenuItem { get; set; }
        public List<SelectListItem> AvailableCourses { get; set; } = new List<SelectListItem>();
        public List<ModifierViewModel> AvailableModifiers { get; set; } = new List<ModifierViewModel>();
        public List<string> CommonAllergens { get; set; } = new List<string>();
    }
    
    public class MenuCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
    
    public class ModifierViewModel
    {
        public int Id { get; set; }
        public int ModifierId { get; set; } // Added to fix the errors
        public string Name { get; set; }
        public decimal Price { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSelected { get; set; }
    }
    
    public class FireOrderItemsViewModel
    {
        public int OrderId { get; set; }
        public List<int> SelectedItems { get; set; } = new List<int>();
        public bool FireAll { get; set; }
    }
    
    public class TableViewModel
    {
        public int Id { get; set; }
        public string TableName { get; set; }
        public int Capacity { get; set; }
        public int Status { get; set; }
        public string StatusDisplay { get; set; }
    }
}
