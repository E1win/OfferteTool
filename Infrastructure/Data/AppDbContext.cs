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
    public DbSet<TenderSubmissionReview> TenderSubmissionReviews => Set<TenderSubmissionReview>();
    public DbSet<TenderQuestionReview> TenderQuestionReviews => Set<TenderQuestionReview>();
    public DbSet<TenderChangeLog> TenderChangeLogs => Set<TenderChangeLog>();
    public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();

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
        ConfigureTenderSubmissionReview(modelBuilder);
        ConfigureTenderQuestionReview(modelBuilder);
        ConfigureTenderReviewer(modelBuilder);
        ConfigureTenderChangeLog(modelBuilder);
        ConfigureSecurityAuditLog(modelBuilder);
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

            e.HasMany(t => t.Reviewers)
               .WithOne(r => r.Tender)
               .HasForeignKey(r => r.TenderId)
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

            e.HasMany(s => s.Reviews)
                .WithOne(r => r.Submission)
                .HasForeignKey(r => r.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // One submission per supplier per tender
            e.HasIndex(s => new { s.TenderId, s.SupplierId })
                .IsUnique();
        });
    }

    private static void ConfigureTenderAnswer(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<ChoiceAnswerSelection>();

        modelBuilder.Entity<TenderAnswer>(e =>
        {
            // Every question can only be answered once per submission
            e.HasIndex(s => new { s.SubmissionId, s.QuestionId })
                .IsUnique();

            e.Property(a => a.EncryptedPayload)
                .IsRequired();

            e.Property(a => a.Nonce)
                .HasMaxLength(12)
                .IsRequired();

            e.Property(a => a.Tag)
                .HasMaxLength(16)
                .IsRequired();

            e.HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasDiscriminator<AnswerType>(a => a.Type)
                .HasValue<TextAnswer>(AnswerType.Text)
                .HasValue<ChoiceAnswer>(AnswerType.Choice)
                .HasValue<NumberAnswer>(AnswerType.Numeric);
        });

        modelBuilder.Entity<TextAnswer>()
            .Ignore(a => a.TextValue);

        modelBuilder.Entity<NumberAnswer>()
            .Ignore(a => a.NumericValue);

        modelBuilder.Entity<ChoiceAnswer>()
            .Ignore(a => a.Selections);
    }

    private static void ConfigureTenderSubmissionReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderSubmissionReview>(e =>
        {
            e.HasMany(r => r.QuestionReviews)
                .WithOne(qr => qr.SubmissionReview)
                .HasForeignKey(qr => qr.SubmissionReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerUserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => new { r.SubmissionId, r.ReviewerUserId })
                .IsUnique();
        });
    }

    private static void ConfigureTenderQuestionReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderQuestionReview>(e =>
        {
            e.HasOne(qr => qr.Question)
                .WithMany()
                .HasForeignKey(qr => qr.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(qr => new { qr.SubmissionReviewId, qr.QuestionId })
                .IsUnique();
        });
    }

    private static void ConfigureTenderReviewer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderReviewer>(e =>
        {
            e.HasIndex(r => new { r.TenderId, r.UserId })
                    .IsUnique(); // One reviewer per user per tender

            e.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Remove reviewer if user is deleted
        });
    }

    private static void ConfigureTenderChangeLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenderChangeLog>(e =>
        {
            e.HasIndex(changeLog => new { changeLog.TenderId, changeLog.ChangedAtUtc });

            e.HasOne(changeLog => changeLog.Tender)
                .WithMany(tender => tender.ChangeLogs)
                .HasForeignKey(changeLog => changeLog.TenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSecurityAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecurityAuditLog>(e =>
        {
            e.HasIndex(log => new { log.EventType, log.OccurredAtUtc });
            e.HasIndex(log => new { log.ActorUserId, log.OccurredAtUtc });
            e.HasIndex(log => new { log.TargetUserId, log.OccurredAtUtc });
            e.HasIndex(log => new { log.TargetOrganisationId, log.OccurredAtUtc });
        });
    }
}
