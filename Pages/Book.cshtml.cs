using CampingMaster.Data;
using CampingMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampingMaster.Pages
{
    public class BookModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public BookModel(ApplicationDbContext context)
        {
            _db = context;
        }

        [BindProperty]
        public Reservation Reservation { get; set; }
        public User CurrentUser { get; set; }
        public Camping? Camping { get; set; }

        public async Task<IActionResult> OnGetAsync(long campingId)
        {
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                CurrentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
            if (CurrentUser == null)
            {
                return RedirectToPage("/Login");
            }
            Camping = await _db.Campings.FindAsync(campingId);
            if (Camping == null)
            {
                return NotFound();
            }

            Reservation = new Reservation
            {
                CampingId = campingId
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                CurrentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            }

            if (CurrentUser == null)
            {
                return RedirectToPage("/Login");
            }

            Camping = await _db.Campings
                .Include(c => c.Reservations)
                .FirstOrDefaultAsync(c => c.Id == Reservation.CampingId);

            if (Camping == null)
            {
                return NotFound();
            }

            // if (!ModelState.IsValid)
            // {
            //     return Page();
            // }

            Reservation.UserId = CurrentUser.Id;

            _db.Reservations.Add(Reservation);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation submitted successfully!";
            return RedirectToPage("/Index");
        }

    }
}
