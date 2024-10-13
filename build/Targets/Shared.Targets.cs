using System.Diagnostics.CodeAnalysis;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace Targets;

[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class Shared_Targets : NukeBuild
{
    const string FeedUrl = "https://nuget.pkg.github.com/RestApia/index.json";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Client version")]
    string Version = EnvironmentInfo.GetVariable<string>("version") ?? "0.0.1";

    [Parameter("NuGet API key")]
    string NuGetApiKey => EnvironmentInfo.GetVariable<string>("PAT") ?? this.LoadLocalSecret<string>("nuget_pat");

    AbsolutePath ProjectDirectory => RootDirectory / "src" / "RestApia.Shared";

    Target Shared_Clear => _ => _
        .Executes(() =>
        {
            DotNetClean(x => x
                .SetProject(ProjectDirectory / "RestApia.Shared.csproj")
                .SetConfiguration(Configuration)
            );
        });

    Target Shared_Build => _ => _
        .DependsOn(Shared_Clear)
        .Executes(() =>
        {
            Log.Information("Building project RestApia.Shared with version {Version}", Version);
            DotNetBuild(x => x
                .SetProjectFile(ProjectDirectory / "RestApia.Shared.csproj")
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(Version)
                .SetFileVersion(Version)
                .SetVersion(Version)
                .SetInformationalVersion(Version)
            );
        });

    Target Shared_Push => _ => _
        .DependsOn(Shared_Build)
        .Executes(() =>
        {
            var package = RootDirectory / ".local" / "nugets" / $"RestApia.Shared.{Version}.nupkg";
            if (!File.Exists(package)) throw new FileNotFoundException($"Package {package} not found");

            DotNetNuGetPush(x => x
                .SetTargetPath(package)
                .SetSource(FeedUrl)
                .SetApiKey(NuGetApiKey)
                .SetSkipDuplicate(true)
            );
        });
}
