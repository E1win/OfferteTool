using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class TenderChangeLog
{
    public Guid Id { get; set; }

    public required Guid TenderId { get; set; }
    public Tender? Tender { get; set; }

    public required TenderChangeLogType Type { get; set; }

    [MaxLength(128)]
    public required string FieldName { get; set; }

    [MaxLength(2048)]
    public required string OldValue { get; set; }

    [MaxLength(2048)]
    public required string NewValue { get; set; }

    [MaxLength(4096)]
    public required string SupplierVisibleMessage { get; set; }

    public required DateTimeOffset ChangedAtUtc { get; set; }

    [MaxLength(450)]
    public required string ChangedByUserId { get; set; }

    [MaxLength(256)]
    public required string ChangedByDisplayName { get; set; }
}
