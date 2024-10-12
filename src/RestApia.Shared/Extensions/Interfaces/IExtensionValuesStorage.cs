using RestApia.Shared.Extensions.Models;
namespace RestApia.Shared.Extensions.Interfaces;

/// <summary>
/// Extension values storage.
/// </summary>
public interface IExtensionValuesStorage
{
    /// <summary>
    /// Read all extension values.
    /// </summary>
    IReadOnlyCollection<ExtensionValueModel> GetValues(Guid dataId);

    /// <summary>
    /// Update extension values.
    /// </summary>
    void SetValues(IReadOnlyCollection<ExtensionValueModel> values, Guid dataId, DateTime? expiresAt = null);
}
