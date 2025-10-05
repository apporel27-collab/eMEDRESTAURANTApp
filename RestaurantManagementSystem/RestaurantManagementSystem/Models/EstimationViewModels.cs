using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace RestaurantManagementSystem.Models
{
    // View Models for Estimation Page
    public class EstimationViewModel
    {
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> SubCategories { get; set; } = new List<SelectListItem>();
        public List<EstimationMenuItemViewModel> MenuItems { get; set; } = new List<EstimationMenuItemViewModel>();
    }

    public class EstimationMenuItemViewModel
    {
        public int Id { get; set; }
        public string ItemName { get; set; }
        public decimal Price { get; set; }
        public string PLUCode { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int? SubCategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public int Quantity { get; set; } = 0;
    }
}