namespace RestaurantManagementSystem.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Use Name as the storage column
        public bool IsActive { get; set; }
        
        // Navigation property to SubCategories
        public virtual ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
        
        // This property gets the value from the Name column
        // Used for backward compatibility with UI code
        public string CategoryName 
        { 
            get => Name; 
            set => Name = value;
        }
    }
}