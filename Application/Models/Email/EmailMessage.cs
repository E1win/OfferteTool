namespace Application.Models.Email;

public class EmailMessage
{
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string TextBody { get; set; }
    public string? HtmlBody { get; set; }
}
