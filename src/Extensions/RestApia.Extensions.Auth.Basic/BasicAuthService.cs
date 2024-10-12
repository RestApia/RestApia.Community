using System.Text;
using RestApia.Shared.Extensions.Interfaces;
using RestApia.Shared.Extensions.Models;
using RestApia.Shared.Values.Enums;
namespace RestApia.Extensions.Auth.Basic;

public class BasicAuthService : IAuthService
{
    public Type SettingsType => typeof(BasicAuthSettings);

    public string DisplayName => "Basic Authorization";
    public bool IsReloadFeatureAvailable => false;
    public bool IsShowPayloadFeatureAvailable => true;

    public Task<IReadOnlyCollection<ExtensionValueModel>> GetValuesAsync(object settingsObj, Guid authId) => Task.FromResult(GetValues(settingsObj));

    private static IReadOnlyCollection<ExtensionValueModel> GetValues(object rawSettings)
    {
        if (rawSettings is not BasicAuthSettings settings) return [];
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.UserName}:{settings.Password}"));

        return
        [
            new () { Name = "Authorization", Type = ValueTypeEnum.Header, Value = $"Basic {token}" },
        ];
    }

    public Task<bool> ReloadAsync(object settings, Guid authId) => throw new NotSupportedException();
}
