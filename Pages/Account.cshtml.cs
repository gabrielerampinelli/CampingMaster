// Pages/Account.cshtml.cs
using System.Drawing;
using System.Security.Claims;
using CampingMaster.Data;
using CampingMaster.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace CampingMaster.Pages
{
    public class AccountModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public AccountModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public User CurrentUser { get; set; }
        public List<Reservation> Reservations { get; set; } = new List<Reservation>();

        public async Task<IActionResult> OnGetAsync()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToPage("/Login");

            CurrentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (CurrentUser == null)
                return RedirectToPage("/Login");

            Reservations = await _db.Reservations
                .Include(r => r.Camping)
                .Where(r => r.UserId == CurrentUser.Id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentUser.Password))
            {
                ModelState.Remove("CurrentUser.Password");
            }
            if (string.IsNullOrWhiteSpace(CurrentUser.Email))
            {
                ModelState.Remove("CurrentUser.Email");
            }

            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState)
                {
                    foreach (var error in modelState.Value.Errors)
                    {
                        Console.WriteLine($"Key: {modelState.Key}, Error: {error.ErrorMessage}");
                    }
                }
                return Page();
            }


            var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == CurrentUser.Id);
            if (dbUser == null)
                return NotFound();

            dbUser.Username = CurrentUser.Username;
            dbUser.Email = CurrentUser.Email;
            if(!string.IsNullOrWhiteSpace(CurrentUser.Password))
            {
                dbUser.Password = CurrentUser.Password;
            }

            await _db.SaveChangesAsync();

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, dbUser.Username),
    new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
    new Claim(ClaimTypes.Role, dbUser.Role)
};

            var identity = new ClaimsIdentity(claims, "login");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(principal);

            TempData["Success"] = "Account updated successfully!";
            return RedirectToPage("/Index");
        }
        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var reservation = await _db.Reservations.FindAsync(id);
            if (reservation == null)
                return NotFound();

            _db.Reservations.Remove(reservation);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Reservation deleted successfully.";
            return RedirectToPage(); // refresh the same page
        }
    }
}