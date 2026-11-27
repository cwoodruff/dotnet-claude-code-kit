---
name: dotnet-review
description: Review C# / ASP.NET Core changes for the six ways AI-generated .NET code usually fails (leaked EF entities, sync-over-async, missing validation, concatenated SQL, swallowed exceptions, invented APIs). Use when the user asks for a .NET review of their changes or a branch. Reports only; does not edit.
allowed-tools: Read, Grep, Glob, Bash(git diff*), Bash(git status*), Bash(git log*), Bash(dotnet build*)
---

# .NET review: the six failure modes

Scope: `$ARGUMENTS` if given (a path, or a branch to diff against). Otherwise the uncommitted
changes, and if the tree is clean, the current branch against `main`.

This is the domain-specific companion to the built-in `/code-review`, which looks for general
correctness bugs. Run both; they catch different things.

## Checks

For each changed `.cs` file, look for:

1. **Leaked EF entities.** An endpoint or service that returns or accepts a `Data`/`Core` entity
   instead of a `record` DTO. Watch for navigation properties reaching the client and
   over-posting on writes.
2. **Sync-over-async.** `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `async void` outside
   event handlers, and public async methods that don't take or don't pass a `CancellationToken`.
3. **Missing input validation.** Request models, route values, or query strings that reach the
   data layer unchecked. A 400 path must exist where input can be wrong, after any 404 check.
4. **Concatenated SQL.** String building or interpolation into `FromSqlRaw`, `ExecuteSqlRaw`, or
   ADO.NET commands. `FromSqlInterpolated` and LINQ are fine.
5. **Swallowed exceptions.** Empty `catch`, `catch` that only returns a default, or a `catch` that
   loses the exception without logging it through `ILogger<T>`.
6. **Invented or outdated APIs.** Methods or packages that don't exist in the referenced
   versions. Run `dotnet build` once; compile errors and obsolete warnings are evidence. For a
   suspicious package, check the `.csproj` version rather than guessing.

## Output

| # | Failure mode | Location (file:line) | What's wrong | Fix |
| --- | --- | --- | --- | --- |

Then list the failure modes that came back clean. Don't edit files.

## Gotchas

- Only report what the diff introduces or touches. Pre-existing problems go in a short
  "already there" list at the end, so they don't drown the new ones.
- A DTO that wraps an entity in a property (`record BookDto(Book Book)`) is still a leak.
- Minimal API handlers get a `CancellationToken` by declaring the parameter. Missing it on a
  handler that calls async EF Core is a finding.
