using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Microsoft.AspNetCore.Identity;

namespace Capstone.Identity.API.Data.Seeding
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var logger = provider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(IdentitySeeder));

            await SeedRolesAsync(provider, logger);
            await SeedAdminAsync(provider, logger);
        }

        private static async Task SeedRolesAsync(IServiceProvider provider, ILogger logger)
        {
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var allRoles = Roles.GetAllRolesAsync().Result;

            foreach (var role in allRoles)
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}': {string.Join(" ", result.Errors.Select(e => e.Description))}");
                }

                logger.LogInformation("Created role {Role}.", role);
            }
        }

        private static async Task SeedAdminAsync(IServiceProvider provider, ILogger logger)
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var email = config["AdminSeed:Email"];
            var password = config["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogInformation("Admin seed skipped: AdminSeed is not configured.");
                return;
            }

            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                var userService = provider.GetRequiredService<IApplicationUserService>();
                var created = await userService.CreateUserWithProfileAsync(
                    email,
                    password,
                    config["AdminSeed:FirstName"] ?? "Admin",
                    config["AdminSeed:LastName"] ?? "User");

                if (!created.Succeeded)
                {
                    throw new InvalidOperationException($"Admin seed failed: {created.Error}");
                }

                user = created.Data!;
            }

            if (!await userManager.IsInRoleAsync(user, Roles.Admin))
            {
                var result = await userManager.AddToRoleAsync(user, Roles.Admin);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to grant Admin role: {string.Join(" ", result.Errors.Select(e => e.Description))}");
                }

                logger.LogInformation("Granted Admin role to user {UserId}.", user.Id);
            }
        }
    }
}
