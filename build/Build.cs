using System;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Serilog;
using Targets;

class Build : NukeBuild
{
    public static int Main()
    {
        // get build arguments
        var args = Environment.GetCommandLineArgs();
        var group = args
                .Select(x => Regex.Match(x, "^--g:(.*)"))
                .FirstOrDefault(x => x.Success)
                ?.Groups[1]
                .Value
            ?? "Default";

        Log.Information("Group: {Group}", group);

        return group.ToLowerInvariant() switch {
            "shared" => Execute<Shared_Targets>(),
            _ => throw new Exception("Invalid group"),
        };
    }
}
