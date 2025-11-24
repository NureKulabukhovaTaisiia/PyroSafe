using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            // Очистити сесію
            HttpContext.Session.Clear();

            // Перенаправлення на сторінку входу
            return RedirectToPage("/Account/Login");
        }
    }
}
