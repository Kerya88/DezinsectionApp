using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace RequestReceiver.Authentication
{
    public class TokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        private const string SchemeName = "CustomToken";
        private readonly string _validToken = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes("sobits_millenium_actanon_verba")));

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Проверяем наличие заголовка Authorization
            if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader) || string.IsNullOrEmpty(tokenHeader))
            {
                return Task.FromResult(AuthenticateResult.Fail("Authorization header is missing."));
            }

            // Проверяем токен
            if (!tokenHeader.Equals($"Bearer {_validToken}"))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid token."));
            }

            // Создаем пользователя для аутентификации
            var claims = new[] { new Claim(ClaimTypes.Name, "AuthorizedUser") };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

}
