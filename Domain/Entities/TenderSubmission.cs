using Domain.Entities.TenderAnswers;

namespace Domain.Entities;

public class TenderSubmission
{
    public Guid Id { get; set; }

    public Guid TenderId { get; set; }
    public Tender? Tender { get; set; }
    
    public Guid SupplierId { get; set; }
    public Organisation? Supplier { get; set; }
    
    public DateTime SubmittedAt { get; set; }

    public ICollection<TenderAnswer> Answers { get; set; } = [];
}
