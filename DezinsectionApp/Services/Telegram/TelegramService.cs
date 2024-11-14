using DezinsectionApp.BackgroundServices;
using DezinsectionApp.Entities;
using DezinsectionApp.Enums;
using DezinsectionApp.Extentions;
using DezinsectionApp.Services.Ezhkh;
using GJIService;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DezinsectionApp.Services.Telegram
{
    public class TelegramService : ITelegramService
    {
        private readonly IEzhkhService _ezhkhService;
        private readonly Regex _fioRegex = new(@"^[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}$");
        private readonly Regex _phoneRegex = new(@"^(\+7|8)9\d{9}$");

        public TelegramService(IEzhkhService ezhkhService)
        {
            _ezhkhService = ezhkhService;
        }

        public async Task ProcessMessage(TelegramBackgroundService service, Update update, Employee emloyee)
        {
            if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text && !string.IsNullOrEmpty(update.Message.Text))
            {
                var message = update.Message;

                if (message.Text == "8fce83fc-e31b-4f3c-bada-734113326662")
                {
                    service.State = true;
                    await service.SendMessage(message.Chat.Id, "Бот активирован");
                    return;
                }

                if (message.Text == "e5287157-6549-4da7-97ab-6ba5e12c76db")
                {
                    service.State = false;
                    await service.SendMessage(message.Chat.Id, "Бот деактивирован");
                    return;
                }

                if (!service.State)
                {
                    await service.SendMessage(message.Chat.Id, "Бот деактивирован");
                    return;
                }

                switch (message.Text)
                {
                    case "/start":
                        {
                            if (string.IsNullOrEmpty(emloyee.TelegramID))
                            {
                                var replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                                    {
                                        ["Зарегестрироваться"]
                                    }
                                );
                                replyKeyboardMarkup.ResizeKeyboard = true;

                                await service.SendMessage(message.Chat.Id, "Вам необходимо зарегестрироваться в системе, для этого нажмите кнопку \"Зарегистрироваться\" в нижней части окна telegram и ответьте на несколько вопросов", replyKeyboardMarkup);
                            }
                            else
                            {
                                var replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                                    {
                                        ["Получить отчет за день"]
                                    }
                                );
                                replyKeyboardMarkup.ResizeKeyboard = true;

                                await service.SendMessage(message.Chat.Id, $"Здравствуйте, {emloyee.FIO}", replyKeyboardMarkup);
                            }

                            break;
                        }
                    case "Зарегестрироваться":
                        {
                            if (string.IsNullOrEmpty(emloyee.City))
                            {
                                await service.SendMessage(message.Chat.Id, "Введите Ваше ФИО, каждое слово с большой буквы");

                                emloyee.UserActivityStateType = UserActivityStateType.FIO;
                            }
                            else
                            {
                                await service.SendMessage(message.Chat.Id, "Вы уже зарегестрированы");
                            }

                            break;
                        }
                    case "Получить отчет за день":
                        {
                            if (!string.IsNullOrEmpty(emloyee.TelegramID))
                            {
                                await service.SendMessage(message.Chat.Id, "Вы получили отчет");
                            }
                            else
                            {
                                goto case "/start";
                            }
                            break;
                        }
                    default:
                        {
                            switch (emloyee.UserActivityStateType)
                            {
                                case UserActivityStateType.FIO:
                                    {
                                        var fio = message.Text.Trim();

                                        if (_fioRegex.IsMatch(fio))
                                        {
                                            emloyee.FIO = fio;
                                            emloyee.UserActivityStateType = UserActivityStateType.Phone;
                                            await service.SendMessage(message.Chat.Id, "Введите Ваше номер телефона в формате +79123456789");
                                        }
                                        else
                                        {
                                            await service.SendMessage(message.Chat.Id, "Введенное ФИО не соответствует формату");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.Phone:
                                    {
                                        var phone = message.Text.Trim();

                                        if (_phoneRegex.IsMatch(phone))
                                        {
                                            emloyee.Phone = phone;
                                            emloyee.UserActivityStateType = UserActivityStateType.Type;

                                            var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                                {
                                                    ["Администратор"],
                                                    ["Куратор"],
                                                    ["Мастер"],
                                                    ["Оператор"]
                                                }
                                            );
                                            //replyKeyboardMarkup.ResizeKeyboard = true;

                                            await service.SendMessage(message.Chat.Id, "Выберите тип сотрудника", replyKeyboardMarkup);
                                        }
                                        else
                                        {
                                            await service.SendMessage(message.Chat.Id, "Введенный номер телефона не соответствует формату");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.NotSet:
                                    {
                                        if (string.IsNullOrEmpty(emloyee.TelegramID))
                                        {
                                            var replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                                                {
                                                    ["Зарегестрироваться"]
                                                }
                                            );
                                            replyKeyboardMarkup.ResizeKeyboard = true;

                                            await service.SendMessage(message.Chat.Id, "Вам необходимо зарегестрироваться в системе, для этого нажмите кнопку \"Зарегистрироваться\" в нижней части окна telegram и ответьте на несколько вопросов", replyKeyboardMarkup);
                                        }
                                        else
                                        {
                                            await service.SendMessage(message.Chat.Id, "Вы не выбрали команду");
                                        }
                                        break;
                                    }
                            }

                            break;
                        }
                }
            }
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null && update.CallbackQuery.Data != null && update.CallbackQuery.Message != null)
            {
                switch (emloyee.UserActivityStateType)
                {
                    case UserActivityStateType.Type:
                        {
                            if (default(EmployeeType).TryParseDisplayNameToEnumValue(update.CallbackQuery.Data, out var result))
                            {
                                emloyee.EmployeeType = result;
                                emloyee.UserActivityStateType = UserActivityStateType.City;

                                var citys = service.Citys;
                                var rowsCount = citys.Length % 2 == 0 ? citys.Length / 2 + 1 : citys.Length / 2 + 2;

                                var keyboardButtons = new InlineKeyboardButton[rowsCount][];

                                if (citys.Length % 2 == 0)
                                {
                                    for (var i = 0; i < rowsCount; i++)
                                    {
                                        keyboardButtons[i] = [citys[2 * i].Name, citys[2 * i + 1].Name];
                                    }
                                }
                                else
                                {
                                    for (var i = 0; i < rowsCount - 2; i++)
                                    {
                                        keyboardButtons[i] = [citys[2 * i].Name, citys[2 * i + 1].Name];
                                    }

                                    keyboardButtons[rowsCount - 2] = [citys.Last().Name];
                                }

                                keyboardButtons[rowsCount - 1] = ["Завершить выбор"];

                                var replyKeyboardMarkup = new InlineKeyboardMarkup(keyboardButtons);
                                //replyKeyboardMarkup.ResizeKeyboard = true;

                                await service.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);
                                break;
                            }
                            else
                            {
                                await service.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введенный тип сотрудника не существует");
                            }

                            break;
                        }
                    case UserActivityStateType.City:
                        {
                            if (update.CallbackQuery.Data == "Завершить выбор")
                            {
                                if (string.IsNullOrEmpty(emloyee.City))
                                {
                                    await service.SendMessage(update.CallbackQuery.Message.Chat.Id, "Необходимо выбрать хотя бы один город");

                                    var citys = service.Citys;
                                    var rowsCount = citys.Length % 2 == 0 ? citys.Length / 2 + 1 : citys.Length / 2 + 2;

                                    var keyboardButtons = new InlineKeyboardButton[rowsCount][];

                                    if (citys.Length % 2 == 0)
                                    {
                                        for (var i = 0; i < rowsCount; i++)
                                        {
                                            keyboardButtons[i] = [citys[2 * i].Name, citys[2 * i + 1].Name];
                                        }
                                    }
                                    else
                                    {
                                        for (var i = 0; i < rowsCount - 2; i++)
                                        {
                                            keyboardButtons[i] = [citys[2 * i].Name, citys[2 * i + 1].Name];
                                        }

                                        keyboardButtons[rowsCount - 2] = [citys.Last().Name];
                                    }

                                    keyboardButtons[rowsCount - 1] = ["Завершить выбор"];

                                    var replyKeyboardMarkup = new InlineKeyboardMarkup(keyboardButtons);

                                    await service.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);

                                    break;
                                }

                                await service.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Вы выбрали следующие города: {emloyee.City}");

                                emloyee.TelegramID = update.CallbackQuery.From.Id.ToString();

                                var ezhkhEmployee = new SESEmployerProxy
                                {
                                    TelegramID = emloyee.TelegramID,
                                    FIO = emloyee.FIO,
                                    Phone = emloyee.Phone,
                                    Cities = emloyee.City,
                                    EmplType = ((int)emloyee.EmployeeType).ToString()
                                };

                                var registrSuccess = await _ezhkhService.RegisterNewEmployee(ezhkhEmployee);

                                if (registrSuccess)
                                {
                                    emloyee.UserActivityStateType = UserActivityStateType.NotSet;

                                    await service.SendMessage(update.CallbackQuery.Message.Chat.Id, "Вы успешно зарегестрировались");

                                    var replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                                        {
                                            ["Получить отчет за день"]
                                        }
                                    );
                                    replyKeyboardMarkup.ResizeKeyboard = true;

                                    await service.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Здравствуйте, {emloyee.FIO}", replyKeyboardMarkup);
                                }
                                else
                                {
                                    emloyee.Clear();

                                    await service.SendMessage(update.CallbackQuery.Message.Chat.Id, "При регистрации произошла ошибка, попробуйте позже");
                                }
                            }
                            else
                            {
                                var splitCitys = emloyee.City.Split(',').ToList();

                                if (string.IsNullOrEmpty(emloyee.City))
                                {
                                    emloyee.City = update.CallbackQuery.Data;
                                    splitCitys.Add(update.CallbackQuery.Data);
                                }
                                else
                                {
                                    if (splitCitys.Contains(update.CallbackQuery.Data.Replace("✅", "")))
                                    {
                                        splitCitys.Remove(update.CallbackQuery.Data.Replace("✅", ""));
                                        emloyee.City = string.Join(",", splitCitys);
                                    }
                                    else
                                    {
                                        splitCitys.Add(update.CallbackQuery.Data.Replace("✅", ""));
                                        emloyee.City = string.Join(",", splitCitys);
                                    }
                                }

                                var citys = service.Citys.Select(x => x.Name).Select(x =>
                                {
                                    if (splitCitys.Contains(x))
                                    {
                                        x = "✅" + x;
                                    }

                                    return x;
                                })
                                .ToArray();

                                var rowsCount = citys.Length % 2 == 0 ? citys.Length / 2 + 1 : citys.Length / 2 + 2;

                                var keyboardButtons = new InlineKeyboardButton[rowsCount][];

                                if (citys.Length % 2 == 0)
                                {
                                    for (var i = 0; i < rowsCount; i++)
                                    {
                                        keyboardButtons[i] = [citys[2 * i], citys[2 * i + 1]];
                                    }
                                }
                                else
                                {
                                    for (var i = 0; i < rowsCount - 2; i++)
                                    {
                                        keyboardButtons[i] = [citys[2 * i], citys[2 * i + 1]];
                                    }

                                    keyboardButtons[rowsCount - 2] = [citys.Last()];
                                }

                                keyboardButtons[rowsCount - 1] = ["Завершить выбор"];

                                var replyKeyboardMarkup = new InlineKeyboardMarkup(keyboardButtons);

                                await service.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);
                            }

                            break;
                        }
                }
            }
        }
    }
}
