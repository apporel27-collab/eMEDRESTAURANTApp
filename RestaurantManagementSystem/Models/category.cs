namespace RestaurantManagementSystem.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string CategoryName { get; set; } // Add 'required'
        public bool IsActive { get; set; }
    }
}