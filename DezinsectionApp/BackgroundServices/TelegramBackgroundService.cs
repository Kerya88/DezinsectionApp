using DezinsectionApp.Entities;
using DezinsectionApp.Services.Telegram;
using System.ServiceModel.Channels;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DezinsectionApp.BackgroundServices
{
    public class TelegramBackgroundService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly TelegramBotClient _infoBot;
        private readonly StorageBackgroundService _storageBackgroundService;

        public TelegramBackgroundService(IConfiguration configuration, IServiceProvider serviceProvider, StorageBackgroundService storageBackgroundService)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _infoBot = new TelegramBotClient(_configuration["Token"]!);
            _storageBackgroundService = storageBackgroundService;
        }

        public async Task SendInfoMessage(string message)
        {
            await _infoBot.SendMessage(_configuration["InfoChatId"]!, message);
        }

        public async Task SendMessage(long chatId, string message, IReplyMarkup? replyMarkup = default)
        {
            try
            {
                if (_storageBackgroundService.State)
                {
                    await _infoBot.SendMessage(chatId, message, replyMarkup: replyMarkup);
                }
                else
                {
                    await _infoBot.SendMessage(chatId, "Бот деактивирован");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public async Task SendMessage(string chatId, string message, IReplyMarkup? replyMarkup = default)
        {
            try
            {
                if (_storageBackgroundService.State)
                {
                    await _infoBot.SendMessage(chatId, message, replyMarkup: replyMarkup);
                }
                else
                {
                    await _infoBot.SendMessage(chatId, "Бот деактивирован");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _infoBot.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();

                Employee? employee = null;
                long employeeId = 0;
                ChatType? chatType = null;

                if (update.Type == UpdateType.Message && update.Message != null && update.Message.From != null && update.Message.Chat.Type == ChatType.Private)
                {
                    _storageBackgroundService.EmployeeStore.TryGetValue(update.Message.From.Id, out employee);
                    employeeId = update.Message.From.Id;
                    chatType = update.Message.Chat.Type;

                    if (update.Message.Text == "8fce83fc-e31b-4f3c-bada-734113326662")
                    {
                        _storageBackgroundService.State = true;
                        await _infoBot.SendMessage(update.Message.Chat.Id, "Бот активирован");
                        return;
                    }

                    if (update.Message.Text == "e5287157-6549-4da7-97ab-6ba5e12c76db")
                    {
                        _storageBackgroundService.State = false;
                        await _infoBot.SendMessage(update.Message.Chat.Id, "Бот деактивирован");
                        return;
                    }
                }
                else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null && update.CallbackQuery.Message != null && update.CallbackQuery.Message.Chat.Type == ChatType.Private)
                {
                    _storageBackgroundService.EmployeeStore.TryGetValue(update.CallbackQuery.From.Id, out employee);
                    employeeId = update.CallbackQuery.From.Id;
                    chatType = update.CallbackQuery.Message.Chat.Type;
                }

                if (_storageBackgroundService.State)
                {
                    if (chatType != null && chatType == ChatType.Private)
                    {
                        if (employee == null)
                        {
                            employee = new Employee
                            {
                                UserActivityStateType = Enums.UserActivityStateType.NotSet,
                                City = string.Empty
                            };

                            _storageBackgroundService.EmployeeStore.Add(employeeId, employee);
                        }

                        await telegramService.ProcessMessage(update, employee);
                    }
                }
                else
                {
                    await _infoBot.SendMessage(employeeId, "Бот деактивирован");
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
