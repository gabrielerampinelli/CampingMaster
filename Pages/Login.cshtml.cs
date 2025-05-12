using CampingMaster.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CampingMaster.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

public class LoginModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public LoginModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; }
    public User CurrentUser { get; set; }


    public List<User> Users { get; set; } // Add this property

    public class LoginInputModel
    {
        public string Username { get; set; }
        public string Password { get; set; } = "User";

        public bool RememberMe { get; set; } = false;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Load all users for debugging purposes
        Users = await _context.Users.ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ModelState.IsValid)
        {
            // Query the database for the user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == Input.Username && u.Password == Input.Password);

            if (user != null)
            {
                var claims = new[]
                {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // 🔐 Persistent login logic
               var authProperties = Input.RememberMe
    ? new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(2)
    }
    : new AuthenticationProperties
    {
        IsPersistent = false
    };


                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                return RedirectToPage("/Index");
            }

            else
            {
                // Invalid login attempt
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your username and password.");
            }
        }

        // If we reach here, it means the login attempt was invalid
        // Reload users for debugging purposes
        Users = await _context.Users.ToListAsync();
        return Page();
    }
}
