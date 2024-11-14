using GJIService;

namespace DezinsectionApp.Services.Ezhkh
{
    public interface IEzhkhService
    {
        public Task<int> GetResponsibleUserIdAsync();
        public Task<BotUserProxy[]?> GetRegistredEmployees();
        public Task<CrmCityProxy[]?> GetCrmCity();
        public Task<bool> RegisterNewEmployee(SESEmployerProxy employe);
    }
}
