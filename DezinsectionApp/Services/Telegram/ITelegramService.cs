using DezinsectionApp.Entities;
using Telegram.Bot.Types;

namespace DezinsectionApp.Services.Telegram
{
    public interface ITelegramService
    {
        public Task SendInfoMessage(string message);
        public Task ProcessMessage(Update update, Employee employee);
        public Task NotifyCurator(AmoLead lead);
        public Task NotifyMaster(NotifyProxy notifyProxy);
    }
}
