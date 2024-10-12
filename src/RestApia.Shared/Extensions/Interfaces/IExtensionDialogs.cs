using RestApia.Shared.Extensions.Models;
namespace RestApia.Shared.Extensions.Interfaces;

public interface IExtensionDialogs
{
    void ShowError(string message);

    Task<BrowserDialogResult> OpenAuthBrowserAsync(string url, string stopUrl, string title);
}
