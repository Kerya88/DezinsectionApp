using DezinsectionApp.Services.Ezhkh;

namespace DezinsectionApp.Entities
{
    public class AmoLead
    {
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
                    values = [ new() { value = requestProxy.RoomsNoData == "Да" ? $"Примерно {requestProxy.ApproximateRoomsCount}" : requestProxy.NumberApartments } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077519, //площадь
                    values = [ new() { value = requestProxy.AreaNoData == "Да" ? $"Примерно {requestProxy.ApproximateArea}" : requestProxy.Area } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077295, //регион
                    values = [ new() { value = requestProxy.TestCountry } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077293, //город
                    values = [ new() { value = requestProxy.Page == "Ростов" ? "Ростов на Дону" : requestProxy.Page } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077575, //адрес объекта
                    values = [ new() { value = requestProxy.RoomType } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077683, //источник
                    values = [ new() { value = "Сайт" } ]
                },
                new CustomFieldsValue
                {
                    field_id = 1077577, //комментарий
                    values = [ new() { value = requestProxy.Form switch {
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
                    new()
                    {
                        id = 757565
                    }
                ]
            };

            if (requestProxy.Phone != "0" || requestProxy.TestPhone != "0")
            {
                embeded.contacts =
                [
                    new()
                    {
                        name = requestProxy.Phone != "0" ? requestProxy.Phone : requestProxy.TestPhone,
                        custom_fields_values =
                        [
                            new() {
                                field_id = 983491, //телефон
                                values = [ new() { value = requestProxy.Phone != "0" ? requestProxy.Phone : requestProxy.TestPhone, enum_id = 1235915 } ]
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
        public string? field_code { get; set; }
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
