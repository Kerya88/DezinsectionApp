using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace DezinsectionApp.Entities
{
    public class CreatiumLead
    {
        public string Target { get; set; }
        public string RoomType { get; set; }
        public string NumberApartments { get; set; }
        public string Area { get; set; }
        public string InsectionTime { get; set; }
        public string ConnectionForm { get; set; }
        public string Phone { get; set; }
        public string TestCountry { get; set; }
        public string TestPhone { get; set; }
        public string RoomsNoData { get; set; }
        public string ApproximateRoomsCount { get; set; }
        public string AreaNoData { get; set; }
        public string ApproximateArea { get; set; }
        public string Page { get; set; }
        public string Form { get; set; }

        public static CreatiumLead ParseToObject(string rawData)
        {
            var proxy = new CreatiumLead();

            var fieldsAndValues = rawData.Split('&');

            foreach (var fieldAndValue in fieldsAndValues)
            {
                var splitFieldAndValue = fieldAndValue.Split('=');

                var field = typeof(CreatiumLead).GetProperty(splitFieldAndValue[0].Trim());

                field?.SetValue(proxy, splitFieldAndValue[1].Trim());
            }

            proxy.Phone = proxy.Phone.Replace(" ", "");
            proxy.TestPhone = proxy.TestPhone.Replace(" ", "");
            if (proxy.Phone.StartsWith('8'))
            {
                proxy.Phone = "+7" + proxy.Phone.Substring(1);
            }
            if (proxy.TestPhone.StartsWith('8'))
            {
                proxy.TestPhone = "+7" + proxy.TestPhone.Substring(1);
            }

            if (proxy.Phone.StartsWith('7'))
            {
                proxy.Phone = "+" + proxy.Phone;
            }
            if (proxy.TestPhone.StartsWith('7'))
            {
                proxy.TestPhone = "+" + proxy.TestPhone;
            }

            if (proxy.ConnectionForm == "0" && proxy.Form != "Квиз")
            {
                proxy.ConnectionForm = "WhatsApp";
            }

            return proxy;
        }

        public static string ParseToJson(string rawData)
        {
            var proxy = ParseToObject(rawData);

            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            };

            return JsonSerializer.Serialize(proxy, options);
        }
    }
}
