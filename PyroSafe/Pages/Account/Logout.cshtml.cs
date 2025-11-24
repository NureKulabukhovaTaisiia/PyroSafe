using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            // Очистити серверну частину сесії
            HttpContext.Session.Clear();

            // Видалити cookie сесії
            Response.Cookies.Delete(".PyroSafe.Session");

            // Додатково блокуємо кеш браузера, щоб не повертало назад
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToPage("/Account/Login");
        }
    }
}
