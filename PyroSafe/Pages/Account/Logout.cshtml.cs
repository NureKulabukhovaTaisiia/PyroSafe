using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public async Task OnGetAsync()
        {
            // ќчищаЇмо сес≥ю
            HttpContext.Session.Clear();

            // якщо використовуЇш куки-автентиф≥кац≥ю (рекомендуЇтьс€!) Ч теж виходимо
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // ¬ажливо: очищаЇмо саме сес≥йну куку
            Response.Cookies.Delete(".AspNetCore.Session");
        }
    }
}