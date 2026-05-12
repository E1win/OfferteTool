using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.Email;
using Application.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Tests.Application.Services;

public class TenderChangeNotificationServiceTests
{
    private readonly Mock<ITenderSubmissionRepository> tenderSubmissionRepository = new();
    private readonly Mock<IApplicationUserRepository> applicationUserRepository = new();
    private readonly Mock<IEmailSender> emailSender = new();

    [Fact]
    public async Task NotifySubmittedSuppliersAsync_SendsEmailToActiveSupplierUsersForSubmittedSuppliers()
    {
        // Arrange
        var tender = CreateTender();
        var supplierId = Guid.NewGuid();
        var supplierUser = CreateUser(supplierId, "supplier@example.com", isActive: true);
        var inactiveSupplierUser = CreateUser(supplierId, "inactive@example.com", isActive: false);
        var buyerUser = CreateUser(supplierId, "buyer@example.com", isActive: true);

        tenderSubmissionRepository
            .Setup(repository => repository.GetByTenderWithSuppliersAsync(tender.Id))
            .ReturnsAsync(
            [
                new TenderSubmission { TenderId = tender.Id, SupplierId = supplierId }
            ]);
        applicationUserRepository
            .Setup(repository => repository.GetByOrganisationAsync(supplierId))
            .ReturnsAsync([supplierUser, inactiveSupplierUser, buyerUser]);
        applicationUserRepository
            .Setup(repository => repository.GetRolesAsync(supplierUser))
            .ReturnsAsync([Roles.Leverancier]);
        applicationUserRepository
            .Setup(repository => repository.GetRolesAsync(buyerUser))
            .ReturnsAsync([Roles.Inkoper]);

        var service = CreateService();

        // Act
        await service.NotifySubmittedSuppliersAsync(
            tender,
            [
                new TenderChangeLog
                {
                    TenderId = tender.Id,
                    Type = TenderChangeLogType.TenderTitleAmended,
                    FieldName = nameof(Tender.Title),
                    OldValue = "Oude titel",
                    NewValue = "Nieuwe titel",
                    SupplierVisibleMessage = "De titel is gewijzigd.",
                    ChangedAtUtc = DateTimeOffset.UtcNow,
                    ChangedByUserId = "user-1",
                    ChangedByDisplayName = "Inkoper"
                }
            ]);

        // Assert
        emailSender.Verify(sender => sender.SendAsync(
            It.Is<EmailMessage>(message =>
                message.To == "supplier@example.com"
                && message.Subject.Contains(tender.Title)
                && message.HtmlBody != null
                && message.HtmlBody.Contains("<p>Beste leverancier,</p>")
                && message.HtmlBody.Contains("<strong>Vorige waarde:</strong>")
                && message.HtmlBody.Contains("Oude titel")
                && message.HtmlBody.Contains("Nieuwe titel")),
            It.IsAny<CancellationToken>()), Times.Once);
        emailSender.Verify(sender => sender.SendAsync(
            It.Is<EmailMessage>(message => message.To == "inactive@example.com" || message.To == "buyer@example.com"),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private TenderChangeNotificationService CreateService()
    {
        return new TenderChangeNotificationService(
            tenderSubmissionRepository.Object,
            applicationUserRepository.Object,
            emailSender.Object);
    }

    private static Tender CreateTender()
    {
        return new Tender
        {
            Id = Guid.NewGuid(),
            Title = "Tender",
            Description = "Description",
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Status = TenderStatus.Open,
            IsPublic = true,
            OrganisationId = Guid.NewGuid()
        };
    }

    private static ApplicationUser CreateUser(Guid organisationId, string email, bool isActive)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            OrganisationId = organisationId,
            IsActive = isActive
        };
    }
}
