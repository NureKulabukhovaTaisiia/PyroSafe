using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            HttpContext.Session.Clear(); // чистим сессию
            Response.Cookies.Delete(".AspNetCore.Session"); // удаляем cookie сессии
            return RedirectToPage("/Account/Register"); // редирект на регистрацию
        }
    }
}
