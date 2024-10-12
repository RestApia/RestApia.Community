using Nuke.Common;
using Serilog;
namespace Targets;

public class Shared_Targets : NukeBuild
{
    public Target Test => _ => _
        .Executes(() =>
        {
            Log.Information("Test");
        });

    public Target Test2 => _ => _
        .Executes(() =>
        {
            Log.Information("Test 2");
        });
}
