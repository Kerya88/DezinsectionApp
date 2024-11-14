using DezinsectionApp.BackgroundServices;
using DezinsectionApp.Entities;
using GJIService;
using Telegram.Bot.Types;

namespace DezinsectionApp.Services.Telegram
{
    public interface ITelegramService
    {
        public Task ProcessMessage(TelegramBackgroundService service, Update update, Employee emloyee);
    }
}
