using Nuke.Common;
using Serilog;
namespace Targets;

public class Default_Targets : NukeBuild
{
    public Target Test => _ => _
        .Executes(() =>
        {
            Log.Information("Test");
        });
}
