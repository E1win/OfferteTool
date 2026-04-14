namespace Presentation.Models.Api;

public class ApiResponse<T>
{
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<string> Errors { get; init; } = [];
}
