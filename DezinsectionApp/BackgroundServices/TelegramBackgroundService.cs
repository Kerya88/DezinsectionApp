using DezinsectionApp.Entities;
using DezinsectionApp.Services.Ezhkh;
using DezinsectionApp.Services.Telegram;
using GJIService;
using System.ServiceModel.Channels;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DezinsectionApp.BackgroundServices
{
    public class TelegramBackgroundService : BackgroundService, ITelegramBackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ITelegramService _telegramService;
        private readonly IEzhkhService _ezhkhService;
        private readonly TelegramBotClient _infoBot;

        private Dictionary<long, Employee> _employeeStore;

        public bool State { get; set; }
        public CrmCityProxy[] Citys { get; set; }

        public TelegramBackgroundService(IConfiguration configuration, ITelegramService telegramService, IEzhkhService ezhkhService)
        {
            _configuration = configuration;
            _telegramService = telegramService;
            _ezhkhService = ezhkhService;

            _infoBot = new TelegramBotClient(_configuration["Token"]!);

            State = true;
        }

        public async Task SendInfoMessage(string message)
        {
            await _infoBot.SendMessage(_configuration["InfoChatId"]!, message);
        }

        public async Task SendMessage(long chatId, string message, IReplyMarkup? replyMarkup = default)
        {
            if (State)
            {
                await _infoBot.SendMessage(chatId, message, replyMarkup: replyMarkup);
            }
            else
            {
                await _infoBot.SendMessage(chatId, "Бот деактивирован");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await UpdateState(stoppingToken);

            _infoBot.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandleErrorAsync), cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task UpdateState(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var foundedEmployees = await _ezhkhService.GetRegistredEmployees();
                var citys = await _ezhkhService.GetCrmCity();

                if (foundedEmployees != null && citys != null)
                {
                    _employeeStore = new(foundedEmployees.Length);
                    foundedEmployees.ToList().ForEach(x =>
                    {
                        var successParse = long.TryParse(x.TelegramID, out var tgId);

                        if (successParse)
                        {
                            _employeeStore.Add(tgId, new Employee
                            {
                                TelegramID = tgId.ToString(),
                                Phone = x.Phone,
                                FIO = x.FIO,
                                UserActivityStateType = Enums.UserActivityStateType.NotSet
                            });
                        }
                    });

                    Citys = citys;
                }
                else
                {
                    Console.WriteLine("Не удалось получить данные из сервиса");
                    throw new Exception("Не удалось получить данные из сервиса");
                }

                // Ждем 10 минут перед следующим обновлением
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message != null && update.Message.From != null && update.Message.Chat.Type == ChatType.Private)
            {
                var emloyeeFounded = _employeeStore.TryGetValue(update.Message.From.Id, out var emloyee);

                if (!emloyeeFounded)
                {
                    emloyee = new Employee
                    {
                        UserActivityStateType = Enums.UserActivityStateType.NotSet,
                        City = string.Empty
                    };

                    _employeeStore.Add(update.Message.From.Id, emloyee);
                }

                await _telegramService.ProcessMessage(this, update, emloyee!);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                var emloyeeFounded = _employeeStore.TryGetValue(update.CallbackQuery.From.Id, out var emloyee);

                if (!emloyeeFounded)
                {
                    emloyee = new Employee
                    {
                        UserActivityStateType = Enums.UserActivityStateType.NotSet,
                        City = string.Empty
                    };

                    _employeeStore.Add(update.Message.From.Id, emloyee);
                }

                await _telegramService.ProcessMessage(this, update, emloyee!);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
