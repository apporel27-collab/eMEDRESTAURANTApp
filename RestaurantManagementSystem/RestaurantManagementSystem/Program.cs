using Microsoft.EntityFrameworkCore;
using RestaurantManagementSystem.Data;
using RestaurantManagementSystem.Middleware;
using RestaurantManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestaurantManagementSystem.Utilities;
using Microsoft.Data.SqlClient;

namespace RestaurantManagementSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHttpContextAccessor();

            // Add authentication services
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromHours(12);
                    options.SlidingExpiration = true;
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                });

            // Add authorization services
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Administrator"));
                options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Administrator", "Manager"));
                options.AddPolicy("RequireStaffRole", policy => policy.RequireRole("Administrator", "Manager", "Staff"));
            });

            // Register custom services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<UserRoleService>();
            builder.Services.AddScoped<AdminSetupService>();
            builder.Services.AddScoped<PasswordResetTool>();
            // Hosted service for non-blocking admin initialization
            builder.Services.AddHostedService<AdminInitializationHostedService>();

            // Configure SQL Server database connection using connection string from appsettings.json
            builder.Services.AddDbContext<RestaurantDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
                
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                // In development, enable detailed error pages
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseMiddleware<DatabaseColumnFixMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Ensure compatibility view exists so code that expects dbo.MenuItemGroups works
            try
            {
                using (var scope = app.Services.CreateScope())
                {
                    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var connStr = configuration.GetConnectionString("DefaultConnection");
                    if (!string.IsNullOrWhiteSpace(connStr))
                    {
                        using var conn = new SqlConnection(connStr);
                        conn.Open();
                        var checkViewSql = @"SELECT COUNT(*) FROM sys.views WHERE object_id = OBJECT_ID(N'dbo.MenuItemGroups')";
                        using var checkCmd = new SqlCommand(checkViewSql, conn);
                        var exists = (int)checkCmd.ExecuteScalar() > 0;
                        if (!exists)
                        {
                            var createViewSql = @"
IF OBJECT_ID('dbo.MenuItemGroups', 'V') IS NULL
BEGIN
    CREATE VIEW dbo.MenuItemGroups AS
    SELECT ID, itemgroup AS ItemGroup, is_active AS IsActive, GST_Perc
    FROM dbo.menuitemgroup;
END";
                            using var createCmd = new SqlCommand(createViewSql, conn);
                            createCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch
            {
                // Non-fatal; if DB access isn't available at startup, the controller will attempt to create the table.
            }

            // Removed blocking admin initialization; now handled by AdminInitializationHostedService

            app.Run();
        }
    }
}
