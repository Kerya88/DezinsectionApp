using DezinsectionApp.BackgroundServices;
using DezinsectionApp.Entities;
using GJIService;
using Telegram.Bot.Types;

namespace DezinsectionApp.Services.Telegram
{
    public interface ITelegramService
    {
        public Task ProcessMessage(Update update, Employee emloyee);
        public Task NotifyCurator(AmoLead lead);
        public Task NotifyMaster(NotifyProxy notifyProxy);
    }
}
