namespace DezinsectionApp.Entities
{
    public class NotifyProxy
    {
        public string leadId { get; set; }
        public string masterId { get; set; }
        public string masterCrmName { get; set; }
        public bool sendToAmo { get; set; }
        public string lead { get; set; }
        public bool report { get; set; }
    }
}
