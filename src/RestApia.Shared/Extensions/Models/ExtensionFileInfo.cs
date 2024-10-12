using System.Diagnostics.CodeAnalysis;
namespace RestApia.Shared.Extensions.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public record ExtensionFileInfo
{
    public required string FullPath { get; init; }
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Author { get; init; }
    public required string Version { get; init; }
    public string Description { get; init; } = "No description provided.";
    public string ExtensionHomePage { get; init; } = string.Empty;
    public bool IsPreview { get; init; } = true;
}
