using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        [ValidateAntiForgeryToken] // захищає від CSRF
        public IActionResult OnPost()
        {
            // Очистити сесію
            HttpContext.Session.Clear();

            // Переадресувати на сторінку реєстрації або логіну
            return RedirectToPage("/Account/Register");
        }
    }
}
