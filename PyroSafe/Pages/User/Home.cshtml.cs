using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;

namespace PyroSafe.Pages.User
{
    public class HomeModel : PageModel
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomeModel(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string Username { get; private set; }

        public IActionResult OnGet()
        {
            // Беремо ім’я користувача з сесії
            Username = _httpContextAccessor.HttpContext.Session.GetString("Username");

            // Якщо сесія порожня — редірект на логін
            if (string.IsNullOrEmpty(Username))
                return RedirectToPage("/Account/Login");

            return Page();
        }
    }
}
