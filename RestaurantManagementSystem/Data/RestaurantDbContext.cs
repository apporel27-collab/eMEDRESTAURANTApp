using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Models;

namespace RestaurantManagementSystem.Data
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) 
            : base(options)
        {
        }

        // Only include model types that we're sure exist
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Ingredients> Ingredients { get; set; } = null!;
        public DbSet<Table> Tables { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        
        // Menu and Recipe Management
        public DbSet<MenuItem> MenuItems { get; set; } = null!;
        public DbSet<Allergen> Allergens { get; set; } = null!;
        public DbSet<MenuItemAllergen> MenuItemAllergens { get; set; } = null!;
        public DbSet<Modifier> Modifiers { get; set; } = null!;
        public DbSet<MenuItemModifier> MenuItemModifiers { get; set; } = null!;
        public DbSet<MenuItemIngredient> MenuItemIngredients { get; set; } = null!;
        public DbSet<Recipe> Recipes { get; set; } = null!;
        public DbSet<RecipeStep> RecipeSteps { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Category entity
            modelBuilder.Entity<Category>(entity => 
            {
                entity.ToTable("Categories");
                entity.Property(e => e.Id).HasColumnName("Id").IsRequired();
                entity.Property(e => e.Name).HasColumnName("Name").IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired();
                
                // Ignore CategoryName for database operations
                entity.Ignore(e => e.CategoryName);
            });
            
            // Seed data for Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Appetizers", IsActive = true },
                new Category { Id = 2, Name = "Main Course", IsActive = true },
                new Category { Id = 3, Name = "Desserts", IsActive = true },
                new Category { Id = 4, Name = "Beverages", IsActive = true }
            );

            // Seed data for Ingredients
            modelBuilder.Entity<Ingredients>().HasData(
                new Ingredients { Id = 1, IngredientsName = "Tomato", DisplayName = "Tomato", Code = "TMT" },
                new Ingredients { Id = 2, IngredientsName = "Cheese", DisplayName = "Cheese", Code = "CHS" },
                new Ingredients { Id = 3, IngredientsName = "Chicken", DisplayName = "Chicken", Code = "CHK" },
                new Ingredients { Id = 4, IngredientsName = "Basil", DisplayName = "Basil", Code = "BSL" }
            );

            // Seed data for Tables
            modelBuilder.Entity<Table>().HasData(
                new Table { Id = 1, TableNumber = "T1", Capacity = 4, Status = TableStatus.Available, IsActive = true },
                new Table { Id = 2, TableNumber = "T2", Capacity = 2, Status = TableStatus.Occupied, IsActive = true },
                new Table { Id = 3, TableNumber = "T3", Capacity = 6, Status = TableStatus.Available, IsActive = true },
                new Table { Id = 4, TableNumber = "T4", Capacity = 8, Status = TableStatus.Reserved, IsActive = true }
            );

            // Seed data for Reservations
            modelBuilder.Entity<Reservation>().HasData(
                new Reservation { 
                    Id = 1, 
                    ReservationDate = DateTime.Today, 
                    ReservationTime = DateTime.Today.AddHours(19),
                    PartySize = 4, 
                    GuestName = "John Smith", 
                    PhoneNumber = "555-1234", 
                    Status = ReservationStatus.Confirmed, 
                    TableId = 4 
                },
                new Reservation { 
                    Id = 2, 
                    ReservationDate = DateTime.Today.AddDays(1), 
                    ReservationTime = DateTime.Today.AddDays(1).AddHours(18).AddMinutes(30),
                    PartySize = 2, 
                    GuestName = "Mary Johnson", 
                    PhoneNumber = "555-5678", 
                    Status = ReservationStatus.Pending 
                }
            );
            
            base.OnModelCreating(modelBuilder);
        }
    }
}
