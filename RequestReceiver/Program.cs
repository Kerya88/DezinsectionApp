
using Microsoft.AspNetCore.Authentication;
using RequestReceiver.Authentication;
using RequestReceiver.Services.RabbitMq;

namespace RequestReceiver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton<RabbitMqService>();

            builder.Services.AddAuthentication("CustomToken")
                .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("CustomToken", null);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
