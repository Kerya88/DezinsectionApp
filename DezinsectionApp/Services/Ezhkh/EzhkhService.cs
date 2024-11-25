using GJIService;
using System.Security.Cryptography;
using System.Text;

namespace DezinsectionApp.Services.Ezhkh
{
    public class EzhkhService(IUrbanAppealService serviceClient) : IEzhkhService
    {
        private readonly IUrbanAppealService _serviceClient = serviceClient;

        public async Task<int> GetResponsibleUserIdAsync()
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                var responce = await _serviceClient.GetActiveOperatorAsync(token);

                var id = int.Parse(responce.ActiveOperator.Id);

                return id;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task<BotUserProxy[]?> GetRegistredEmployees()
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                var responce = await _serviceClient.GetBotUserAsync(token);

                responce.BotUserProxyes ??= [];

                return responce.BotUserProxyes;
            }
            catch
            {
                return null;
            }
        }

        public async Task<CrmCityProxy[]?> GetCrmCity()
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                var responce = await _serviceClient.GetCrmCityAsync(token);

                responce.CrmCityProxyes ??= [];

                return responce.CrmCityProxyes;
            }
            catch
            {
                return null;
            }
        }

        public async Task<SESKuratorProxy?> GetKurator(string city, string leadDate)
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                var responce = await _serviceClient.GetKuratorAsync(token, city, leadDate);

                responce.SESKuratorProxy ??= new SESKuratorProxy();

                return responce.SESKuratorProxy;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> RegisterNewEmployee(SESEmployerProxy employe)
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                await _serviceClient.RegisterNewEmployerAsync(employe, token);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CreateDeal(DealProxy deal)
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                await _serviceClient.CreateDealAsync(deal, token);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<DealProxy[]?> GetMyDeals(string employeeId, bool? isReassig = null)
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                var responce = await _serviceClient.GetMyDealsAsync(token, employeeId, isReassig != null ? isReassig.Value ? "Да" : "Нет" : "");

                responce.DealProxyes ??= [];

                return responce.DealProxyes;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateDeal(DealProxy deal)
        {
            try
            {
                var token = ComputeHash("huiktozalezet" + DateTime.Now.ToString("dd"));

                var responce = await _serviceClient.UpdateDealAsync(deal, token);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ComputeHash(string hashBase)
        {
            //переводим строку в байт-массим  
            byte[] bytes = Encoding.ASCII.GetBytes(hashBase);

            //вычисляем хеш-представление в байтах  
            byte[] byteHash = MD5.HashData(bytes);

            string hash = string.Empty;

            //формируем одну цельную строку из массива  
            foreach (byte b in byteHash)
            {
                hash += $"{b:x2}";
            }

            return hash;
        }
    }
}
