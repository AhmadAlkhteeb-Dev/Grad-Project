using System.ComponentModel.DataAnnotations;

namespace SmartHR_Platform.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال المعرّف الوظيفي أو البريد الإلكتروني")]
        [Display(Name = "المعرّف الوظيفي أو البريد")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "يرجى إدخال كلمة المرور")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
    }

    public class AuthenticatedUserSession
    {
        public string Identifier { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
        public string Department { get; set; } = "تطوير البرمجيات";
    }
}