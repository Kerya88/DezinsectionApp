using GJIService;

namespace DezinsectionApp.Services.Ezhkh
{
    public interface IEzhkhService
    {
        public Task<int> GetResponsibleUserIdAsync();
        public Task<BotUserProxy[]?> GetRegistredEmployees();
        public Task<CrmCityProxy[]?> GetCrmCity();
        public Task<SESKuratorProxy?> GetKurator(string city, string leadDate);
        public Task<bool> RegisterNewEmployee(SESEmployerProxy employe);
        public Task<bool> CreateDeal(DealProxy deal);
        public Task<DealProxy[]?> GetMyDeals(string employeeId, bool? isReassig = null);
    }
}
