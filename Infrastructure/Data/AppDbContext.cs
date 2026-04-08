using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AppRoles = Domain.Constants.Roles;

namespace Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<Tender> Tenders => Set<Tender>();
    public DbSet<TenderQuestion> TenderQuestions => Set<TenderQuestion>();
    public DbSet<TenderQuestionOption> TenderQuestionOptions => Set<TenderQuestionOption>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureRole(modelBuilder);
        ConfigureTender(modelBuilder);
        ConfigureTenderQuestion(modelBuilder);
        ConfigureTenderQuestionOption(modelBuilder);
    }

    private static void ConfigureRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "1", Name = AppRoles.Inkoper, NormalizedName = AppRoles.Inkoper.ToUpper(), ConcurrencyStamp = "1" },
            new IdentityRole { Id = "2", Name = AppRoles.Beoordelaar, NormalizedName = AppRoles.Beoordelaar.ToUpper(), ConcurrencyStamp = "2" },
            new IdentityRole { Id = "3", Name = AppRoles.Beheerder, NormalizedName = AppRoles.Beheerder.ToUpper(), ConcurrencyStamp = "3" },
            new IdentityRole { Id = "4", Name = AppRoles.Leverancier, NormalizedName = AppRoles.Leverancier.ToUpper(), ConcurrencyStamp = "4" }
        );
    }

    private static void ConfigureTender(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tender>(e =>
        {
            e.HasMany(t => t.Questions)
                .WithOne(q => q.Tender)
                .HasForeignKey(q => q.TenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTenderQuestion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderQuestion>(e =>
        {
            e.HasDiscriminator<QuestionType>(q => q.Type)
                .HasValue<TextQuestion>(Domain.Enums.QuestionType.Text)
                .HasValue<NumberQuestion>(Domain.Enums.QuestionType.Numeric)
                .HasValue<ChoiceQuestion>(Domain.Enums.QuestionType.Choice);

            e.HasIndex(q => new { q.TenderId, q.Order })
                .IsUnique();
        });

        modelBuilder.Entity<ChoiceQuestion>(entity =>
        {
            entity.HasMany(q => q.Options)
                .WithOne(o => o.Question)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTenderQuestionOption(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderQuestionOption>(e =>
        {
            e.HasIndex(o => new { o.QuestionId, o.Order })
                .IsUnique();
        });
    }
}
