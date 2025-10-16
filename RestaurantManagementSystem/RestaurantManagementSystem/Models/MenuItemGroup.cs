namespace RestaurantManagementSystem.Models
{
    public class MenuItemGroup
    {
        public int ID { get; set; }
        public string ItemGroup { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal? GST_Perc { get; set; }
    }
}
