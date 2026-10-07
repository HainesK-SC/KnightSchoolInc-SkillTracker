using Capstone.Identity.API.Auth;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Capstone.Identity.API.Tests.Auth
{
    [TestClass]
    public class AuthCookieExtensionsTests
    {
        private static string GetSetCookieHeader(HttpContext context) =>
        context.Response.Headers.SetCookie.ToString();

        [TestMethod]
        public void SetAuthCookie_WritesNameAndTokenValue()
        {
            var context = new DefaultHttpContext();
            var token = new AccessToken("test-token-value", DateTimeOffset.UtcNow.AddHours(1));

            context.Response.SetAuthCookie(token);

            StringAssert.Contains(GetSetCookieHeader(context), $"{AuthCookie.Name}=test-token-value");
        }

        [TestMethod]
        public void SetAuthCookie_SetsSecurityAttributes()
        {
            var context = new DefaultHttpContext();
            var token = new AccessToken("test-token-value", DateTimeOffset.UtcNow.AddHours(1));

            context.Response.SetAuthCookie(token);

            var header = GetSetCookieHeader(context).ToLowerInvariant();
            StringAssert.Contains(header, "httponly");
            StringAssert.Contains(header, "secure");
            StringAssert.Contains(header, "samesite=lax");
            StringAssert.Contains(header, "path=/");
        }

        [TestMethod]
        public void SetAuthCookie_ExpiryMatchesToken()
        {
            var context = new DefaultHttpContext();
            var expiresAt = new DateTimeOffset(2030, 1, 15, 12, 0, 0, TimeSpan.Zero);
            var token = new AccessToken("test-token-value", expiresAt);

            context.Response.SetAuthCookie(token);

            var header = GetSetCookieHeader(context).ToLowerInvariant();
            var expectedExpiry = $"expires={expiresAt.ToString("R")}".ToLowerInvariant();
            StringAssert.Contains(header, expectedExpiry);
        }

        [TestMethod]
        public void ClearAuthCookie_ExpiresCookieWithSamePath()
        {
            var context = new DefaultHttpContext();

            context.Response.ClearAuthCookie();

            var header = GetSetCookieHeader(context).ToLowerInvariant();
            StringAssert.Contains(header, $"{AuthCookie.Name.ToLowerInvariant()}=;");
            StringAssert.Contains(header, "expires=thu, 01 jan 1970 00:00:00 gmt");
            StringAssert.Contains(header, "path=/");
        }
    }
}
