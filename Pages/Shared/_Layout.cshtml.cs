using CampingMaster.Data;
using CampingMaster.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CampingMaster.Pages.Shared
{
    public class _LayoutModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public User CurrentUser { get; set; }

        public _LayoutModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task OnGetAsync()
        {
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                CurrentUser = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == username);
            }
        }
    }
}
