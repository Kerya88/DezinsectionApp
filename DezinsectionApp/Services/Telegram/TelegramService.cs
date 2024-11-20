using DezinsectionApp.BackgroundServices;
using DezinsectionApp.Entities;
using DezinsectionApp.Enums;
using DezinsectionApp.Extentions;
using DezinsectionApp.Services.AmoCrm.Lead;
using DezinsectionApp.Services.Ezhkh;
using GJIService;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DezinsectionApp.Services.Telegram
{
    public class TelegramService(IEzhkhService ezhkhService, StorageBackgroundService storageBackgroundService, TelegramBackgroundService telegramBackgroundService) : ITelegramService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        private readonly IEzhkhService _ezhkhService = ezhkhService;
        private readonly StorageBackgroundService _storageBackgroundService = storageBackgroundService;
        private readonly TelegramBackgroundService _telegramBackgroundService = telegramBackgroundService;
        private readonly Regex _fioRegex = new(@"^[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}$");
        private readonly Regex _phoneRegex = new(@"^(\+7|8)9\d{9}$");

        public async Task ProcessMessage(Update update, Employee employee)
        {
            if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text && !string.IsNullOrEmpty(update.Message.Text))
            {
                var message = update.Message;

                switch (message.Text)
                {
                    case "/start":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                var replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                                    {
                                        ["Зарегистрироваться"]
                                    }
                                );
                                replyKeyboardMarkup.ResizeKeyboard = true;

                                await _telegramBackgroundService.SendMessage(message.Chat.Id, "Вам необходимо зарегистрироваться в системе, для этого нажмите кнопку \"Зарегистрироваться\" в нижней части окна telegram и ответьте на несколько вопросов", replyKeyboardMarkup);
                            }
                            else
                            {
                                var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);

                                await _telegramBackgroundService.SendMessage(message.Chat.Id, $"Здравствуйте, {employee.FIO}", replyKeyboardMarkup);
                            }

                            break;
                        }
                    case "Зарегистрироваться":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                await _telegramBackgroundService.SendMessage(message.Chat.Id, "Введите Ваше ФИО, каждое слово с большой буквы");

                                employee.UserActivityStateType = UserActivityStateType.FIO;
                            }
                            else
                            {
                                await _telegramBackgroundService.SendMessage(message.Chat.Id, "Вы уже зарегистрированы");

                                var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);

                                await _telegramBackgroundService.SendMessage(message.Chat.Id, $"Здравствуйте, {employee.FIO}", replyKeyboardMarkup);
                            }

                            break;
                        }
                    case "Получить отчет за день":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                goto case "/start";
                            }
                            else
                            {
                                await _telegramBackgroundService.SendMessage(message.Chat.Id, "Вы получили отчет");
                            }
                            break;
                        }
                    case "Мои сделки":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                goto case "/start";
                            }
                            else
                            {
                                switch (employee.EmployeeType)
                                {
                                    case EmployeeType.Curator:
                                        {
                                            var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                                {
                                                    ["По которым назначен мастер"],
                                                    ["По которым НЕ назначен мастер"]
                                                }
                                            );

                                            await _telegramBackgroundService.SendMessage(message.Chat.Id, "Какие сделки вас интересуют?", replyKeyboardMarkup);

                                            break;
                                        }
                                    case EmployeeType.ExternalCurator:
                                        {
                                            goto case EmployeeType.Curator;
                                        }
                                    case EmployeeType.Master:
                                        {
                                            var deals = await _ezhkhService.GetMyDeals(employee.TelegramID);

                                            if (deals == null)
                                            {
                                                await _telegramBackgroundService.SendMessage(message.Chat.Id, "Сервис недоступен");
                                                break;
                                            }

                                            if (deals.Length > 0)
                                            {
                                                foreach (var deal in deals)
                                                {
                                                    var stringDeal = string.Join("\n", $"Сумма: {deal.Budget}", $"Дата и время визита: {deal.DealDateTime}", $"Комментарий: {deal.DealDetails}");

                                                    await _telegramBackgroundService.SendMessage(message.Chat.Id, stringDeal);
                                                }
                                            }
                                            else
                                            {
                                                await _telegramBackgroundService.SendMessage(message.Chat.Id, "У вас нет сделок");
                                            }

                                            break;
                                        }
                                }
                            }
                            break;
                        }
                    default:
                        {
                            switch (employee.UserActivityStateType)
                            {
                                case UserActivityStateType.FIO:
                                    {
                                        var fio = message.Text.Trim();

                                        if (_fioRegex.IsMatch(fio))
                                        {
                                            employee.FIO = fio;
                                            employee.UserActivityStateType = UserActivityStateType.Phone;
                                            await _telegramBackgroundService.SendMessage(message.Chat.Id, "Введите Ваше номер телефона в формате +79123456789");
                                        }
                                        else
                                        {
                                            await _telegramBackgroundService.SendMessage(message.Chat.Id, "Введенное ФИО не соответствует формату");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.Phone:
                                    {
                                        var phone = message.Text.Trim();

                                        if (_phoneRegex.IsMatch(phone))
                                        {
                                            employee.Phone = phone;
                                            employee.UserActivityStateType = UserActivityStateType.Type;

                                            var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                                {
                                                    ["Администратор"],
                                                    ["Куратор"],
                                                    ["Мастер"],
                                                    ["Оператор"]
                                                }
                                            );

                                            await _telegramBackgroundService.SendMessage(message.Chat.Id, "Выберите тип сотрудника", replyKeyboardMarkup);
                                        }
                                        else
                                        {
                                            await _telegramBackgroundService.SendMessage(message.Chat.Id, "Введенный номер телефона не соответствует формату");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.NotSet:
                                    {
                                        if (string.IsNullOrEmpty(employee.TelegramID))
                                        {
                                            var replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                                                {
                                                    ["Зарегистрироваться"]
                                                }
                                            );
                                            replyKeyboardMarkup.ResizeKeyboard = true;

                                            await _telegramBackgroundService.SendMessage(message.Chat.Id, "Вам необходимо зарегистрироваться в системе, для этого нажмите кнопку \"Зарегистрироваться\" в нижней части окна telegram и ответьте на несколько вопросов", replyKeyboardMarkup);
                                        }
                                        else
                                        {
                                            var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);
                                            await _telegramBackgroundService.SendMessage(message.Chat.Id, "Вы не выбрали команду", replyKeyboardMarkup);
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
                switch (update.CallbackQuery.Data.Split("%")[0])
                {
                    case "Наз":
                        {
                            var city = update.CallbackQuery.Data.Split("%")[2];
                            var date = update.CallbackQuery.Data.Split("%")[3];

                            var curatorAndMasters = await _ezhkhService.GetKurator(city, date);

                            if (curatorAndMasters == null)
                            {
                                await _telegramBackgroundService.SendInfoMessage($"Сервис недоступен, не удалось назначить мастера на заявку id = \n{update.CallbackQuery.Data.Split("%")[1]}");
                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
                                break;
                            }

                            var masters = curatorAndMasters.SESMasterProxyes.ToList();

                            var buttons = new InlineKeyboardButton[masters.Count][];
                            for (int i = 0; i < masters.Count; i++)
                            {
                                buttons[i] = [InlineKeyboardButton.WithCallbackData(masters[i].MasterData, update.CallbackQuery.Data.Replace("Наз%", "") + "%" + masters[i].TelegramID)];
                            }

                            employee.UserActivityStateType = UserActivityStateType.AssignMaster;

                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Доступные мастера", new InlineKeyboardMarkup(buttons));

                            break;
                        }
                    case "По которым назначен мастер":
                        {
                            var deals = await _ezhkhService.GetMyDeals(employee.TelegramID, true);

                            if (deals == null)
                            {
                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
                                break;
                            }

                            if (deals.Length > 0)
                            {
                                foreach (var deal in deals)
                                {
                                    //var stringDeal = string.Join("\n", $"Мастер: {_storageBackgroundService.EmployeeStore[long.Parse(deal.MasterTelegramID)].FIO}", $"Сумма: {deal.Budget}", $"Дата и время визита: {deal.DealDateTime}", $"Комментарий: {deal.DealDetails}");
                                    var stringDeal = string.Join("\n", $"Мастер: {deal.MasterData}", "{deal.DealDetails}");

                                    var city = deal.DealDetails.Split("Город: ")[1].Split("\n")[0];
                                    var date = deal.DealDetails.Split("визита: ")[1].Split("\n")[0];

                                    var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                        {
                                            [InlineKeyboardButton.WithCallbackData("Изменить мастера", $"Наз%{deal.DealId}%{city}%{date}")],
                                        }
                                    );

                                    await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, stringDeal, replyKeyboardMarkup);
                                }
                            }
                            else
                            {
                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "У вас нет сделок");
                            }

                            

                            break;
                        }
                    case "По которым НЕ назначен мастер":
                        {
                            var deals = await _ezhkhService.GetMyDeals(employee.TelegramID, false);

                            if (deals == null)
                            {
                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
                                break;
                            }

                            if (deals.Length > 0)
                            {
                                foreach (var deal in deals)
                                {
                                    var city = deal.DealDetails.Split("Город: ")[1].Split("\n")[0];
                                    var date = deal.DealDetails.Split("визита: ")[1].Split("\n")[0];

                                    var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                        {
                                            [InlineKeyboardButton.WithCallbackData("Назначить мастера", $"Наз%{deal.DealId}%{city}%{date}")],
                                        }
                                    );

                                    await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, deal.DealDetails, replyKeyboardMarkup);
                                }
                            }
                            else
                            {
                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "По всем сделкам мастер назначен");
                            }

                            break;
                        }
                    case "Из":
                        {
                            goto case "Наз";
                        }
                    default:
                        {
                            switch (employee.UserActivityStateType)
                            {
                                case UserActivityStateType.Type:
                                    {
                                        if (default(EmployeeType).TryParseDisplayNameToEnumValue(update.CallbackQuery.Data, out var result))
                                        {
                                            employee.EmployeeType = result;
                                            employee.UserActivityStateType = UserActivityStateType.City;

                                            var citys = _storageBackgroundService.Citys;
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

                                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);
                                            break;
                                        }
                                        else
                                        {
                                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введенный тип сотрудника не существует");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.City:
                                    {
                                        if (update.CallbackQuery.Data == "Завершить выбор")
                                        {
                                            if (string.IsNullOrEmpty(employee.City))
                                            {
                                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Необходимо выбрать хотя бы один город");

                                                var citys = _storageBackgroundService.Citys;
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

                                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);

                                                break;
                                            }

                                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Вы выбрали следующие города: {employee.City}");

                                            employee.TelegramID = update.CallbackQuery.From.Id.ToString();

                                            var ezhkhEmployee = new SESEmployerProxy
                                            {
                                                TelegramID = employee.TelegramID,
                                                FIO = employee.FIO,
                                                Phone = employee.Phone,
                                                Cities = employee.City,
                                                EmplType = ((int)employee.EmployeeType).ToString()
                                            };

                                            var registrSuccess = await _ezhkhService.RegisterNewEmployee(ezhkhEmployee);

                                            if (registrSuccess)
                                            {
                                                employee.UserActivityStateType = UserActivityStateType.NotSet;

                                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Вы успешно зарегистрировались");

                                                var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);

                                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Здравствуйте, {employee.FIO}", replyKeyboardMarkup);
                                            }
                                            else
                                            {
                                                employee.Clear();

                                                await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "При регистрации произошла ошибка, попробуйте позже");
                                            }
                                        }
                                        else
                                        {
                                            var splitCitys = employee.City.Split(',').ToList();

                                            if (string.IsNullOrEmpty(employee.City))
                                            {
                                                employee.City = update.CallbackQuery.Data;
                                                splitCitys.Add(update.CallbackQuery.Data);
                                            }
                                            else
                                            {
                                                if (splitCitys.Contains(update.CallbackQuery.Data.Replace("✅", "")))
                                                {
                                                    splitCitys.Remove(update.CallbackQuery.Data.Replace("✅", ""));
                                                    employee.City = string.Join(",", splitCitys);
                                                }
                                                else
                                                {
                                                    splitCitys.Add(update.CallbackQuery.Data.Replace("✅", ""));
                                                    employee.City = string.Join(",", splitCitys);
                                                }
                                            }

                                            var citys = _storageBackgroundService.Citys.Select(x => x.Name).Select(x =>
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

                                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);
                                        }

                                        break;
                                    }
                                case UserActivityStateType.AssignMaster:
                                    {
                                        var masterTgId = update.CallbackQuery.Data.Split("%")[3];
                                        var leadId = update.CallbackQuery.Data.Split("%")[0];
                                        var master = _storageBackgroundService.EmployeeStore[long.Parse(masterTgId)];

                                        var leads = await _ezhkhService.GetMyDeals(update.CallbackQuery.Message.Chat.Id.ToString());

                                        if (leads == null)
                                        {
                                            await _telegramBackgroundService.SendInfoMessage($"Сервис недоступен, не удалось назначить мастера на заявку с id = {leadId}");
                                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
                                            employee.UserActivityStateType = UserActivityStateType.NotSet;
                                            break;
                                        }

                                        var lead = leads.ToList().Where(x => x.DealId == leadId).FirstOrDefault();

                                        employee.UserActivityStateType = UserActivityStateType.NotSet;

                                        var success = await AssignMaster(lead!, master);

                                        if (!success)
                                        {
                                            await _telegramBackgroundService.SendInfoMessage($"Не удалось назначить мастера или смена статуса!\n\n{lead}");
                                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Не удалось назначить мастера");
                                        }
                                        else
                                        {
                                            await _telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Назначен мастер {master.FIO}");
                                        }

                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
        }

        public async Task NotifyCurator(AmoLead lead)
        {
            var cityField = lead.custom_fields_values?.FirstOrDefault(x => x.field_id == 1077293);
            var city = string.Empty;
            if (cityField != null)
            {
                city = cityField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var dateField = lead.custom_fields_values?.FirstOrDefault(x => x.field_id == 1077571);
            var date = string.Empty;
            if (dateField != null)
            {
                var unixDate = dateField.values.Select(x => int.Parse(x.value.ToString())).FirstOrDefault();
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var dateTime = epoch.AddSeconds(unixDate);
                var localDateTime = dateTime.ToLocalTime();
                date = localDateTime.ToString("g");
            }

            var commentField = lead!.custom_fields_values?.FirstOrDefault(x => x.field_id == 1077577);
            var comment = string.Empty;
            if (commentField != null)
            {
                comment = commentField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var sumField = lead!.custom_fields_values?.FirstOrDefault(x => x.field_id == 1545483);
            var sum = string.Empty;
            if (sumField != null)
            {
                sum = sumField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            if (string.IsNullOrEmpty(city) && string.IsNullOrEmpty(date))
            {
                await _telegramBackgroundService.SendInfoMessage($"Не удалось назначить мастера на заявку по причине отсутствия города или даты обработки\n{lead}");
                return;
            }

            var curatorAndMasters = await _ezhkhService.GetKurator(city, date);

            if (curatorAndMasters == null)
            {
                await _telegramBackgroundService.SendInfoMessage($"Не удалось назначить мастера на заявку\n{lead}");
                return;
            }

            var leadJson = JsonSerializer.Serialize(lead, _jsonOptions);

            var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                [InlineKeyboardButton.WithCallbackData("Назначить мастера", $"Наз%{lead.id}%{city}%{date}")],
                //[InlineKeyboardButton.WithCallbackData("Отложить назначение", lead.ToString())],
                //[InlineKeyboardButton.WithCallbackData("Автоназначение", lead.ToString())]
            });

            await _telegramBackgroundService.SendMessage(long.Parse(curatorAndMasters.TelegramID), $"Новая Заявка!\n\n{lead}", replyKeyboardMarkup);

            var dealProxy = new DealProxy
            {
                DealId = lead.id.ToString(),
                KuratorTelegramID = curatorAndMasters.TelegramID,
                Budget = sum,
                DealDateTime = date,
                DealDetails = lead.ToString(),
            };

            var success = await AssignMaster(dealProxy);

            if (!success)
            {
                await _telegramBackgroundService.SendInfoMessage($"Не удалось создать первоначальную заявку!\n\n{lead}");
            }
        }

        public async Task<bool> AssignMaster(DealProxy deal, Employee? master = null)
        {
            if (master != null)
            {
                var amoService = ServiceLocator.Instance?.GetRequiredService<IAmoCrmLeadService>();

                deal.MasterTelegramID = master.TelegramID;

                var ezhkhResult = await _ezhkhService.CreateDeal(deal);
                var amoResult = await amoService.ChangeLead(deal.DealId, master.CrmName);

                return ezhkhResult && amoResult;
            }
            else
            {
                return await _ezhkhService.CreateDeal(deal);
            }
        }

        public async Task NotifyMaster(NotifyProxy notifyProxy)
        {
            await _telegramBackgroundService.SendMessage(long.Parse(notifyProxy.masterId), $"Новая Заявка!\n\n{notifyProxy.lead}");
        }

        private ReplyKeyboardMarkup GetKeyboardByEmployeeType(EmployeeType type)
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup
            {
                ResizeKeyboard = true
            };

            switch (type)
            {
                case EmployeeType.Admin:
                    {
                        break;
                    }
                case EmployeeType.Curator:
                    {
                        replyKeyboardMarkup.AddButton("Мои сделки");
                        break;
                    }
                case EmployeeType.ExternalCurator:
                    {
                        goto case EmployeeType.Curator;
                    }
                case EmployeeType.Master:
                    {
                        replyKeyboardMarkup.AddNewRow("Мои сделки");
                        replyKeyboardMarkup.AddNewRow("Получить отчет за день");
                        break;
                    }
                case EmployeeType.Operator:
                    {
                        break;
                    }
            }

            return replyKeyboardMarkup;
        }
    }
}
