using DezinsectionApp.Services.Ezhkh;
using System.Text;

namespace DezinsectionApp.Entities
{
    public class AmoLead
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public int? price { get; set; }
        public int? status_id { get; set; }
        public int? pipeline_id { get; set; }
        public int? created_by { get; set; }
        public int? updated_by { get; set; }
        public int? closed_at { get; set; }
        public int? created_at { get; set; }
        public int? updated_at { get; set; }
        public int? loss_reason_id { get; set; }
        public int? responsible_user_id { get; set; }
        public Embedded? _embedded { get; set; }
        public CustomFieldsValue[]? custom_fields_values { get; set; }

        public static AmoLead[] ParseRequestToAmo(CreatiumLead requestProxy)
        {
            var ezhkhService = ServiceLocator.Instance?.GetRequiredService<IEzhkhService>();

            var customFields = new List<CustomFieldsValue>()
            {
                new CustomFieldsValue
                {
                    field_id = 1077467, //проблема
                    values = requestProxy.Target
                        .Split(",")
                        .Select(x => new Value
                        {
                            value = x.Trim() == "крысы" ? "Крысы" : x.Trim()
                        })
                        .ToArray()
                },
                //new CustomFieldsValue
                //{
                //    field_id = 1077467, //проблема
                //    values = [ new() { value = requestProxy.Target.Split(",")[0] } ]
                //},
                new CustomFieldsValue
                {
                    field_id = 1077517, //кол-во комнат
                    values = [ new Value { value = requestProxy.RoomsNoData == "Да" ? $"Примерно {requestProxy.ApproximateRoomsCount}" : requestProxy.NumberApartments } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077519, //площадь
                    values = [ new Value { value = requestProxy.AreaNoData == "Да" ? $"Примерно {requestProxy.ApproximateArea}" : requestProxy.Area } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077295, //регион
                    values = [ new Value { value = requestProxy.TestCountry } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077293, //город
                    values = [ new Value { value = requestProxy.Page == "Ростов" ? "Ростов на Дону" : requestProxy.Page } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077575, //адрес объекта
                    values = [ new Value { value = requestProxy.RoomType } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077683, //источник
                    values = [ new Value { value = "Сайт" } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077577, //комментарий
                    values = [ new Value { value = requestProxy.Form switch {
                        "Квиз" => $"Способ связи: {requestProxy.ConnectionForm}, период обработки: {requestProxy.InsectionTime}",
                        "Прайс" => $"Способ связи: {requestProxy.ConnectionForm}, период обработки: {requestProxy.InsectionTime}",
                        _ => ""
                    }}]
                }
            };

            customFields.RemoveAll(x => x.values.Any(y => y.value.ToString() == "0"));

            var embeded = new Embedded()
            {
                tags =
                [
                    new Tag
                    {
                        id = 757565
                    }
                ]
            };

            if (requestProxy.Phone != "0" || requestProxy.TestPhone != "0")
            {
                embeded.contacts =
                [
                    new Contact
                    {
                        name = requestProxy.Phone != "0" ? requestProxy.Phone : requestProxy.TestPhone,
                        custom_fields_values =
                        [
                            new CustomFieldsValue {
                                field_id = 983491, //телефон
                                values = [ new Value { value = requestProxy.Phone != "0" ? requestProxy.Phone : requestProxy.TestPhone, enum_id = 1235915 } ]
                            }
                        ]
                    }
                ];
            }

            var amoProxy = new AmoLead
            {
                created_by = 0,
                created_at = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                custom_fields_values = customFields.ToArray(),
                _embedded = embeded
            };

            var respnibleUserId = ezhkhService!.GetResponsibleUserIdAsync().Result;

            if (respnibleUserId > 0)
            {
                amoProxy.responsible_user_id = respnibleUserId;
            }

            amoProxy.name = requestProxy.Form switch
            {
                "Квиз" => $"Заявка с dezregioses.ru т. {requestProxy.Phone}",
                "Прайс" => $"Запрос прайса т. {requestProxy.TestPhone}",
                "Вызов мастера (категории)" => $"Вызов мастера т. {requestProxy.TestPhone}",
                _ => ""
            };

            //amoProxy.name = $"Тест контроля дублей";

            return [amoProxy];
        }

        public override string ToString()
        {
            var contact = _embedded?.contacts?.FirstOrDefault(x => x.custom_fields_values != null
                    && x.custom_fields_values.Any(x => x.field_id == 983491 && x.values.Any(x => !string.IsNullOrEmpty(x.value.ToString()))));

            var name = contact?.name ?? string.Empty;
            var phone = contact?.custom_fields_values?.FirstOrDefault(x => x.field_id == 983491)?.values.FirstOrDefault()?.value ?? string.Empty;

            var problem = string.Join(", ", custom_fields_values?.FirstOrDefault(x => x.field_id == 1077467)?.values.Select(x => x.value.ToString() ?? string.Empty) ?? Array.Empty<string>());

            var dateField = custom_fields_values?.FirstOrDefault(x => x.field_id == 1077571);
            var date = string.Empty;
            if (dateField != null)
            {
                var unixDate = dateField.values.Select(x => int.Parse(x.value.ToString())).FirstOrDefault();
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var dateTime = epoch.AddSeconds(unixDate);
                var localDateTime = dateTime.ToLocalTime();
                date = localDateTime.ToString("g");
            }

            var cityField = custom_fields_values?.FirstOrDefault(x => x.field_id == 1077293);
            var city = string.Empty;
            if (cityField != null)
            {
                city = cityField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var addressField = custom_fields_values?.FirstOrDefault(x => x.field_id == 1077575);
            var address = string.Empty;
            if (addressField != null)
            {
                address = addressField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var commentField = custom_fields_values?.FirstOrDefault(x => x.field_id == 1077577);
            var comment = string.Empty;
            if (commentField != null)
            {
                comment = commentField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var sumField = custom_fields_values?.FirstOrDefault(x => x.field_id == 1545483);
            var sum = string.Empty;
            if (sumField != null)
            {
                sum = sumField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var roomsCountField = custom_fields_values?.FirstOrDefault(x => x.field_id == 1077517);
            var roomsCount = string.Empty;
            if (roomsCountField != null)
            {
                roomsCount = roomsCountField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var areaField = custom_fields_values?.FirstOrDefault(x => x.field_id == 1077519);
            var area = string.Empty;
            if (areaField != null)
            {
                area = areaField.values.Select(x => x.value.ToString()).FirstOrDefault();
            }

            var sb = new StringBuilder();
            sb.Append($"Имя клиента: {name}\n");
            sb.Append($"Телефон: {phone}\n");
            sb.Append($"Стартовая сумма: {sum}\n");
            sb.Append($"Дата и время визита: {date}\n");
            sb.Append($"Город: {city}\n");
            sb.Append($"Адрес объекта: {address}\n");
            sb.Append($"Проблема: {problem}\n");
            sb.Append($"Кол-во комнат: {roomsCount}\n");
            sb.Append($"Площадь: {area}\n");
            sb.Append($"Комментарий: {comment}\n");

            return sb.ToString();
        }
    }

    public class Embedded
    {
        public Tag[]? tags { get; set; }
        public Contact[]? contacts { get; set; }
        public Company[]? companies { get; set; }
    }

    public class Tag
    {
        public int? id { get; set; }
    }

    public class Contact
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public string? first_name { get; set; }
        public string? last_name { get; set; }
        public int? responsible_user_id { get; set; }
        public int? group_id { get; set; }
        public int? created_by { get; set; }
        public int? updated_by { get; set; }
        public int? created_at { get; set; }
        public int? updated_at { get; set; }
        public int? closest_task_at { get; set; }
        public bool? is_deleted { get; set; }
        public bool? is_unsorted { get; set; }
        public int? account_id { get; set; }
        public Embedded? _embedded { get; set; }
        public CustomFieldsValue[]? custom_fields_values { get; set; }
    }

    public class Company
    {
        public string? name { get; set; }
        public int? responsible_user_id { get; set; }
        public int? created_by { get; set; }
        public int? updated_by { get; set; }
        public int? created_at { get; set; }
        public int? updated_at { get; set; }
        public CustomFieldsValue[]? custom_fields_values { get; set; }
    }

    public class CustomFieldsValue
    {
        public int field_id { get; set; }
        public string? field_name { get; set; }
        public string? field_code { get; set; }
        public string? field_type { get; set; }
        public required Value[] values { get; set; }
    }

    public class Value
    {
        public required object value { get; set; }
        public int? enum_id { get; set; }
    }

    public class ContactsRequestProxy
    {
        public int? _page { get; set; }
        public Links? _links { get; set; }
        public Embedded? _embedded { get; set; }

        public class Links
        {
            public Self? self { get; set; }
            public Next? next { get; set; }
            public First? first { get; set; }
            public Prev? prev { get; set; }

            public class Self
            {
                public string? href { get; set; }
            }

            public class Next
            {
                public string? href { get; set; }
            }

            public class First
            {
                public string? href { get; set; }
            }

            public class Prev
            {
                public string? href { get; set; }
            }
        }
    }
}
