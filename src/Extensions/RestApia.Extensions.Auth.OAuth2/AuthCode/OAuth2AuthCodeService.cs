using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestApia.Shared.Common;
using RestApia.Shared.Extensions.Interfaces;
using RestApia.Shared.Extensions.Models;
using RestApia.Shared.Values.Enums;
namespace RestApia.Extensions.Auth.OAuth2.AuthCode;

public class OAuth2AuthCodeService : IAuthService
{
    private static readonly HttpClient AccessTokenHttpClient = new ();

    private readonly IExtensionValuesStorage _storage;
    private readonly ILogger _logger;
    private readonly IExtensionDialogs _dialogs;

    public OAuth2AuthCodeService(IExtensionValuesStorage storage, ILogger logger, IExtensionDialogs dialogs)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    }

    public string DisplayName => "OAuth2 Auth Code";
    public bool IsReloadFeatureAvailable => true;
    public bool IsShowPayloadFeatureAvailable => true;
    public Type SettingsType => typeof(OAuth2AuthCodeSettings);

    public Task<IReadOnlyCollection<ExtensionValueModel>> GetValuesAsync(object settingsObj, Guid authId)
    {
        var result = _storage.GetValues(authId);
        result = [..result, ..OAuth2Helper.GetCustomValues(result, _logger)];
        return Task.FromResult(result);
    }

    public async Task<bool> ReloadAsync(object settingsObj, Guid authId)
    {
        if (settingsObj is not OAuth2AuthCodeSettings settings)
        {
            _logger.LogWarning("Invalid settings type '{SettingsType}' for service {DisplayName} found", settingsObj?.GetType(), DisplayName);
            return false;
        }

        var codeResponse = await RequestCodeOrDefaultAsync(settings);
        if (codeResponse == null) return false;
        if (!TryExtractCodeOrError(codeResponse, out var codeString)) return false;

        var tokenString = await RequestTokenOrDefaultAsync(codeString, settings);
        if (tokenString == null) return false;

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

    private async Task<BrowserDialogResult?> RequestCodeOrDefaultAsync(OAuth2AuthCodeSettings settings)
    {
        var scopes = settings.Scopes.SplitRegex(";, ");
        var queryParams = new List<KeyValuePair<string, string>>
        {
            new ("response_type", "code"),
            new ("client_id", settings.ClientId),
            new ("redirect_uri", settings.RedirectUrl),
            new ("scope", string.Join(' ', scopes)),
            new ("state", settings.State),
            new ("audience", settings.Audience),
            new ("resource", settings.Resource),
            new ("nonce", OAuth2Helper.Rnd.NextInt64().ToString()),
        };

        var startUrl = OAuth2Helper.AddQueryParams(settings.AuthUrl, queryParams);
        if (string.IsNullOrWhiteSpace(startUrl))
        {
            _dialogs.ShowError("Cannot open Authorization page, URL build from parameters is empty");
            return null;
        }

        // build stop URL
        var stopUrl = settings.RedirectUrl;
        if (string.IsNullOrWhiteSpace(stopUrl))
        {
            _dialogs.ShowError("Redirect URL cannot be empty");
            return null;
        }

        var result = await _dialogs.OpenAuthBrowserAsync(startUrl, stopUrl, "OAuth2 Auth Code");
        return result;
    }

    private async Task<string?> RequestTokenOrDefaultAsync(string codeString, OAuth2AuthCodeSettings settings)
    {
        // prepare request
        var requestParams = new List<KeyValuePair<string, string>>
        {
            new ("grant_type", "authorization_code"),
            new ("code", codeString),
            new ("redirect_uri", settings.RedirectUrl),
            new ("state", settings.State),
            new ("audience", settings.Audience),
            new ("resource", settings.Resource),
        };

        var headers = new List<KeyValuePair<string, string>>
        {
            new ("Accept", "application/x-www-form-urlencoded, application/json"),
            new ("Origin", settings.Origin),
        };

        switch (settings.CredentialsSendMethod.ToLower().Trim())
        {
            case "body":
                requestParams.Add(new ("client_id", settings.ClientId));
                requestParams.Add(new ("client_secret", settings.ClientSecret));
                break;

            default:
                var credentialsString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));
                headers.Add(new ("Authorization", "Basic " + credentialsString));
                break;
        }

        // send request
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.AccessTokenUrl);
        request.Content = new FormUrlEncodedContent(requestParams.Where(x => !x.Value.IsEmpty()));

        foreach (var header in headers.Where(x => !x.Value.IsEmpty()))
            request.Headers.Add(header.Key, header.Value);

        var response = await AccessTokenHttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _dialogs.ShowError($"Cannot retrieve access token. HTTP Status code: {response.ReasonPhrase} ({response.StatusCode})");
            return null;
        }

        // read response
        var responseContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            _dialogs.ShowError("Error occurred during authorization. Empty response received.");
            return null;
        }

        var extracted = response.Content.Headers.ContentType?.MediaType?.Trim().Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) == true
            ? TryExtractTokenFromUrlEncoded(responseContent, out var result)
            : TryExtractTokenFromJson(responseContent, out result);

        if (!extracted)
        {
            _dialogs.ShowError("Error occurred during authorization. Token not found in response.");
            return null;
        }

        return result;
    }

    private bool TryExtractCodeOrError(BrowserDialogResult response, [NotNullWhen(true)] out string? code)
    {
        code = null;
        var url = response.Url;

        if (string.IsNullOrWhiteSpace(url))
        {
            _dialogs.ShowError("Empty URL received in OAuth2 AuthCode flow window");
            return false;
        }

        var queryString = OAuth2Helper.GetQueryParams(url);

        // check errors
        var errorCode = queryString["error"];
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            var errorDescription = queryString["error_description"];
            _dialogs.ShowError($"Error response received: {errorDescription} ({errorCode})");
            return false;
        }

        // find code
        code = queryString["code"];
        if (string.IsNullOrWhiteSpace(code))
            _dialogs.ShowError("No code found in OAuth2 AuthCode flow window response");

        return !string.IsNullOrWhiteSpace(code);
    }

    private bool TryExtractTokenFromUrlEncoded(string responseContent, [NotNullWhen(true)] out string? token)
    {
        token = null;

        // expected content: access_token=gho_tUWZ&scope=&token_type=bearer
        var responseParams = OAuth2Helper.GetQueryParams(responseContent);
        token = responseParams["access_token"];

        if (string.IsNullOrWhiteSpace(token))
        {
            _dialogs.ShowError("Error occurred during authorization. Access token not found.");
            return false;
        }

        return !token.IsEmpty();
    }

    private bool TryExtractTokenFromJson(string responseContent, out string? token)
    {
        token = null;
        var responseJson = JsonConvert.DeserializeObject<JObject>(responseContent);

        /* expected format
        {
          "token_type": "Bearer",
          "scope": "read:example write:example",
          "expires_in": 4597,
          "ext_expires_in": 4597,
          "access_token": "eyJ0eXA..."
        } */

        if (responseJson == null)
        {
            _dialogs.ShowError("Error occurred during authorization. Response content is empty.");
            return false;
        }

        if (!responseJson.TryGetValue("access_token", out var accessTokenProp))
        {
            _dialogs.ShowError("Error occurred during authorization. Access token not found.");
            return false;
        }

        token = accessTokenProp.Value<string?>();
        if (string.IsNullOrWhiteSpace(token))
        {
            _dialogs.ShowError("Error occurred during authorization. Access token is empty.");
            return false;
        }

        return true;
    }
}
