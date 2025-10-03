using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class ServerAssignment
    {
        public int Id { get; set; }

        [Required]
        public int TableId { get; set; }

        [Required]
        public int ServerId { get; set; }

        [Required]
        [Display(Name = "Assigned At")]
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        [Display(Name = "Last Modified")]
        public DateTime LastModifiedAt { get; set; } = DateTime.Now;

        [Display(Name = "Assigned By")]
        public int AssignedById { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties for easier data access
        public virtual Table Table { get; set; }
        public virtual User Server { get; set; }
        public virtual User AssignedBy { get; set; }
    }
}
