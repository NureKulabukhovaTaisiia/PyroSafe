using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost()
        {
            // очистити сесію
            HttpContext.Session.Clear();

            // редірект назад на логін
            return RedirectToPage("/Account/Login");
        }
    }
}
