using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PyroSafe.Pages.Account
{
    public class LogoutModel : PageModel
    {
        [ValidateAntiForgeryToken]
        public IActionResult OnPost()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Account/Register");
        }

    }
}
