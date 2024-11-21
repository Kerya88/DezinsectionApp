
using DezinsectionApp.Entities;
using DezinsectionApp.Enums;
using DezinsectionApp.Services.Ezhkh;
using GJIService;

namespace DezinsectionApp.BackgroundServices
{
    public class StorageBackgroundService : BackgroundService
    {
        private readonly IEzhkhService _ezhkhService;
        private readonly IConfiguration _configuration;

        public Dictionary<long, Employee> EmployeeStore { get; set; }
        public bool State { get; set; }
        public CrmCityProxy[] Citys { get; set; }

        public StorageBackgroundService(IEzhkhService ezhkhService, IConfiguration configuration)
        {
            _ezhkhService = ezhkhService;
            _configuration = configuration;
            UpdateState();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                UpdateState();

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        private void UpdateState()
        {
            var foundedEmployees = _ezhkhService.GetRegistredEmployees().Result;
            var citys = _ezhkhService.GetCrmCity().Result;

            if (foundedEmployees != null && citys != null)
            {
                EmployeeStore = new(foundedEmployees.Length);
                foundedEmployees.ToList().ForEach(x =>
                {
                    var successParse = long.TryParse(x.TelegramID, out var tgId);

                    if (successParse)
                    {
                        EmployeeStore.Add(tgId, new Employee
                        {
                            TelegramID = x.TelegramID,
                            Phone = x.Phone,
                            FIO = x.FIO,
                            Code = x.Code,
                            CrmName = x.CrmName,
                            UserActivityStateType = UserActivityStateType.NotSet,
                            EmployeeType = (EmployeeType)int.Parse(x.UserType)
                        });
                    }
                });

                Citys = citys;
                State = bool.Parse(_configuration["State"]!);
            }
            else
            {
                Console.WriteLine("Не удалось получить данные из сервиса");
                throw new Exception("Не удалось получить данные из сервиса");
            }
        }
    }
}
