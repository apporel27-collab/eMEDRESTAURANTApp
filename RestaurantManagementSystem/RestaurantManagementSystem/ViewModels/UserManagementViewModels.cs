using System.Collections.Generic;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.ViewModels
{
    public class UserRoleViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class RoleUsersViewModel
    {
        public Role Role { get; set; }
        public List<UserRoleViewModel> Users { get; set; }
    }

    public class UserWithRolesViewModel
    {
        public User User { get; set; }
        public List<Role> AssignedRoles { get; set; } = new List<Role>();
        public List<Role> AvailableRoles { get; set; } = new List<Role>();
    }
}
