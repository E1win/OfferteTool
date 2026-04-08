using Domain.Entities;
using Domain.Entities.TenderAnswers;
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
    public DbSet<TenderSubmission> TenderSubmissions => Set<TenderSubmission>();
    public DbSet<TenderAnswer> TenderAnswers => Set<TenderAnswer>();
    public DbSet<ChoiceAnswerSelection> ChoiceAnswerSelections => Set<ChoiceAnswerSelection>();

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
        ConfigureTenderSubmission(modelBuilder);
        ConfigureTenderAnswer(modelBuilder);
        ConfigureChoiceAnswerSelection(modelBuilder);
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

            e.HasMany(t => t.Submissions)
                .WithOne(s => s.Tender)
                .HasForeignKey(s => s.TenderId)
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

    private static void ConfigureTenderSubmission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderSubmission>(e =>
        {
            e.HasMany(s => s.Answers)
                .WithOne(a => a.Submission)
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // One submission per supplier per tender
            e.HasIndex(s => new { s.TenderId, s.SupplierId })
                .IsUnique();
        });
    }

    private static void ConfigureTenderAnswer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderAnswer>(e =>
        {
            // Every question can only be answered once per submission
            e.HasIndex(s => new { s.SubmissionId, s.QuestionId })
                .IsUnique();

            e.HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasDiscriminator<AnswerType>(a => a.Type)
                .HasValue<TextAnswer>(AnswerType.Text)
                .HasValue<ChoiceAnswer>(AnswerType.Choice)
                .HasValue<NumberAnswer>(AnswerType.Numeric);
        });

        modelBuilder.Entity<ChoiceAnswer>(e =>
        {
            e.HasMany(a => a.Selections)
                .WithOne(s => s.ChoiceAnswer)
                .HasForeignKey(s => s.ChoiceAnswerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureChoiceAnswerSelection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ChoiceAnswerSelection>(e =>
        {
            e.HasOne(s => s.Option)
                .WithMany()
                .HasForeignKey(s => s.OptionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(s => new { s.ChoiceAnswerId, s.OptionId })
                .IsUnique();
        });
    }
}
