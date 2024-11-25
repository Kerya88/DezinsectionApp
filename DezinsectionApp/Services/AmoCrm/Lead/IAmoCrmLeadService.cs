using DezinsectionApp.Entities;
using GJIService;

namespace DezinsectionApp.Services.AmoCrm.Lead
{
    public interface IAmoCrmLeadService
    {
        public Task AcceptLead(string rawLead);
        public Task<AmoLead?> GetLead(string leadId);
        public Task<bool> AssignOrReplaceMaster(string leadId, string masterName);
        public Task<bool> UpdateReport(DealProxy deal, FileResponceProxy file);
        public Task<Contact?> GetContact(string contactId);
        public Task AssignMaster(string rawRequest);
        public Task<bool> NotifyMaster(string rawRequest);
        public Task<FileResponceProxy?> SendReport(DealProxy deal, byte[] file);
    }
}
