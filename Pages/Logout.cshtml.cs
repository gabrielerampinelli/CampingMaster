using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace CampingMaster.Pages
{
    public class LogoutModel : PageModel
    {
        // This handler signs out the user and clears the cookie
        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }

        // Optional: allow GET to also log out immediately
        public async Task<IActionResult> OnGetAsync()
        {
            return await OnPostAsync();
        }
    }
}
