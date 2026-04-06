using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "1", Name = "Inkoper", NormalizedName = "INKOPER", ConcurrencyStamp = "1" },
            new IdentityRole { Id = "2", Name = "Beoordelaar", NormalizedName = "BEOORDELAAR", ConcurrencyStamp = "2" },
            new IdentityRole { Id = "3", Name = "Beheerder", NormalizedName = "BEHEERDER", ConcurrencyStamp = "3" },
            new IdentityRole { Id = "4", Name = "Leverancier", NormalizedName = "LEVERANCIER", ConcurrencyStamp = "4" }
        );
    }
}
