using Capstone.Identity.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Capstone.Identity.API.Tests.TestHelpers
{
    // this is a bit of a wonky class
    // not mocking these at all would have been easier...
    internal static class IdentityMocks
    {
        public static Mock<UserManager<ApplicationUser>> CreateUserManager() =>
        new(Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        public static Mock<SignInManager<ApplicationUser>> CreateSignInManager(
            UserManager<ApplicationUser> userManager) =>
            new(userManager,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
                null!, null!, null!, null!);
    }
}
