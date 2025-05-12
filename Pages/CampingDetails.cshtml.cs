using System.Threading.Tasks;
using CampingMaster.Data;
using CampingMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CampingMaster.Pages
{
    public class CampingDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        // The Camping entity (with PhotoUrls and Reviews helpers)
        public Camping Camping { get; private set; }

        public CampingDetailsModel(ApplicationDbContext db)
        {
            _db = db;
        }

        // We expect the route to supply an "id" (long)
        public async Task<IActionResult> OnGetAsync(long id)
        {
            // Load from DB (includes PhotoUrlsJson & ReviewsJson)
            Camping = await _db.Campings
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (Camping == null)
                return NotFound();

            // At this point Camping.PhotoUrls and Camping.Reviews are available
            return Page();
        }
    }
}
