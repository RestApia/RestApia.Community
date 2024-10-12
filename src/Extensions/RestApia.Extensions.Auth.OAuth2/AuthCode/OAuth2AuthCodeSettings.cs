using RestApia.Shared.Values.Attributes;
namespace RestApia.Extensions.Auth.OAuth2.AuthCode;

public record OAuth2AuthCodeSettings
{
    [ContentValue("AuthUrl", "Authorization URL", IsRequired = true)]
    public required string AuthUrl { get; init; }

    [ContentValue("TokenUrl", "Access token URL", IsRequired = true)]
    public required string AccessTokenUrl { get; init; }

    [ContentValue("RedirectUrl", "Redirect URL", IsRequired = true)]
    public required string RedirectUrl { get; init; }

    [ContentValue("ClientId", "Client Id", IsRequired = true)]
    public required string ClientId { get; init; }

    [ContentValue("ClientSecret", "Client secret")]
    public string ClientSecret { get; init; } = string.Empty;

    [ContentValue("SendMethod", "Credentials send method. Can be 'Header' or 'Body'")]
    public required string CredentialsSendMethod { get; init; }

    [ContentValue("Scopes", "List of scopes, separated by space, comma or semicolon")]
    public string Scopes { get; init; } = string.Empty;

    [ContentValue("Audience", "Audience")]
    public string Audience { get; init; } = string.Empty;

    [ContentValue("State", "State")]
    public string State { get; init; } = string.Empty;

    [ContentValue("Resource", "Resource")]
    public string Resource { get; init; } = string.Empty;

    [ContentValue("Origin", "Origin")]
    public string Origin { get; init; } = string.Empty;
}
