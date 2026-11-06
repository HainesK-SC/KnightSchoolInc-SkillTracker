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
            await SeedInstructorAsync(provider, logger);
            await SeedRegularUsersAsync(provider, logger);
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

        private static async Task SeedInstructorAsync(IServiceProvider provider, ILogger logger)
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var email = config["InstructorSeed:Email"];
            var password = config["InstructorSeed:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogInformation("Instructir seed skipped: InstuctorSeed is not configured.");
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
                    config["InstructorSeed:FirstName"] ?? "Instructor",
                    config["InstructorSeed:LastName"] ?? "User");

                if (!created.Succeeded)
                {
                    throw new InvalidOperationException($"Instructor seed failed: {created.Error}");
                }

                user = created.Data!;
            }

            if (!await userManager.IsInRoleAsync(user, Roles.Instructor))
            {
                var result = await userManager.AddToRoleAsync(user, Roles.Instructor);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to grant Instructor role: {string.Join(" ", result.Errors.Select(e => e.Description))}");
                }

                logger.LogInformation("Granted Instructor role to user {UserId}.", user.Id);
            }
        }

        private static async Task SeedRegularUsersAsync(IServiceProvider provider, ILogger logger)
        {
            for (int i = 1; i <= 3; i++)
            {
                var userEmailString = $"User{i}Seed:Email";
                var userPasswordString = $"User{i}Seed:Password";
                var userFirstNameString = $"User{i}:FirstName";
                var userLastNameString = $"User{i}:LastName";

                var config = provider.GetRequiredService<IConfiguration>();
                var email = config[userEmailString];
                var password = config[userPasswordString];

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    logger.LogInformation($"User{i} seed skipped: User{i}Seed is not configured.");
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
                        config[userFirstNameString] ?? "Regular",
                        config[userLastNameString] ?? $"User{i}");

                    if (!created.Succeeded)
                    {
                        throw new InvalidOperationException($"RegularUser{i} seed failed: {created.Error}");
                    }

                    user = created.Data!;
                }

                if (!await userManager.IsInRoleAsync(user, Roles.RegularUser))
                {
                    var result = await userManager.AddToRoleAsync(user, Roles.RegularUser);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Failed to grant REGULAR_USER role: {string.Join(" ", result.Errors.Select(e => e.Description))}");
                    }

                    logger.LogInformation("Granted REGULAR_USER role to user {UserId}.", user.Id);
                }
            }

        }
    }
}
