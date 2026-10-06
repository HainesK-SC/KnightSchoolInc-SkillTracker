using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Data
{
    public static class DatabaseMigrator
    {
        public static async Task MigrateIfEnabledAsync(WebApplication app)
        {
            if (!app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            {
                return;
            }

            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(DatabaseMigrator));
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            logger.LogInformation("Applying database migrations.");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
    }
}
