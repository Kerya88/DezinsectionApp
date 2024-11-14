using Telegram.Bot.Types.ReplyMarkups;

namespace DezinsectionApp.BackgroundServices
{
    public interface ITelegramBackgroundService
    {
        public Task SendInfoMessage(string message);
        public Task SendMessage(long chatId, string message, IReplyMarkup? replyMarkup = default);
    }
}
