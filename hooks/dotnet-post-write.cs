// PostToolUse hook for Write|Edit: build after any change that affects compilation and feed
// errors and warnings back to Claude so it fixes them. Registered in hooks/hooks.json as:
//   cd "${CLAUDE_PLUGIN_ROOT}/hooks" && dotnet run --file dotnet-post-write.cs
// It starts from the plugin folder so hooks/global.json picks SDK 10 even when the solution pins an
// older SDK; build and format subprocesses run back in the project folder.
// Exit 2 sends stderr back to Claude. Warnings count as failures, as the kit's CLAUDE.md says.
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

string[] buildInputs = [".cs", ".csproj", ".sln", ".slnx", ".props", ".targets", ".razor"];

// The launcher runs in the plugin folder; build in the project, under the project's own SDK choice.
var projectDir = Environment.GetEnvironmentVariable("CLAUDE_PROJECT_DIR") ?? Directory.GetCurrentDirectory();

string file;
try
{
    using var doc = JsonDocument.Parse(await Console.In.ReadToEndAsync());
    var root = doc.RootElement;
    file = root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty("tool_input", out var toolInput) &&
        toolInput.ValueKind == JsonValueKind.Object &&
        toolInput.TryGetProperty("file_path", out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
catch (JsonException)
{
    // Not a tool call we understand: nothing to build.
    return 0;
}

if (!buildInputs.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
{
    return 0;
}

var psi = new ProcessStartInfo("dotnet")
{
    WorkingDirectory = projectDir,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
};
foreach (var arg in new[] { "build", "--nologo", "--no-restore", "-v", "q" })
{
    psi.ArgumentList.Add(arg);
}

using var build = Process.Start(psi)!;
var stdout = build.StandardOutput.ReadToEndAsync();
var stderr = build.StandardError.ReadToEndAsync();
await build.WaitForExitAsync();
var output = await stdout + await stderr;

// MSBuild diagnostics look like "path(line,col): error CS1002: ...", "...: warning xUnit1031: ..."
// or, with no code, "...: warning : ...". Each one can be printed
// twice (once per project and once in the summary), so keep the distinct lines.
var diagnostics = output.Split('\n')
    .Select(line => line.Trim())
    .Where(line => Regex.IsMatch(line, @": (error|warning)( [A-Za-z]+\d+)? *:", RegexOptions.None, TimeSpan.FromSeconds(1)))
    .Distinct()
    .ToList();
var warnings = diagnostics.Where(line => line.Contains(": warning ")).ToList();

if (build.ExitCode != 0)
{
    await Console.Error.WriteLineAsync($"BUILD FAILED after editing {file}:");
    foreach (var line in diagnostics.Take(30))
    {
        await Console.Error.WriteLineAsync(line);
    }
    return 2;
}

if (warnings.Count > 0)
{
    await Console.Error.WriteLineAsync($"Build succeeded with {warnings.Count} warning(s) after editing {file}:");
    foreach (var line in warnings.Take(15))
    {
        await Console.Error.WriteLineAsync(line);
    }
    return 2;
}

return 0;
