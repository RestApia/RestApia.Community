using RestApia.Shared.Values.Attributes;
namespace RestApia.Extensions.Auth.Basic;

public record BasicAuthSettings
{
    [ContentValue("Name", "User login name", IsRequired = true)]
    public string UserName { get; init; } = string.Empty;

    [ContentValue("Password", "User password", IsRequired = true)]
    public string Password { get; init; } = string.Empty;
}
