using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.Email;
using Domain.Constants;
using Domain.Entities;
using System.Net;

namespace Application.Services;

public class TenderChangeNotificationService(
    ITenderSubmissionRepository tenderSubmissionRepository,
    IApplicationUserRepository applicationUserRepository,
    IEmailSender emailSender) : ITenderChangeNotificationService
{
    public async Task NotifySubmittedSuppliersAsync(Tender tender, IReadOnlyCollection<TenderChangeLog> changes)
    {
        ArgumentNullException.ThrowIfNull(tender);

        if (changes.Count == 0)
            return;

        var recipients = await GetSubmittedSupplierRecipientsAsync(tender.Id);

        foreach (var recipient in recipients)
            await emailSender.SendAsync(CreateEmail(tender, recipient, changes));
    }

    private async Task<List<ApplicationUser>> GetSubmittedSupplierRecipientsAsync(Guid tenderId)
    {
        var submissions = await tenderSubmissionRepository.GetByTenderWithSuppliersAsync(tenderId);
        var supplierIds = submissions
            .Select(submission => submission.SupplierId)
            .Distinct()
            .ToList();

        var recipients = new List<ApplicationUser>();

        foreach (var supplierId in supplierIds)
        {
            var users = await applicationUserRepository.GetByOrganisationAsync(supplierId);

            foreach (var user in users)
            {
                if (!user.IsActive || string.IsNullOrWhiteSpace(user.Email))
                    continue;

                var roles = await applicationUserRepository.GetRolesAsync(user);
                if (roles.Contains(Roles.Leverancier))
                    recipients.Add(user);
            }
        }

        return recipients
            .GroupBy(user => user.Email!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static EmailMessage CreateEmail(Tender tender, ApplicationUser recipient, IReadOnlyCollection<TenderChangeLog> changes)
    {
        return new EmailMessage
        {
            To = recipient.Email!,
            Subject = $"Wijziging in offertetraject: {tender.Title}",
            TextBody = "Deze e-mail bevat HTML-inhoud.",
            HtmlBody = CreateHtmlBody(tender, changes)
        };
    }

    private static string CreateHtmlBody(Tender tender, IReadOnlyCollection<TenderChangeLog> changes)
    {
        var changeItems = string.Join(Environment.NewLine, changes.Select(change => $"""
            <li>
                {Encode(change.SupplierVisibleMessage)}<br>
                <strong>Vorige waarde:</strong><br>
                {EncodeMultiline(change.OldValue)}<br>
                <strong>Nieuwe waarde:</strong><br>
                {EncodeMultiline(change.NewValue)}
            </li>
            """));

        return $"""
            <p>Beste leverancier,</p>
            <p>Er is een wijziging doorgevoerd in het offertetraject <strong>{Encode(tender.Title)}</strong> waarop u heeft ingeschreven.</p>
            <ul>
                {changeItems}
            </ul>
            <p>Bekijk het offertetraject voor de volledige wijzigingshistorie.</p>
            <p>Met vriendelijke groet,<br>OfferteTool</p>
            """;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string EncodeMultiline(string value) =>
        Encode(value).Replace(Environment.NewLine, "<br>");
}
