// Stop hook: when a turn ends with changed C# files, check formatting and list the files
// that need it. Never blocks (always exit 0). Registered in hooks/hooks.json as:
//   dotnet run --file "${CLAUDE_PLUGIN_ROOT}/hooks/format-on-stop.cs"
using System.Diagnostics;

var changed = await Run("git", "diff", "--name-only", "HEAD");
if (changed.ExitCode != 0 ||
    !changed.Output.Split('\n').Any(line => line.Trim().EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
{
    return 0;
}

var format = await Run("dotnet", "format", "--verify-no-changes", "--no-restore");
if (format.ExitCode != 0)
{
    Console.WriteLine("dotnet format found style problems in this session's changes. Run 'dotnet format' or ask Claude to fix them:");
    foreach (var line in format.Output.Split('\n').Where(l => l.Contains(".cs")).Take(10))
    {
        Console.WriteLine(line.Trim());
    }
}
return 0;

static async Task<(int ExitCode, string Output)> Run(string fileName, params string[] args)
{
    var psi = new ProcessStartInfo(fileName)
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    foreach (var arg in args)
    {
        psi.ArgumentList.Add(arg);
    }

    try
    {
        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await stdout + await stderr);
    }
    catch (Exception ex)
    {
        // git or dotnet missing: skip the check rather than fail the turn.
        return (-1, ex.Message);
    }
}
