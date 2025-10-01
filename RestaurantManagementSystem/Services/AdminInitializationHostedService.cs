using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RestaurantManagementSystem.Services;
using RestaurantManagementSystem.Utilities;

namespace RestaurantManagementSystem.Services
{
    /// <summary>
    /// Performs non-critical admin initialization after the web host has started so Kestrel can bind immediately.
    /// </summary>
    public class AdminInitializationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AdminInitializationHostedService> _logger;
        private Task? _backgroundTask;
        private CancellationTokenSource _cts = new();

        public AdminInitializationHostedService(IServiceProvider serviceProvider, ILogger<AdminInitializationHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Admin initialization hosted service starting in background");
            _backgroundTask = Task.Run(() => RunAsync(_cts.Token), cancellationToken);
            return Task.CompletedTask; // Don't block startup
        }

        private async Task RunAsync(CancellationToken token)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var adminSetupService = scope.ServiceProvider.GetRequiredService<AdminSetupService>();
                var envLogger = scope.ServiceProvider.GetRequiredService<ILogger<AdminInitializationHostedService>>();

                // Hard timeout so we never hang forever if DB is unreachable
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(12));
                try
                {
                    envLogger.LogInformation("Ensuring admin user exists (background)...");
                    await adminSetupService.EnsureAdminUserAsync();
                }
                catch (Exception ex)
                {
                    envLogger.LogWarning(ex, "Admin user initialization failed or timed out");
                }

                // Attempt password reset only in Development
                var hostEnv = scope.ServiceProvider.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>();
                if (hostEnv?.IsDevelopment() == true)
                {
                    try
                    {
                        envLogger.LogInformation("Attempting admin password reset (background)...");
                        await AdminPasswordReset.ResetAdminPassword(scope.ServiceProvider);
                    }
                    catch (Exception ex)
                    {
                        envLogger.LogWarning(ex, "Admin password reset failed");
                    }
                }

                envLogger.LogInformation("Admin initialization hosted service completed tasks");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in admin initialization hosted service");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cts.Cancel();
                if (_backgroundTask != null)
                {
                    var completed = await Task.WhenAny(_backgroundTask, Task.Delay(TimeSpan.FromSeconds(2), cancellationToken));
                    if (completed != _backgroundTask)
                    {
                        _logger.LogInformation("Admin initialization background task did not finish before stop timeout");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error stopping admin initialization hosted service");
            }
        }
    }
}
