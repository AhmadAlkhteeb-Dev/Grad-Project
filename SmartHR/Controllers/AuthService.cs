using SmartHR_Platform.Models;
using System.Collections.Generic;
using System.Linq;

namespace SmartHR_Platform.Controllers
{
    public interface IAuthService
    {
        AuthenticatedUserSession? Authenticate(string identifier, string password);
        IReadOnlyList<AuthenticatedUserSession> GetDemoAccounts();
    }

    public class AuthService : IAuthService
    {
        private static readonly List<AuthenticatedUserSession> _demoUsers = new()
        {
            new AuthenticatedUserSession
            {
                Identifier = "EMP-101",
                DisplayName = "سارة العلي",
                Role = "Employee",
                Department = "تطوير البرمجيات"
            },
            new AuthenticatedUserSession
            {
                Identifier = "MGR-201",
                DisplayName = "م. خالد العمري",
                Role = "Manager",
                Department = "تطوير البرمجيات"
            },
            new AuthenticatedUserSession
            {
                Identifier = "HR-301",
                DisplayName = "الموارد البشرية",
                Role = "HR",
                Department = "الموارد البشرية"
            },
            new AuthenticatedUserSession
            {
                Identifier = "ADM-401",
                DisplayName = "مسؤول النظام",
                Role = "Admin",
                Department = "إدارة النظام"
            }
        };

        public AuthenticatedUserSession? Authenticate(string identifier, string password)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                return null;

            var normalizedId = identifier.Trim();

            // يقبل المعرّف التجريبي أو الاسم أو البريد/الرقم الوظيفي مع كلمة مرور غير فارغة أثناء مرحلة التطوير
            var matched = _demoUsers.FirstOrDefault(u =>
                u.Identifier.Equals(normalizedId, System.StringComparison.OrdinalIgnoreCase) ||
                u.DisplayName.Equals(normalizedId, System.StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                return new AuthenticatedUserSession
                {
                    Identifier = matched.Identifier,
                    DisplayName = matched.DisplayName,
                    Role = matched.Role,
                    Department = matched.Department
                };
            }

            // دخول مرن أثناء التطوير لأي معرّف جديد بتحديد الدور من البادئة أو الافتراضي Employee
            var role = "Employee";
            if (normalizedId.StartsWith("MGR", System.StringComparison.OrdinalIgnoreCase)) role = "Manager";
            else if (normalizedId.StartsWith("HR", System.StringComparison.OrdinalIgnoreCase)) role = "HR";
            else if (normalizedId.StartsWith("ADM", System.StringComparison.OrdinalIgnoreCase)) role = "Admin";

            return new AuthenticatedUserSession
            {
                Identifier = normalizedId,
                DisplayName = normalizedId,
                Role = role,
                Department = role == "HR" ? "الموارد البشرية" : "تطوير البرمجيات"
            };
        }

        public IReadOnlyList<AuthenticatedUserSession> GetDemoAccounts() => _demoUsers;
    }
}