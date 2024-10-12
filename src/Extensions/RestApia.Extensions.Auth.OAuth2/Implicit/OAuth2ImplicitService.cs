using System.Web;
using Microsoft.Extensions.Logging;
using RestApia.Shared.Common;
using RestApia.Shared.Extensions.Interfaces;
using RestApia.Shared.Extensions.Models;
using RestApia.Shared.Values.Enums;
namespace RestApia.Extensions.Auth.OAuth2.Implicit;

public class OAuth2ImplicitService : IAuthService
{
    private readonly IExtensionValuesStorage _storage;
    private readonly ILogger _logger;
    private readonly IExtensionDialogs _dialogs;

    public OAuth2ImplicitService(IExtensionValuesStorage storage, ILogger logger, IExtensionDialogs dialogs)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    }

    public string DisplayName => "OAuth2 Implicit";
    public bool IsReloadFeatureAvailable => true;
    public bool IsShowPayloadFeatureAvailable => true;
    public Type SettingsType => typeof(OAuth2ImplicitSettings);

    public Task<IReadOnlyCollection<ExtensionValueModel>> GetValuesAsync(object settingsObj, Guid authId)
    {
        var result = _storage.GetValues(authId);
        result = [..result, ..OAuth2Helper.GetCustomValues(result, _logger)];
        return Task.FromResult(result);
    }

    public async Task<bool> ReloadAsync(object settingsObj, Guid authId)
    {
        if (settingsObj is not OAuth2ImplicitSettings settings)
        {
            _logger.LogWarning("Invalid settings type '{SettingsType}' for service {DisplayName} found", settingsObj?.GetType(), DisplayName);
            return false;
        }

        // build start URL
        var scopes = settings.Scopes.SplitRegex(";, ");

        var startUrl = OAuth2Helper.AddQueryParams(settings.AuthUrl, [
            new ("response_type", "token"),
            new ("client_id", settings.ClientId),
            new ("redirect_uri", settings.RedirectUrl),
            new ("scope", string.Join(' ', scopes)),
            new ("audience", settings.Audience),
            new ("nonce", OAuth2Helper.Rnd.NextInt64().ToString()),
        ]);

        if (string.IsNullOrWhiteSpace(startUrl))
        {
            _dialogs.ShowError("Cannot open Authorization page, URL build from parameters is empty");
            return false;
        }

        // build stop URL
        var stopUrl = settings.RedirectUrl;
        if (string.IsNullOrWhiteSpace(stopUrl))
        {
            _dialogs.ShowError("Redirect URL cannot be empty");
            return false;
        }

        // var result = await AuthWindow.OpenDialogAsync(startUrl, stopUrl, context.AuthDna.Name, context.ParentWindow, context.Services);
        var result = await _dialogs.OpenAuthBrowserAsync(startUrl, stopUrl, "OAuth2 Implicit");
        if (result == null)
        {
            _logger.LogDebug("Auth canceled by user");
            return false;
        }

        // validate response and extract access token
        var errorMessage = TryGetTokenOrError(result, out var tokenString);
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            _dialogs.ShowError(errorMessage);
            return false;
        }

        // read from token when it will be expired
        DateTime? expiresAt = null;
        if (!OAuth2Helper.TryParseToken(tokenString, _logger, out var token))
        {
            _logger.LogWarning("Cannot validate JWT token, expiration date will not be set");
        }
        else
        {
            expiresAt = token.ValidTo;
        }

        IReadOnlyCollection<ExtensionValueModel> values =
        [
            new ()
            {
                Name = "Authorization",
                Type = ValueTypeEnum.Header,
                Value = $"Bearer {tokenString}",
            },
        ];

        _storage.SetValues(values, authId, expiresAt);
        return true;
    }

    private static string TryGetTokenOrError(BrowserDialogResult result, out string? token)
    {
        // read response details
        token = null;
        var hashParts = result.Url.Split('#');

        if (hashParts.Length < 2)
            return "Empty hash part found in response for OAuth2 implicit auth";

        var queryString = HttpUtility.ParseQueryString(hashParts[1]);
        if (queryString == null)
            return "Empty hash part found in response for OAuth2 implicit auth";

        // check if error returned
        var errorCode = queryString["error"];
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            var errorDescription = queryString["error_description"];
            return $"Error response received: {errorDescription} ({errorCode})";
        }

        // extract token
        token = queryString["access_token"]!;
        if (string.IsNullOrWhiteSpace(token))
            return "Empty token received in response";

        return string.Empty;
    }
}
