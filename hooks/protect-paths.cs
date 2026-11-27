// PreToolUse hook for Write|Edit: block hand-edits to generated or protected files.
// Migrations go through `dotnet ef migrations add`; lock files and production config are
// not the agent's to edit. Registered in hooks/hooks.json as:
//   dotnet run --file "${CLAUDE_PLUGIN_ROOT}/hooks/protect-paths.cs"
// Exit 2 blocks the call and stderr goes back to Claude as the reason. Exit 0 lets it run.
using System.Text.Json;

(Func<string, bool> Matches, string Reason)[] rules =
[
    (p => p.Contains("/Migrations/"),
        "files under Migrations/ are generated. Use the /ef-migration skill (dotnet ef migrations add) instead of editing migration files or the model snapshot."),
    (p => p.EndsWith("packages.lock.json"),
        "packages.lock.json is generated. Change the .csproj PackageReference and run dotnet restore."),
    (p => p.EndsWith("appsettings.Production.json"),
        "production configuration must not be edited by the agent. Propose the change to the user instead."),
];

try
{
    using var doc = JsonDocument.Parse(await Console.In.ReadToEndAsync());
    var root = doc.RootElement;
    var file = "";
    if (root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty("tool_input", out var toolInput) &&
        toolInput.ValueKind == JsonValueKind.Object &&
        toolInput.TryGetProperty("file_path", out var value) &&
        value.ValueKind == JsonValueKind.String)
    {
        file = value.GetString() ?? "";
    }

    // Compare with forward slashes so Windows paths match the same rules.
    var path = file.Replace('\\', '/');
    foreach (var (matches, reason) in rules)
    {
        if (matches(path))
        {
            await Console.Error.WriteLineAsync($"Blocked: {reason}");
            return 2;
        }
    }

    return 0;
}
catch (Exception ex)
{
    // Fail closed: if the hook can't read the path, it doesn't let the write through.
    await Console.Error.WriteLineAsync($"Blocked: could not check the file path ({ex.GetType().Name}).");
    return 2;
}
