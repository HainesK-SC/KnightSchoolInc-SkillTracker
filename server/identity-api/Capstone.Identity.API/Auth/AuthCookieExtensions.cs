namespace Capstone.Identity.API.Auth
{
    public static class AuthCookieExtensions
    {
        public static void SetAuthCookie(this HttpResponse response, AccessToken token) =>
       response.Cookies.Append(AuthCookie.Name, token.Value, AuthCookie.Options(token.ExpiresAt));

        public static void ClearAuthCookie(this HttpResponse response) =>
            response.Cookies.Delete(AuthCookie.Name, AuthCookie.Options(DateTimeOffset.UnixEpoch));
    }
}
