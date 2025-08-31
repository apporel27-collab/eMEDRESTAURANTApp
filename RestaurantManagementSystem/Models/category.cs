namespace RestaurantManagementSystem.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; } // Use Name as the storage column
        public bool IsActive { get; set; }
        
        // This property gets the value from the Name column
        // Used for backward compatibility with UI code
        public string CategoryName 
        { 
            get => Name; 
            set => Name = value;
        }
    }
}