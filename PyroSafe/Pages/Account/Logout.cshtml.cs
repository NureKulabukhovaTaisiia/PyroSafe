using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            // Очистка сесії
            HttpContext.Session.Clear();

            // Або, якщо використовуєш cookie-аутентифікацію:
            // await HttpContext.SignOutAsync();

            return RedirectToPage("/Account/Login"); // Повернення на сторінку логіну
        }
    }
}
