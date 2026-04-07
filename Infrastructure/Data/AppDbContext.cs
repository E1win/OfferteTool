using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AppRoles = Domain.Constants.Roles;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<Tender> Tenders => Set<Tender>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "1", Name = AppRoles.Inkoper, NormalizedName = AppRoles.Inkoper.ToUpper(), ConcurrencyStamp = "1" },
            new IdentityRole { Id = "2", Name = AppRoles.Beoordelaar, NormalizedName = AppRoles.Beoordelaar.ToUpper(), ConcurrencyStamp = "2" },
            new IdentityRole { Id = "3", Name = AppRoles.Beheerder, NormalizedName = AppRoles.Beheerder.ToUpper(), ConcurrencyStamp = "3" },
            new IdentityRole { Id = "4", Name = AppRoles.Leverancier, NormalizedName = AppRoles.Leverancier.ToUpper(), ConcurrencyStamp = "4" }
        );
    }
}
