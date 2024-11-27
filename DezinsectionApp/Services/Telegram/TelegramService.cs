using DezinsectionApp.BackgroundServices;
using DezinsectionApp.Entities;
using DezinsectionApp.Enums;
using DezinsectionApp.Extentions;
using DezinsectionApp.Services.AmoCrm.Lead;
using DezinsectionApp.Services.Ezhkh;
using GJIService;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.Metrics;
using System.IO;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DezinsectionApp.Services.Telegram
{
    public class TelegramService(IEzhkhService ezhkhService, StorageBackgroundService storageBackgroundService, TelegramBackgroundService telegramBackgroundService) : ITelegramService
    {
        private static readonly Regex FioRegex = new(@"^[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}\s[А-ЯЁ]{1}[а-яё]{1,}$");
        private static readonly Regex PhoneRegex = new(@"^(\+7|8)9\d{9}$");
        private static readonly Regex SumRegex = new(@"^\d+$");

        public async Task ProcessMessage(Update update, Employee employee)
        {
            if (update is { Type: UpdateType.Message, Message.Type: MessageType.Text } && !string.IsNullOrEmpty(update.Message.Text))
            {
                var message = update.Message;

                switch (message.Text)
                {
                    case "/start":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                await SendRegistrationInfo(message.Chat.Id);
                            }
                            else
                            {
                                var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);

                                await telegramBackgroundService.SendMessage(message.Chat.Id, $"Здравствуйте, {employee.FIO}", replyKeyboardMarkup);
                            }

                            break;
                        }
                    case "Зарегистрироваться":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                await telegramBackgroundService.SendMessage(message.Chat.Id, "Введите Ваше ФИО, каждое слово с большой буквы");

                                employee.UserActivityStateType = UserActivityStateType.FIO;
                            }
                            else
                            {
                                await telegramBackgroundService.SendMessage(message.Chat.Id, "Вы уже зарегистрированы");

                                var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);

                                await telegramBackgroundService.SendMessage(message.Chat.Id, $"Здравствуйте, {employee.FIO}", replyKeyboardMarkup);
                            }

                            break;
                        }
                    case "Получить отчет за день":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                await SendRegistrationInfo(message.Chat.Id);
                            }
                            else
                            {
                                var fileResponce = await ezhkhService.GetWorkerReportFile(message.Chat.Id.ToString());

                                if (fileResponce == null || string.IsNullOrEmpty(fileResponce.FileName) || string.IsNullOrEmpty(fileResponce.File))
                                {
                                    await telegramBackgroundService.SendMessage(message.Chat.Id, "Сервис не доступен, попробуйте позже");
                                    break;
                                }

                                var fileBytes = Convert.FromBase64String(fileResponce.File);

                                using (var ms = new MemoryStream(fileBytes))
                                {
                                    var inputFile = InputFile.FromStream(ms, fileResponce.FileName);

                                    await telegramBackgroundService.SendDocument(message.Chat.Id, inputFile);
                                }
                            }
                            break;
                        }
                    case "Мои сделки":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                await SendRegistrationInfo(message.Chat.Id);
                            }
                            else
                            {
                                await ShowMyLeads(employee);
                            }
                            break;
                        }
                    case "Проверить свой статус":
                        {
                            if (string.IsNullOrEmpty(employee.TelegramID))
                            {
                                await SendRegistrationInfo(message.Chat.Id);
                            }
                            else
                            {
                                if (employee.EmployeeType != EmployeeType.NotSet)
                                {
                                    var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);
                                    await telegramBackgroundService.SendMessage(message.Chat.Id, $"Теперь вам доступны действия сотрудника типа \"{employee.EmployeeType.GetDisplayName()}\"", replyKeyboardMarkup);
                                }
                                else
                                {
                                    await telegramBackgroundService.SendMessage(message.Chat.Id, "Дождитесь, пока одобрят запрошенный тип сотрудника");
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

                                        if (FioRegex.IsMatch(fio))
                                        {
                                            employee.FIO = fio;
                                            employee.UserActivityStateType = UserActivityStateType.Phone;
                                            await telegramBackgroundService.SendMessage(message.Chat.Id, "Введите Ваше номер телефона в формате +79123456789");
                                        }
                                        else
                                        {
                                            await telegramBackgroundService.SendMessage(message.Chat.Id, "Введенное ФИО не соответствует формату");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.Phone:
                                    {
                                        var phone = message.Text.Trim();

                                        if (PhoneRegex.IsMatch(phone))
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

                                            await telegramBackgroundService.SendMessage(message.Chat.Id, "Выберите тип сотрудника", replyKeyboardMarkup);
                                        }
                                        else
                                        {
                                            await telegramBackgroundService.SendMessage(message.Chat.Id, "Введенный номер телефона не соответствует формату");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.Sum:
                                    {
                                        var sum = message.Text.Trim();

                                        if (SumRegex.IsMatch(sum))
                                        {
                                            StorageBackgroundService.LeadStore[message.Chat.Id].Budget = sum;

                                            employee.UserActivityStateType = UserActivityStateType.Report;

                                            await telegramBackgroundService.SendMessage(message.Chat.Id, "Отправьте фотографию договора");
                                        }
                                        else
                                        {
                                            await telegramBackgroundService.SendMessage(message.Chat.Id, "Введите число");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.NotSet:
                                    {
                                        if (string.IsNullOrEmpty(employee.TelegramID))
                                        {
                                            await SendRegistrationInfo(message.Chat.Id);
                                        }
                                        else
                                        {
                                            var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);
                                            await telegramBackgroundService.SendMessage(message.Chat.Id, "Вы не выбрали команду", replyKeyboardMarkup);
                                        }
                                        break;
                                    }
                            }

                            break;
                        }
                }
            }
            else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: { Data: not null, Message: not null } })
            {
                switch (update.CallbackQuery.Data.Split("%")[0])
                {
                    case "Наз":
                        {
                            var city = update.CallbackQuery.Data.Split("%")[2];
                            var date = update.CallbackQuery.Data.Split("%")[3];

                            var curatorAndMasters = await ezhkhService.GetKurator(city, date);

                            if (curatorAndMasters == null)
                            {
                                await telegramBackgroundService.SendInfoMessage($"Сервис недоступен, не удалось назначить мастера на заявку id = \n{update.CallbackQuery.Data.Split("%")[1]}");
                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
                                break;
                            }

                            var masters = curatorAndMasters.SESMasterProxyes.ToList();

                            var buttons = new InlineKeyboardButton[masters.Count][];
                            for (int i = 0; i < masters.Count; i++)
                            {
                                buttons[i] = [InlineKeyboardButton.WithCallbackData(masters[i].MasterData, update.CallbackQuery.Data.Replace("Наз%", "") + "%" + masters[i].TelegramID)];
                            }

                            employee.UserActivityStateType = UserActivityStateType.AssignMaster;

                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Доступные мастера", new InlineKeyboardMarkup(buttons));

                            break;
                        }
                    case "По которым назначен мастер":
                        {
                            var deals = await ezhkhService.GetMyDeals(employee.TelegramID, true);

                            if (deals == null)
                            {
                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
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
                                        [InlineKeyboardButton.WithCallbackData("Изменить мастера", $"Наз%{deal.DealId}%{city}%{date}")]
                                    });

                                    await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Мастер: {deal.MasterData}\n{deal.DealDetails}", replyKeyboardMarkup);
                                }
                            }
                            else
                            {
                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "У вас нет сделок");
                            }

                            break;
                        }
                    case "По которым НЕ назначен мастер":
                        {
                            var deals = await ezhkhService.GetMyDeals(employee.TelegramID, false);

                            if (deals == null)
                            {
                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
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
                                    });

                                    await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, deal.DealDetails, replyKeyboardMarkup);
                                }
                            }
                            else
                            {
                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "По всем сделкам мастер назначен");
                            }

                            break;
                        }
                    case "Отч":
                        {
                            var dealId = update.CallbackQuery.Data.Split("%")[1];

                            if (!StorageBackgroundService.LeadStore.TryAdd(update.CallbackQuery.Message.Chat.Id, new DealProxy { DealId = dealId }))
                            {
                                StorageBackgroundService.LeadStore.Remove(update.CallbackQuery.Message.Chat.Id);
                                StorageBackgroundService.LeadStore.Add(update.CallbackQuery.Message.Chat.Id, new DealProxy { DealId = dealId });
                            }

                            employee.UserActivityStateType = UserActivityStateType.Sum;

                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите сумму сделки");

                            break;
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

                                            var citys = storageBackgroundService.Citys;
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

                                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);
                                        }
                                        else
                                        {
                                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введенный тип сотрудника не существует");
                                        }

                                        break;
                                    }
                                case UserActivityStateType.City:
                                    {
                                        if (update.CallbackQuery.Data == "Завершить выбор")
                                        {
                                            if (string.IsNullOrEmpty(employee.City))
                                            {
                                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Необходимо выбрать хотя бы один город");

                                                var citys = storageBackgroundService.Citys;
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

                                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);

                                                break;
                                            }

                                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Вы выбрали следующие города: {employee.City}");

                                            employee.TelegramID = update.CallbackQuery.From.Id.ToString();

                                            var ezhkhEmployee = new SESEmployerProxy
                                            {
                                                TelegramID = employee.TelegramID,
                                                FIO = employee.FIO,
                                                Phone = employee.Phone,
                                                Cities = employee.City,
                                                EmplType = ((int)employee.EmployeeType).ToString()
                                            };

                                            employee.EmployeeType = EmployeeType.NotSet;

                                            var registrSuccess = await ezhkhService.RegisterNewEmployee(ezhkhEmployee);

                                            if (registrSuccess)
                                            {
                                                employee.UserActivityStateType = UserActivityStateType.NotSet;

                                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Вы успешно зарегистрировались");

                                                var replyKeyboardMarkup = GetKeyboardByEmployeeType(employee.EmployeeType);
                                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "После проверки запрошенного вами типа сотрудника вам станет доступно меню внизу экрана", replyKeyboardMarkup);

                                                
                                                //await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Здравствуйте, {employee.FIO}", replyKeyboardMarkup);
                                            }
                                            else
                                            {
                                                employee.Clear();

                                                await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "При регистрации произошла ошибка, попробуйте позже");
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

                                            var citys = storageBackgroundService.Citys.Select(x => x.Name).Select(x =>
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

                                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Введите города, для окончания выбора нажмите кнопку \"Завершить\"", replyKeyboardMarkup);
                                        }

                                        break;
                                    }
                                case UserActivityStateType.AssignMaster:
                                    {
                                        var masterTgId = update.CallbackQuery.Data.Split("%")[3];
                                        var leadId = update.CallbackQuery.Data.Split("%")[0];
                                        var master = StorageBackgroundService.EmployeeStore[long.Parse(masterTgId)];

                                        var leads = await ezhkhService.GetMyDeals(update.CallbackQuery.Message.Chat.Id.ToString());

                                        if (leads == null)
                                        {
                                            await telegramBackgroundService.SendInfoMessage($"Сервис недоступен, не удалось назначить мастера на заявку с id = {leadId}");
                                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Сервис недоступен");
                                            employee.UserActivityStateType = UserActivityStateType.NotSet;
                                            break;
                                        }

                                        var lead = leads.ToList().FirstOrDefault(x => x.DealId == leadId);

                                        employee.UserActivityStateType = UserActivityStateType.NotSet;

                                        var success = await AssignMaster(lead!, master);

                                        if (!success)
                                        {
                                            await telegramBackgroundService.SendInfoMessage($"Не удалось назначить мастера или смена статуса!\n\n{lead}");
                                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, "Не удалось назначить мастера");
                                        }
                                        else
                                        {
                                            await telegramBackgroundService.SendMessage(update.CallbackQuery.Message.Chat.Id, $"Назначен мастер {master.FIO}");
                                        }

                                        break;
                                    }
                            }
                            break;
                        }
                }
            }
            else if (update is { Type: UpdateType.Message, Message.Type: MessageType.Photo } && !update.Message.Photo.IsNullOrEmpty() && employee.UserActivityStateType == UserActivityStateType.Report)
            {
                var message = update.Message;

                var largestPhoto = update.Message.Photo!.Last();

                var file = await telegramBackgroundService.GetFile(largestPhoto.FileId);

                var ezhkh = await ezhkhService.UpdateDeal(StorageBackgroundService.LeadStore[message.Chat.Id]);
                
                if (!ezhkh)
                {
                    await telegramBackgroundService.SendMessage(message.Chat.Id, "Не удалось отправить отчет, повторте попытку позже");
                    return;
                }

                StorageBackgroundService.LeadStore[message.Chat.Id].MasterData = StorageBackgroundService.EmployeeStore[message.Chat.Id].CrmName;

                var amoService = ServiceLocator.Instance.GetRequiredService<IAmoCrmLeadService>();

                var fileProxy = await amoService.SendReport(StorageBackgroundService.LeadStore[message.Chat.Id], file);

                if (fileProxy == null)
                {
                    StorageBackgroundService.LeadStore.Remove(message.Chat.Id);
                    await telegramBackgroundService.SendMessage(message.Chat.Id, "Не удалось отправить отчет, повторте попытку позже");
                    return;
                }

                var amo = await amoService.UpdateReport(StorageBackgroundService.LeadStore[message.Chat.Id], fileProxy);

                if (!amo)
                {
                    StorageBackgroundService.LeadStore.Remove(message.Chat.Id);
                    await telegramBackgroundService.SendMessage(message.Chat.Id, "Не удалось отправить отчет, повторте попытку позже");
                    return;
                }

                StorageBackgroundService.LeadStore.Remove(message.Chat.Id);
                await telegramBackgroundService.SendMessage(message.Chat.Id, "Отчет отправлен");
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
                var unixDate = dateField.values.Select(x => int.Parse(x.value.ToString()!)).FirstOrDefault();
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var dateTime = epoch.AddSeconds(unixDate);
                var localDateTime = dateTime.ToLocalTime();
                date = localDateTime.ToString("g");
            }
            
            var sumField = lead!.custom_fields_values?.FirstOrDefault(x => x.field_id == 1545483);
            var sum = string.Empty;
            if (sumField != null)
            {
                sum = sumField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            if (string.IsNullOrEmpty(city) && string.IsNullOrEmpty(date))
            {
                await telegramBackgroundService.SendInfoMessage($"Не удалось назначить мастера на заявку по причине отсутствия города или даты обработки\n{lead}");
                return;
            }

            var curatorAndMasters = await ezhkhService.GetKurator(city!, date);

            if (curatorAndMasters == null)
            {
                await telegramBackgroundService.SendInfoMessage($"Не удалось назначить мастера на заявку\n{lead}");
                return;
            }

            var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
            {
                [InlineKeyboardButton.WithCallbackData("Назначить мастера", $"Наз%{lead.id}%{city}%{date}")],
                //[InlineKeyboardButton.WithCallbackData("Отложить назначение", lead.ToString())],
                //[InlineKeyboardButton.WithCallbackData("Автоназначение", lead.ToString())]
            });

            await telegramBackgroundService.SendMessage(long.Parse(curatorAndMasters.TelegramID), $"Новая Заявка!\n\n{lead}", replyKeyboardMarkup);

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
                await telegramBackgroundService.SendInfoMessage($"Не удалось создать первоначальную заявку!\n\n{lead}");
            }
        }

        private async Task<bool> AssignMaster(DealProxy deal, Employee? master = null)
        {
            if (master != null)
            {
                var amoService = ServiceLocator.Instance.GetRequiredService<IAmoCrmLeadService>();

                deal.MasterTelegramID = master.TelegramID;

                var ezhkhResult = await ezhkhService.CreateDeal(deal);
                var amoResult = await amoService!.AssignOrReplaceMaster(deal.DealId, master.CrmName);

                return ezhkhResult && amoResult;
            }
            else
            {
                return await ezhkhService.CreateDeal(deal);
            }
        }

        public async Task NotifyMaster(NotifyProxy notifyProxy)
        {
            await telegramBackgroundService.SendMessage(long.Parse(notifyProxy.masterId), $"Новая Заявка!\n\n{notifyProxy.lead}");
        }

        private async Task ShowMyLeads(Employee employee)
        {
            switch (employee.EmployeeType)
            {
                case EmployeeType.Curator:
                    {
                        var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                        {
                            ["По которым назначен мастер"],
                            ["По которым НЕ назначен мастер"]
                        });

                        await telegramBackgroundService.SendMessage(employee.TelegramID, "Какие сделки вас интересуют?", replyKeyboardMarkup);

                        break;
                    }
                case EmployeeType.ExternalCurator:
                    {
                        goto case EmployeeType.Curator;
                    }
                case EmployeeType.Master:
                    {
                        var deals = await ezhkhService.GetMyDeals(employee.TelegramID);

                        if (deals == null)
                        {
                            await telegramBackgroundService.SendMessage(employee.TelegramID, "Сервис недоступен");
                            break;
                        }

                        if (deals.Length > 0)
                        {
                            foreach (var deal in deals)
                            {
                                var replyKeyboardMarkup = new InlineKeyboardMarkup(new InlineKeyboardButton[][]
                                {
                                    [InlineKeyboardButton.WithCallbackData("Отправить отчет", $"Отч%{deal.DealId}")]
                                });

                                var stringDeal = string.Join("\n", $"Сумма: {deal.Budget}", $"Дата и время визита: {deal.DealDateTime}", $"Комментарий: {deal.DealDetails}");

                                await telegramBackgroundService.SendMessage(employee.TelegramID, stringDeal, replyKeyboardMarkup);
                            }
                        }
                        else
                        {
                            await telegramBackgroundService.SendMessage(employee.TelegramID, "У вас нет сделок");
                        }

                        break;
                    }
            }
        }

        private async Task SendRegistrationInfo(long employeeId)
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                ["Зарегистрироваться"]
            })
            {
                ResizeKeyboard = true
            };

            await telegramBackgroundService.SendMessage(employeeId, "Вам необходимо зарегистрироваться в системе, для этого нажмите кнопку \"Зарегистрироваться\" в нижней части окна telegram и ответьте на несколько вопросов", replyKeyboardMarkup);
        }

        private static ReplyKeyboardMarkup? GetKeyboardByEmployeeType(EmployeeType type)
        {
            var replyKeyboardMarkup = new ReplyKeyboardMarkup
            {
                ResizeKeyboard = true
            };

            switch (type)
            {
                case EmployeeType.NotSet:
                    {
                        replyKeyboardMarkup.AddButton("Проверить свой статус");
                        break;
                    }
                case EmployeeType.Admin:
                    {
                        replyKeyboardMarkup = null;
                        break;
                    }
                case EmployeeType.Curator:
                    {
                        replyKeyboardMarkup.AddButton("Мои сделки");
                        replyKeyboardMarkup.AddNewRow("Получить отчет за день");
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
                        replyKeyboardMarkup = null;
                        break;
                    }
            }

            return replyKeyboardMarkup;
        }
    }
}
