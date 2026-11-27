---
name: setup-project
description: Set up a .NET solution to use the dotnet-kit plugin. Copies the CLAUDE.md template, the six .claude/rules files, and the shared permission rules into the solution. Use when the user asks to set up, install, or initialize the .NET kit in a repo, or after installing the dotnet-kit plugin.
disable-model-invocation: true
allowed-tools: Read, Glob, Write, Bash(git status*)
---

# Set up a solution for the dotnet-kit plugin

The plugin gives every session the kit's skills, agents, commands, and hooks. Three things can't
ship inside a plugin and have to live in the solution itself: `CLAUDE.md`, the path-scoped
`.claude/rules/` files, and the `permissions` block of `.claude/settings.json`. This skill copies
them from `templates/` in this skill's folder.

## Steps

1. Find the solution root: the folder with the `.sln` or `.slnx` file. If there are several,
   ask which one. All paths below are relative to it.
2. Check what already exists. Never overwrite a file without showing the user the difference
   and getting a yes:
   - `CLAUDE.md`
   - `.claude/rules/*.md` with the same names as the templates
   - `.claude/settings.json`
3. Copy `templates/CLAUDE.md` to `CLAUDE.md`. Fill in the `[bracketed placeholders]` you can
   read from the solution (solution and project names, target framework). Ask about the one-line
   description; leave any placeholder you can't fill.
4. Copy every file in `templates/rules/` to `.claude/rules/`, keeping the names.
5. Merge `templates/settings.json` into `.claude/settings.json`: add the `permissions.allow` and
   `permissions.deny` entries that are missing and keep everything already there. Don't copy the
   `$comment` key. Don't add hooks; the plugin supplies them.
6. Run `git status` and list what changed. Tell the user to review and commit it, so the rest of
   the team gets the same setup after they install the plugin.

## Gotchas

- Path-scoped rules load when Claude reads a matching file, not when it creates one. A rule meant
  to guide new files has to be unscoped.
- Rules under `~/.claude/rules/` with `paths:` are unreliable. Keep these in the project.
- `CLAUDE.md` in the template expects the `Api / Core / Data / Tests` layout. If the solution is
  shaped differently, update the overview and the dependency line rather than leaving them wrong.
