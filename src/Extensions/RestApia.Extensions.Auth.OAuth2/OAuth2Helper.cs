using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Web;
using Microsoft.Extensions.Logging;
using RestApia.Shared.Extensions.Models;
using RestApia.Shared.Values.Enums;

namespace RestApia.Extensions.Auth.OAuth2;

internal static class OAuth2Helper
{
    public static readonly Random Rnd = new ();

    public static bool TryParseToken(string? token, ILogger logger, [NotNullWhen(true)] out JwtSecurityToken? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Cannot parse empty JWT token");
            return false;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            result = handler.ReadJwtToken(token);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("Error during JWT token parsing: {ExMessage}", ex.Message);
            return false;
        }
    }

    public static string AddQueryParams(string url, IReadOnlyCollection<KeyValuePair<string, string>> parameters)
    {
        var uriBuilder = new UriBuilder(url);
        uriBuilder.Query = (string.IsNullOrWhiteSpace(uriBuilder.Query) ? "?" : "&") + BuildQueryString(parameters);
        return uriBuilder.ToString();
    }

    public static IReadOnlyCollection<ExtensionValueModel> GetCustomValues(IReadOnlyCollection<ExtensionValueModel> original, ILogger logger)
    {
        var tokenStr = original.FirstOrDefault(x => x.Name == "Authorization")?.Value?.ToString().Split(' ').LastOrDefault();

        if (string.IsNullOrWhiteSpace(tokenStr) || !TryParseToken(tokenStr, logger, out var token)) return [];

        var result = new List<ExtensionValueModel>();
        result.Add(new () { Name = "Valid from", Value = token.ValidFrom.ToString("s"), Type = ValueTypeEnum.Other, Description = "Info" });
        result.Add(new () { Name = "Valid to", Value = token.ValidTo.ToString("s"), Type = ValueTypeEnum.Other, Description = "Info" });
        result.Add(new () { Name = "Expired in", Value = token.ValidTo > DateTime.UtcNow ? (token.ValidTo - DateTime.UtcNow).ToString("g") : "Expired", Type = ValueTypeEnum.Other, Description = "Info" });
        result.AddRange(token.Claims.Select(claim => new ExtensionValueModel
        {
            Name = claim.Type,
            Value = claim.Value,
            Type = ValueTypeEnum.Other,
            Description = "JWT Claim",
        }));

        return result;
    }

    private static string BuildQueryString(IReadOnlyCollection<KeyValuePair<string, string>> parameters) =>
        string.Join("&", parameters
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(kvp => Uri.EscapeDataString(kvp.Key) + "=" + Uri.EscapeDataString(kvp.Value)));

    public static NameValueCollection GetQueryParams(string url)
    {
        var splitIndex = url.IndexOf('?', StringComparison.Ordinal);
        if (splitIndex == -1) return [];

        var query = url[(splitIndex + 1) ..];
        return HttpUtility.ParseQueryString(query);
    }
}
