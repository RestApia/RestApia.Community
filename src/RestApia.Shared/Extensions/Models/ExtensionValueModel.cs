using RestApia.Shared.Values.Enums;
namespace RestApia.Shared.Extensions.Models;

public record ExtensionValueModel
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public required ValueTypeEnum Type { get; init; }
    public string Description { get; set; } = string.Empty;
}
