using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            // 1) Очистити дані
            HttpContext.Session.Clear();

            // 2) Видалити session cookie
            Response.Cookies.Delete(".AspNetCore.Session");

            // 3) Видалити custom cookie (раптом у тебе є)
            Response.Cookies.Delete("Username");
            Response.Cookies.Delete("UserId");

            // 4) Заборонити кеш, щоб браузер не повертав назад
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "-1";

            // 5) Редірект на login
            return Redirect("/Account/Login");
        }
    }
}
