using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantManagementSystem.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Role Name")]
        public string Name { get; set; }

        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "System Role")]
        public bool IsSystemRole { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public List<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
    }
}
