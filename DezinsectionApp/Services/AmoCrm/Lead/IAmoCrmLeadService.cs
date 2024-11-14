using DezinsectionApp.Entities;

namespace DezinsectionApp.Services.AmoCrm.Lead
{
    public interface IAmoCrmLeadService
    {
        public Task AcceptLead(string rawLead);

        public Task<AmoLead?> GetLead(string leadId);
    }
}
