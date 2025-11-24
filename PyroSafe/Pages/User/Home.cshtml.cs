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
            Username = _httpContextAccessor.HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(Username))
                return RedirectToPage("/Account/Register"); // <-- редирект на регистрацию после выхода

            return Page();
        }

    }
}
