using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManagementSystem.Models
{
    public class SubCategory
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Sub Category Name is required")]
        [StringLength(100, ErrorMessage = "Sub Category Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        
        // Foreign Key to Category
        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }
        
        // Navigation property to Category
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        
    // Display Order for sorting
    [Range(1, int.MaxValue, ErrorMessage = "Display Order must be greater than 0")]
    [Required(ErrorMessage = "Display Order is required")]
    public int DisplayOrder { get; set; } = 1;
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        
        // This property gets the value from the Name column
        // Used for backward compatibility with UI code (similar to Category)
        public string SubCategoryName 
        { 
            get => Name; 
            set => Name = value;
        }
    }
}