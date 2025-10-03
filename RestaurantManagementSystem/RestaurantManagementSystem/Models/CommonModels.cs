using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    // Menu Item Modifier model for UC-006
    public class MenuItemModifierOption
    {
        public int Id { get; set; }
        
        public int MenuItemId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(200)]
        public string Description { get; set; }
        
        public decimal Price { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
    
    // Customer model for UC-006
    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [StringLength(20)]
        public string Phone { get; set; }
        
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}
