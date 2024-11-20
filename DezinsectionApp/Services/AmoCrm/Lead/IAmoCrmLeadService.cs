using DezinsectionApp.Entities;

namespace DezinsectionApp.Services.AmoCrm.Lead
{
    public interface IAmoCrmLeadService
    {
        public Task AcceptLead(string rawLead);
        public Task<AmoLead?> GetLead(string leadId);
        public Task<bool> ChangeLead(string leadId, string masterName);
        public Task<Contact?> GetContact(string contactId);
        public Task AssignMaster(string rawRequest);
        public Task<bool> NotifyMaster(string rawRequest);
    }
}
