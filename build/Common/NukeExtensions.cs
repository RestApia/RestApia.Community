using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common;

[SuppressMessage("ReSharper", "CheckNamespace")]
public static class NukeExtensions
{
    public static T LoadLocalSecret<T>(this NukeBuild build, string key)
    {
        var secrets = Path.Combine(NukeBuild.RootDirectory, ".local", "secrets.json");
        if (!File.Exists(secrets))
            return default;

        var str = File.ReadAllText(secrets);
        var json = JsonConvert.DeserializeObject<JObject>(str);
        if (json == null || !json.TryGetValue(key, out var value)) return default;
        return value.ToObject<T>();
    }
}
