using System.Security.Cryptography;
using System.Text;

namespace RequestReceiver.Middlewares
{
    public class TokenAuthenticationMiddleware(RequestDelegate next)
    {
        private readonly string _validToken = Convert.ToBase64String(MD5.HashData(Encoding.UTF8.GetBytes("sobits_millenium_actanon_verba")));

        public async Task Invoke(HttpContext context)
        {
            if ((!context.Request.Headers.TryGetValue("Authorization", out var extractedToken) ||
                 !extractedToken.ToString().Equals($"Bearer {_validToken}", StringComparison.OrdinalIgnoreCase)) &&
                context.Request.Path.Value != null &&
                !context.Request.Path.Value.Contains("assignmaster"))
            {
                context.Response.StatusCode = 401; // Unauthorized
                await context.Response.WriteAsync("Unauthorized access");
                return;
            }

            await next(context);
        }
    }

}
