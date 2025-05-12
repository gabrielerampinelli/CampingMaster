using CampingMaster.Data;
using CampingMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CampingMaster.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public User CurrentUser { get; set; } // 👈 Add this

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; }

        public class RegisterInputModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string ConfirmPassword { get; set; }
        }


        public async void OnGet()
        {
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Check if the username already exists in the database
                if (await _context.Users.AnyAsync(u => u.Username == Input.Username))
                {
                    ModelState.AddModelError(string.Empty, "Username is already taken.");
                    return Page();
                }

                // Check if the passwords match
                if (Input.Password != Input.ConfirmPassword)
                {
                    ModelState.AddModelError(string.Empty, "Passwords do not match.");
                    return Page();
                }

                // Create and save the new user to the database
                var newUser = new User
                {
                    Username = Input.Username,
                    Password = Input.Password  // Consider hashing the password in a real app
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Redirect to login page after successful registration
                return RedirectToPage("/Login");
            }

            return Page();
        }
    }
}
