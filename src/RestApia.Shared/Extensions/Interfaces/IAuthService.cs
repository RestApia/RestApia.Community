using RestApia.Shared.Extensions.Models;
namespace RestApia.Shared.Extensions.Interfaces;

public interface IAuthService
{
    string DisplayName { get; }
    bool IsReloadFeatureAvailable { get; }
    bool IsShowPayloadFeatureAvailable { get; }

    Type SettingsType { get; }
    Task<IReadOnlyCollection<ExtensionValueModel>> GetValuesAsync(object settingsObj, Guid authId);
    Task<bool> ReloadAsync(object settings, Guid authId);
}
