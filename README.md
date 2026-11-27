# .NET Claude Code Kit

A Claude Code plugin for C# / ASP.NET Core solutions: skills, subagents, slash commands, and C# hooks that work together out of the box, plus a setup skill that adds a CLAUDE.md template and path-scoped rules to your solution.

Verified against Claude Code 2.1.332 (November 2026).

## Install

One command, from any terminal:

```bash
claude plugin install dotnet-kit --marketplace cwoodruff/dotnet-claude-code-kit
```

Or inside a session:

```
/plugin marketplace add cwoodruff/dotnet-claude-code-kit
/plugin install dotnet-kit@dotnet-claude-code-kit
```

Then, once per solution, open Claude Code in the solution root (the folder with your `.sln` or `.slnx`) and run:

```
/dotnet-kit:setup-project
```

It copies the three things a plugin can't carry: `CLAUDE.md`, the six `.claude/rules/` files, and the shared permission rules in `.claude/settings.json`. Review the diff and commit it so your team gets the same setup.

**Prerequisites:** the .NET 10 SDK on PATH. The hooks are C# file-based apps run with `dotnet run --file`, so they work the same on macOS, Linux, and Windows with nothing else installed.

To share the plugin with a team, add it to the repo's `.claude/settings.json` so Claude Code offers it to everyone who opens the project:

```json
{
  "extraKnownMarketplaces": {
    "dotnet-claude-code-kit": { "source": { "source": "github", "repo": "cwoodruff/dotnet-claude-code-kit" } }
  },
  "enabledPlugins": { "dotnet-kit@dotnet-claude-code-kit": true }
}
```

## What's inside

```
.claude-plugin/
  plugin.json                  Plugin manifest (dotnet-kit)
  marketplace.json             This repo is also a one-plugin marketplace
skills/                        Procedures Claude applies automatically or via /dotnet-kit:name
  setup-project/                 copies CLAUDE.md, rules, and permissions into a solution
    templates/
      CLAUDE.md                    solution-level instructions template
      settings.json                permission allow/deny rules
      rules/                       csharp-style, aspnet-api, ef-core, testing (path-scoped);
                                   security, solution-structure (always loaded)
  dotnet-review/                 the six .NET failure-mode checks; pairs with built-in /code-review
  ef-migration/                  safe EF Core migration workflow
  add-endpoint/                  convention-following Minimal API endpoint, end to end
  gen-tests/                     xUnit test generation procedure
  nuget-audit/                   vulnerable/deprecated/outdated package audit
  async-hygiene/                 async/await anti-pattern sweep with fixes
  dotnet-upgrade/                phased TFM upgrade (assess, execute, modernize)
  summarize-changes/             diff summary + risk flags
agents/                        Subagents with isolated context
  code-reviewer.md               senior review of recent changes (inherits model)
  test-writer.md                 mechanical test generation (Sonnet)
  security-auditor.md            read-only audit with persistent project memory
  perf-analyzer.md               EF/async/allocation performance analysis
  docs-writer.md                 XML docs, endpoint docs, ADRs (Sonnet)
  migration-planner.md           phased modernization plans with effort estimates
commands/                      Explicit-invocation prompts
  dotnet-security-review.md  conventional-commit.md  pr-description.md  api-docs.md
  coverage-gaps.md           changelog.md            perf-review.md     fix-issue.md
hooks/
  hooks.json                   Registers the three hooks below
  dotnet-post-write.cs         builds after every .cs/.csproj write; errors AND warnings
                               go back to Claude (exit 2) so it fixes them
  protect-paths.cs             blocks hand-edits to Migrations/, lock files, prod config
  format-on-stop.cs            end-of-turn dotnet format check (non-blocking notice)
```

Plugin components are namespaced: the skill `ef-migration` is `/dotnet-kit:ef-migration`. Two commands are named so they don't share a name with Claude Code's built-ins: `/dotnet-kit:dotnet-security-review` (built-in: `/security-review`) and `/dotnet-kit:conventional-commit` (built-in: `commit`).

## How the pieces divide the work

| Mechanism | Loads | Best for | In this kit |
| --- | --- | --- | --- |
| CLAUDE.md | Every session, in full | Facts Claude needs always: build commands, architecture, hard rules | Template, copied by setup-project |
| Rules (`.claude/rules/`) | At launch, or when a matching file is read (`paths:` frontmatter) | Conventions scoped to a file type or layer | 4 path-scoped + 2 global, copied by setup-project |
| Skills | Description always visible; body only when invoked (by you or Claude) | Multi-step procedures; long reference that shouldn't cost context all the time | 9 procedures |
| Commands | Only when you type the command | Fixed-structure prompts you trigger deliberately | 8 workflows |
| Subagents | Own context window, own prompt/tools/model | Verbose or specialized work whose output shouldn't flood your session | 6 specialists |
| Hooks | Deterministically at lifecycle events | Things that must *always* happen, regardless of what the model decides | build gate, path protection, format check |

Rules aren't a plugin component (Claude Code ignores a `rules` field in `plugin.json`), which is why they ship as templates that setup-project copies into the project.

## Try it

```
# In your solution root, after /dotnet-kit:setup-project:
claude

> What did I change?                         # summarize-changes skill triggers
> /dotnet-kit:add-endpoint POST /books/{id}/ratings rate a book
> /dotnet-kit:dotnet-review                  # the six .NET checks
> /code-review                               # Claude Code's built-in review, for comparison
> Use the code-reviewer subagent on my recent changes
> /dotnet-kit:dotnet-security-review
> /dotnet-kit:conventional-commit
```

Then make a deliberate syntax error in a `.cs` file and ask Claude to save it. The PostToolUse hook surfaces the build failure and Claude fixes it unprompted.

## Customizing

- Tighten or loosen `permissions.allow`/`deny` in `.claude/settings.json` first. It's the highest-leverage safety dial.
- Subagent models: `test-writer` and `docs-writer` run on Sonnet 5.5 deliberately; switch `model:` to `opus` or `inherit` if you want more judgment at more cost.
- To change a skill or hook for your team, fork this repo and point `extraKnownMarketplaces` at your fork.

### Choosing a model per agent

Set the `model:` field in each agent's frontmatter. One agent = one model; to run the same role at two capability levels, create two agent files (e.g. `quick-reviewer` on `sonnet`, `deep-reviewer` on `inherit`).

| Task profile | `model:` value | Why | Kit examples |
| --- | --- | --- | --- |
| Mechanical, high-volume (test generation, doc writing, formatting-level edits) | `sonnet` (Sonnet 5.5) | Cheaper and faster than Opus; the procedure in the prompt does the heavy lifting | `test-writer`, `docs-writer` |
| Judgment-heavy (review, audits, performance analysis, planning) | `inherit` | Analysis quality tracks whatever model your session runs | `code-reviewer`, `security-auditor`, `perf-analyzer`, `migration-planner` |
| Reproducibility across a team or CI (identical behavior everywhere, always) | full model string, e.g. `claude-sonnet-5-5` | Aliases float to newer versions; a pinned string never changes under you | (opt in per team) |
| No strong opinion | omit the field | Falls back to the configured default subagent model | |

Rule of thumb: if the agent's prompt is a checklist, Sonnet executes it well; if the agent must notice what the checklist missed, inherit your session's model.

- Personal preferences go in `~/.claude/CLAUDE.md` or `~/.claude/rules/`, not this project's files.
- The `security-auditor` uses `memory: project`. Its accumulated findings live in `.claude/agent-memory/security-auditor/` and can be committed to share institutional knowledge.
