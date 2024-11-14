using DezinsectionApp.Enums;

namespace DezinsectionApp.Entities
{
    public class Employee
    {
        public string TelegramID { get; set; }
        public string Phone { get; set; }
        public string FIO { get; set; }
        public EmployeeType EmployeeType { get; set; }
        public string City { get; set; }
        public string CrmName { get; set; }
        public string Code { get; set; }
        public UserActivityStateType UserActivityStateType { get; set; }

        public void Clear()
        {
            TelegramID = string.Empty;
            Phone = string.Empty;
            FIO = string.Empty;
            City = string.Empty;
            CrmName = string.Empty;
            Code = string.Empty;
            EmployeeType = EmployeeType.NotSet;
            UserActivityStateType = UserActivityStateType.NotSet;
        }
    }
}
