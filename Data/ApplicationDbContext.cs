using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CampingMaster.Models;

namespace CampingMaster.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                  "Server=(localdb)\\mssqllocaldb;Database=CampingMasterDb;Trusted_Connection=True;MultipleActiveResultSets=true"
                );
            }
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Camping> Campings { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
    }
}
