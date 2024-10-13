using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
namespace Targets;

[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
public class Extensions_Targets : NukeBuild
{
    [Required]
    [Parameter("Extension manifest")]
    string ManifestFile = EnvironmentInfo.GetVariable<string>("manifestFile");

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    AbsolutePath ManifestPath => RootDirectory / "manifests" / ManifestFile;
    AbsolutePath TempPath => RootDirectory / ".local" / "temp" / _manifest.Name;

    ManifestModel _manifest;
    AbsolutePath _repoPath;
    AbsolutePath _buildPath;
    AbsolutePath _packagePath;

    Target Extensions_Clean => _ => _
        .Executes(() =>
        {
            if (Directory.Exists(TempPath)) Directory.Delete(TempPath, true);
        });

    Target Extensions_ReadManifest => _ => _
        .Executes(() =>
        {
            var manifestPath = ManifestPath;
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException($"Manifest {manifestPath} not found");

            var json = File.ReadAllText(manifestPath);
            _manifest = JsonConvert.DeserializeObject<ManifestModel>(json)
                ?? throw new InvalidOperationException("Failed to deserialize manifest");
        });

    Target Extensions_FetchSource => _ => _
        .DependsOn(Extensions_ReadManifest, Extensions_Clean)
        .Executes(() =>
        {
            _repoPath = TempPath / "repo";
            var branch = _manifest.Source.Branch;
            var cloneUrl = _manifest.Source.Repository;
            var targetPath = _repoPath;

            ProcessTasks
                .StartProcess("git", $"clone --branch {branch} {cloneUrl} {targetPath}", logOutput: false)
                .AssertZeroExitCode();
        });

    Target Extensions_Build => _ => _
        .DependsOn(Extensions_FetchSource)
        .Executes(() =>
        {
            // build extension
            var projectFile = _repoPath / _manifest.Source.Project;
            _buildPath = TempPath / "build";

            DotNetTasks.DotNetBuild(x => x
                .SetProjectFile(projectFile)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(_manifest.Version)
                .SetFileVersion(_manifest.Version)
                .SetVersion(_manifest.Version)
                .SetInformationalVersion(_manifest.Version)
                .SetOutputDirectory(_buildPath)
            );
        });

    Target Extensions_Sign => _ => _
        .DependsOn(Extensions_Build)
        .Executes(() =>
        {
            // var certPath = RootDirectory / ".local" / "certs" / "RestApia-Extensions-Community.pfx";
            // var password = this.LoadLocalSecret<string>("certPasswordCommunity");

            var certPath = RootDirectory / ".local" / "certs" / "RestApia-Extensions-Official.pfx";
            var password = this.LoadLocalSecret<string>("certPasswordOfficial");

            if (!File.Exists(certPath)) throw new FileNotFoundException($"Certificate {certPath} not found");
            if (password.IsEmpty()) throw new InvalidOperationException("Certificate password not found");

            var assemblyPath = _buildPath / $"{_manifest.Name}.dll";
            if (!File.Exists(assemblyPath)) throw new FileNotFoundException($"Assembly {assemblyPath} not found");

            // signtool sign /fd SHA256 /f "$CertPath" /p "$Password" /t http://timestamp.digicert.com "$DLLPath"
            ProcessTasks
                .StartProcess("signtool", $"sign /fd SHA256 /f \"{certPath}\" /p \"{password}\" /t http://timestamp.digicert.com \"{assemblyPath}\"")
                .AssertZeroExitCode();
        });

    Target Extensions_Pack => _ => _
        .DependsOn(Extensions_Sign)
        .Executes(() =>
        {
            _packagePath = TempPath / "packages";
            Directory.CreateDirectory(_packagePath);

            var projectFile = _repoPath / _manifest.Source.Project;
            DotNetTasks.DotNetPack(x => x
                    .SetProject(projectFile)
                    .SetConfiguration(Configuration)
                    .SetNoBuild(true)
                    .SetProperty("OutputPath", _buildPath) // Use the existing build output
                    .SetAssemblyVersion(_manifest.Version)
                    .SetFileVersion(_manifest.Version)
                    .SetVersion(_manifest.Version)
                    .SetInformationalVersion(_manifest.Version)
                    .SetOutputDirectory(_packagePath)
                    .SetProperty("RepositoryUrl", "https://github.com/RestApia/RestApia.Community") // Set the repository URL
            );
        });

    Target Extensions_Push => _ => _
        .DependsOn(Extensions_Pack)
        .Executes(() =>
        {
            // push nuget to github packages
            var packageFile = _packagePath / $"{_manifest.Name}.{_manifest.Version}.nupkg";
            if (!File.Exists(packageFile)) throw new FileNotFoundException($"Package {packageFile} not found");

            var apiKey = this.LoadLocalSecret<string>("nuget_pat");
            if (apiKey.IsEmpty()) throw new InvalidOperationException("NuGet API key not found");

            DotNetTasks.DotNetNuGetPush(x => x
                .SetTargetPath(packageFile)
                .SetSource("https://nuget.pkg.github.com/RestApia/index.json")
                .SetApiKey(apiKey)
            );
        });

    private record ManifestModel
    {
        public required string Name { get; init; }
        public required string Version { get; init; }
        public required ManifestSource Source { get; init; }
    }

    private record ManifestSource
    {
        public required string Repository { get; init; }
        public required string Branch { get; init; }
        public required string Project { get; init; }
    }
}
