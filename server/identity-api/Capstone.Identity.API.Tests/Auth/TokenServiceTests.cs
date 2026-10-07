using Capstone.Identity.API.Auth;
using Capstone.Identity.API.Models;
using Capstone.Identity.API.Services;
using Capstone.Identity.API.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Capstone.Identity.API.Tests.Auth
{
    [TestClass]
    public class TokenServiceTests
    {
        private JwtOptions _jwtOptions = null!;
        private Mock<UserManager<ApplicationUser>> _userManager = null!;
        private TokenService _tokenService = null!;
        private ApplicationUser _user = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _jwtOptions = new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Key = "this-is-a-test-signing-key-that-is-long-enough-for-hs256",
                ExpiryMinutes = 60
            };

            _userManager = IdentityMocks.CreateUserManager();
            SetRoles();

            _tokenService = new TokenService(Options.Create(_jwtOptions), _userManager.Object);

            _user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "kyle@test.com",
                UserName = "kyle@test.com",
                FirstName = "Kyle",
                LastName = "Test"
            };
        }

        private void SetRoles(params string[] roles) =>
            _userManager
                .Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(roles.ToList());

        private static JwtSecurityToken Read(AccessToken token) =>
            new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        private TokenValidationParameters ValidationParameters(SecurityKey key) => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // ---------- Claims ----------

        [TestMethod]
        public async Task GenerateTokenAsync_IncludesUserIdClaim()
        {
            var token = Read(await _tokenService.GenerateTokenAsync(_user));

            var userIdClaim = token.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            Assert.IsNotNull(userIdClaim);
            Assert.AreEqual(_user.Id.ToString(), userIdClaim.Value);
        }

        [TestMethod]
        public async Task GenerateTokenAsync_IncludesOneClaimPerRole()
        {
            SetRoles("Admin", "Learner");

            var token = Read(await _tokenService.GenerateTokenAsync(_user));

            var roles = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            CollectionAssert.AreEquivalent(new[] { "Admin", "Learner" }, roles);
        }

        [TestMethod]
        public async Task GenerateTokenAsync_NoRoles_HasNoRoleClaims()
        {
            var token = Read(await _tokenService.GenerateTokenAsync(_user));

            Assert.IsFalse(token.Claims.Any(c => c.Type == ClaimTypes.Role));
        }

        [TestMethod]
        public async Task GenerateTokenAsync_DoesNotIncludeEmailClaim()
        {
            var token = Read(await _tokenService.GenerateTokenAsync(_user));

            Assert.IsFalse(token.Claims.Any(c =>
                c.Type == ClaimTypes.Email || c.Type == JwtRegisteredClaimNames.Email));
        }

        [TestMethod]
        public async Task GenerateTokenAsync_UserWithoutEmail_DoesNotThrow()
        {
            _user.Email = null;

            var result = await _tokenService.GenerateTokenAsync(_user);

            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Value));
        }

        // ---------- Issuer, audience, expiry, ID ----------

        [TestMethod]
        public async Task GenerateTokenAsync_SetsIssuerAndAudience()
        {
            var token = Read(await _tokenService.GenerateTokenAsync(_user));

            Assert.AreEqual(_jwtOptions.Issuer, token.Issuer);
            CollectionAssert.Contains(token.Audiences.ToList(), _jwtOptions.Audience);
        }

        [TestMethod]
        public async Task GenerateTokenAsync_TokenExpiryMatchesReturnedExpiresAt()
        {
            var result = await _tokenService.GenerateTokenAsync(_user);
            var token = Read(result);

            var difference = (token.ValidTo - result.ExpiresAt.UtcDateTime).Duration();
            Assert.IsTrue(difference < TimeSpan.FromSeconds(1),
                $"Token exp and ExpiresAt differ by {difference}.");
        }

        [TestMethod]
        public async Task GenerateTokenAsync_ExpiresAfterConfiguredMinutes()
        {
            var before = DateTimeOffset.UtcNow;

            var result = await _tokenService.GenerateTokenAsync(_user);

            var expected = before.AddMinutes(_jwtOptions.ExpiryMinutes);
            var difference = (result.ExpiresAt - expected).Duration();
            Assert.IsTrue(difference < TimeSpan.FromSeconds(5),
                $"ExpiresAt is {difference} away from the configured expiry.");
        }

        [TestMethod]
        public async Task GenerateTokenAsync_EachTokenHasUniqueJti()
        {
            var first = Read(await _tokenService.GenerateTokenAsync(_user));
            var second = Read(await _tokenService.GenerateTokenAsync(_user));

            Assert.AreNotEqual(first.Id, second.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(first.Id));
        }

        // ---------- Signing round trip ----------

        [TestMethod]
        public async Task GenerateTokenAsync_ValidatesWithSameKey()
        {
            var result = await _tokenService.GenerateTokenAsync(_user);

            var principal = new JwtSecurityTokenHandler().ValidateToken(
                result.Value, ValidationParameters(_jwtOptions.GetSigningKey()), out _);

            Assert.IsNotNull(principal);
        }

        [TestMethod]
        public async Task GenerateTokenAsync_FailsValidationWithDifferentKey()
        {
            var result = await _tokenService.GenerateTokenAsync(_user);
            var wrongKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("a-completely-different-key-that-is-also-long-enough"));

            var threw = false;
            try
            {
                new JwtSecurityTokenHandler().ValidateToken(result.Value, ValidationParameters(wrongKey), out _);
            }
            catch (SecurityTokenException)
            {
                threw = true;
            }

            Assert.IsTrue(threw, "A token signed with one key must not validate with another.");
        }
    }
}
