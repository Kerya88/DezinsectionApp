using System.ComponentModel.DataAnnotations;

namespace DezinsectionApp.Enums
{
    public enum EmployeeType
    {
        [Display(Name = "Новый пользователь")]
        NotSet = 0,

        [Display(Name = "Администратор")]
        Admin = 10,

        [Display(Name = "Куратор")]
        Curator = 15,

        [Display(Name = "Мастер")]
        Master = 30,

        [Display(Name = "Оператор")]
        Operator = 40
    }
}
