using RestApia.Shared.Values.Attributes;
namespace RestApia.Extensions.Auth.OAuth2.Implicit;

public record OAuth2ImplicitSettings
{
    [ContentValue("AuthUrl", "Authorization URL", IsRequired = true)]
    public required string AuthUrl { get; init; }

    [ContentValue("RedirectUrl", "Redirect URL", IsRequired = true)]
    public required string RedirectUrl { get; init; }

    [ContentValue("ClientId", "Client Id", IsRequired = true)]
    public required string ClientId { get; init; }

    [ContentValue("Scopes", "List of scopes, separated by space, comma or semicolon")]
    public string Scopes { get; init; } = string.Empty;

    [ContentValue("Audience", "Audience")]
    public string Audience { get; init; } = string.Empty;
}
