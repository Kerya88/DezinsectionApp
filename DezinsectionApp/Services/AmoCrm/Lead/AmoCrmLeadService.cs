using DezinsectionApp.BackgroundServices;
using DezinsectionApp.Entities;
using DezinsectionApp.Services.Telegram;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace DezinsectionApp.Services.AmoCrm.Lead
{
    public class AmoCrmLeadService(TelegramBackgroundService telegramBackgroundService, ITelegramService telegramService) : IAmoCrmLeadService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        private static readonly string PostLeadsComplexEndpoint = "https://artliapin.amocrm.ru/api/v4/leads/complex";
        private static readonly string GetLeadEndpoint = "https://artliapin.amocrm.ru/api/v4/leads";
        private static readonly string GetContactEndpoint = "https://artliapin.amocrm.ru/api/v4/contacts/";
        private static readonly string AmoToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImp0aSI6ImU2NzczNWZiYjViMzVhNjU3MmZkMDcyOGFjYjI2NDY3YWJhZjc0MTVhNGFkMDAzNDRhMTQwMzJkMWYwM2ZmY2YwZjY3NTdmODhiMWQ4NjVhIn0.eyJhdWQiOiI0ZDc0Y" +
            "Tk3YS0wZDdhLTRmZWMtOGQ5My0xNjU3MzU2OGE5NmUiLCJqdGkiOiJlNjc3MzVmYmI1YjM1YTY1NzJmZDA3MjhhY2IyNjQ2N2FiYWY3NDE1YTRhZDAwMzQ0YTE0MDMyZDFmMDNmZmNmMGY2NzU3Zjg4YjFkODY1YSIsImlhdCI6MTcyNzg2ODM0MSwibmJmIjoxNzI3O" +
            "DY4MzQxLCJleHAiOjE3NTk0NDk2MDAsInN1YiI6IjExNDMxMTk0IiwiZ3JhbnRfdHlwZSI6IiIsImFjY291bnRfaWQiOjMxMTUzMzEwLCJiYXNlX2RvbWFpbiI6ImFtb2NybS5ydSIsInZlcnNpb24iOjIsInNjb3BlcyI6WyJjcm0iLCJmaWxlcyIsImZpbGVzX2Rlb" +
            "GV0ZSIsIm5vdGlmaWNhdGlvbnMiLCJwdXNoX25vdGlmaWNhdGlvbnMiXSwiaGFzaF91dWlkIjoiMjFmNzUzNTEtMTEzNy00N2YwLWFkYzItNTY3ZDllMTZmMjk5IiwiYXBpX2RvbWFpbiI6ImFwaS1iLmFtb2NybS5ydSJ9.KETAvr5Cg4CyZe5vaRKYMtEagpAG5Lhu" +
            "POLwUWpBKdz-3y-BrbridcgTjgQbe4ZJ27l61D7LHG_F395BgUrByz-5zlda48Jw2FzMjeZk5U4d-v5H0sp-x1WiNoxNSVWOIGH3lWg-pydSqyxJLRqdQGCDuBIHOuvzHvFXw4lF138GPXS0cSTPNIR1InXHnU78aw0Fhy0VJDDRuAYThAN2YNo4_f1gLL2lC6zrzhBH" +
            "0Kwk5XehpyQv2IYWG8pBKhG9hgEc0DvfZ2fdjCFPbA_cuooPXUygktMXLYOKb3riKX8Pt1etuQ-Q6yCAXbNFKUFUIzq8aaDrUy-OGxlmRiqiow";

        public async Task AcceptLead(string rawLead)
        {
            rawLead = Uri.UnescapeDataString(rawLead).Replace("+", " ");

            var infoTask = telegramBackgroundService.SendInfoMessage(rawLead);

            var creatiumLead = CreatiumLead.ParseToObject(rawLead);
            var amoLead = AmoLead.ParseRequestToAmo(creatiumLead);

            HttpResponseMessage response;

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AmoToken);

                response = await client.PostAsJsonAsync(PostLeadsComplexEndpoint, amoLead, JsonOptions);
            }

            await infoTask;
            if (response.IsSuccessStatusCode)
            {
                await telegramBackgroundService.SendInfoMessage($"Успешно обработано:\n{response.Content.ReadAsStringAsync().Result}");
            }
            else
            {
                var sentJson = JsonSerializer.Serialize(amoLead, JsonOptions);

                await telegramBackgroundService.SendInfoMessage($"\nОтправленый JSON:\n{sentJson}\nОшибка:\n{response.Content.ReadAsStringAsync().Result}");
            }
        }

        public async Task AssignMaster(string rawRequest)
        {
            rawRequest = Uri.UnescapeDataString(rawRequest);
            await telegramBackgroundService.SendInfoMessage(rawRequest);

            var leadId = rawRequest.Split("&")[0].Split("=")[1];
            var lead = await GetLead(leadId);
            if (lead == null)
            {
                await telegramBackgroundService.SendInfoMessage($"Не удалось запросить и десериализовать заявку\n\b{rawRequest}");
                return;
            }

            var contact = lead._embedded?.contacts?.FirstOrDefault();
            if (contact?.id == null)
            {
                await telegramBackgroundService.SendInfoMessage($"В заявке нет ни одного контакта\n\b{rawRequest}");
                return;
            }

            contact = await GetContact(contact!.id!.ToString()!);
            lead!._embedded!.contacts = [contact!];

            await telegramBackgroundService.SendInfoMessage($"{lead}");

            await telegramService.NotifyCurator(lead);
        }

        public async Task<AmoLead?> GetLead(string leadId)
        {
            HttpResponseMessage response;

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AmoToken);

                response = await client.GetAsync(GetLeadEndpoint + "/" + leadId + "?with=contacts");
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AmoLead>();
            }
            else
            {
                return null;
            }
        }

        public async Task<bool> AssignOrReplaceMaster(string leadId, string masterName)
        {
            var request = $"[{{\"id\": {leadId},\"status_id\": 58596858,\"updated_by\": 0,\"custom_fields_values\": [{{\"field_id\": 1776661,\"values\": [{{\"value\": true}}]}},{{\"field_id\": 1077579,\"values\": [{{\"value\": \"{masterName}\"}}]}}]}}]";

            HttpResponseMessage response;

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AmoToken);

                var content = new StringContent(request, Encoding.UTF8, "application/json");

                response = await client.PatchAsync(GetLeadEndpoint, content);
            }

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Contact?> GetContact(string contactId)
        {
            HttpResponseMessage response;

            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AmoToken);

                response = await client.GetAsync(GetContactEndpoint + contactId);
            }

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Contact>();
            }
            else
            {
                return null;
            }
        }

        public async Task<bool> NotifyMaster(string rawRequest)
        {
            var notifyProxy = JsonSerializer.Deserialize<NotifyProxy>(rawRequest);

            if (notifyProxy == null)
            {
                return false;
            }

            await telegramService.NotifyMaster(notifyProxy);

            if (notifyProxy.sendToAmo)
            {
                var amoSuccess = await AssignOrReplaceMaster(notifyProxy.leadId, notifyProxy.masterCrmName);

                return amoSuccess;
            }

            return true;
        }
    }
}
