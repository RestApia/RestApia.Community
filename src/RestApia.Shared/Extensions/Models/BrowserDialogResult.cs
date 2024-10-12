namespace RestApia.Shared.Extensions.Models;

public record BrowserDialogResult
{
    public required string Url { get; init; }
    public Dictionary<string, string> Headers { get; init; } = [];
}
