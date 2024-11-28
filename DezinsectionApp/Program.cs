
using DezinsectionApp.BackgroundServices;
using DezinsectionApp.Services.AmoCrm.Lead;
using DezinsectionApp.Services.Ezhkh;
using DezinsectionApp.Services.Telegram;
using GJIService;

namespace DezinsectionApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddTransient<IUrbanAppealService, UrbanAppealServiceClient>(provider =>
            {
                return new(UrbanAppealServiceClient.EndpointConfiguration.BasicHttpBinding_IUrbanAppealService);
            });
            builder.Services.AddSingleton<IEzhkhService, EzhkhService>();
            builder.Services.AddSingleton<StorageBackgroundService>();
            builder.Services.AddHostedService<StorageBackgroundService>();
            builder.Services.AddSingleton<TelegramBackgroundService>();
            builder.Services.AddHostedService<TelegramBackgroundService>();
            builder.Services.AddHostedService<RabbitMqBackgroundService>();
            builder.Services.AddTransient<ITelegramService, TelegramService>();
            builder.Services.AddTransient<IAmoCrmLeadService, AmoCrmLeadService>();

            var app = builder.Build();
            ServiceLocator.Init(app.Services);

            app.Run();
        }
    }
}
