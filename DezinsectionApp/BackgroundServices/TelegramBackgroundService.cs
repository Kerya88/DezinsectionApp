using DezinsectionApp.Entities;
using DezinsectionApp.Services.Telegram;
using System.IO;
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
            try
            {
                await _infoBot.SendMessage(_configuration["InfoChatId"]!, message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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

        public async Task SendDocument(long chatId, InputFile document)
        {
            try
            {
                if (_storageBackgroundService.State)
                {
                    await _infoBot.SendDocument(chatId, document);
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

        public async Task<byte[]> GetFile(string fileId)
        {
            try
            {
                var file = await _infoBot.GetFile(fileId);

                if (file != null)
                {
                    await using (var ms = new MemoryStream())
                    {
                        await _infoBot.DownloadFile(file.FilePath!, ms);

                        return ms.ToArray();
                    }
                }
                else
                {
                    return [];
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return [];
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

                if (update is { Type: UpdateType.Message, Message: { From: not null, Chat.Type: ChatType.Private } })
                {
                    StorageBackgroundService.EmployeeStore.TryGetValue(update.Message.From.Id, out employee);
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
                else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery.Message: not null, CallbackQuery.Message.Chat.Type: ChatType.Private })
                {
                    StorageBackgroundService.EmployeeStore.TryGetValue(update.CallbackQuery.From.Id, out employee);
                    employeeId = update.CallbackQuery.From.Id;
                    chatType = update.CallbackQuery.Message.Chat.Type;
                }

                if (_storageBackgroundService.State)
                {
                    if (chatType is ChatType.Private)
                    {
                        if (employee == null)
                        {
                            employee = new Employee
                            {
                                UserActivityStateType = Enums.UserActivityStateType.NotSet,
                                City = string.Empty
                            };

                            StorageBackgroundService.EmployeeStore.Add(employeeId, employee);
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
