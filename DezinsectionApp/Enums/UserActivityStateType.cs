using System.ComponentModel.DataAnnotations;

namespace DezinsectionApp.Enums
{
    public enum UserActivityStateType
    {
        [Display(Name = "Не указано")]
        NotSet = 0,

        [Display(Name = "ФИО")]
        FIO = 1,

        [Display(Name = "Номер телефона")]
        Phone = 2,

        [Display(Name = "Тип")]
        Type = 3,

        [Display(Name = "Город")]
        City = 4,

        [Display(Name = "Отчет")]
        Report = 5
    }
}
