using Agrisky.Models;
using Microsoft.EntityFrameworkCore;

namespace AgriskyApi.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbcontext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbcontext>>();

                try
                {
                    // ─── Apply pending migrations ──────────────────────────────
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    if (pendingMigrations.Any())
                    {
                        logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                        await context.Database.MigrateAsync();
                        logger.LogInformation("Migrations applied successfully.");
                    }
                    else
                    {
                        logger.LogInformation("Database is up to date. No migrations to apply.");
                    }

                    logger.LogInformation("Database initialization completed successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during database initialization.");
                    throw;
                }
            }
        }
    }
}