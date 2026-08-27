<div align="center">
    <img src="https://github.com/zkwokleung/Flow.Launcher.Plugin.GitEasy/blob/main/Flow.Launcher.Plugin.GitEasy/Images/icon.png?raw=true" alt="Git Easy logo" width="75">
    <h1>Git Easy<br>Fast repository access from Flow Launcher</h1>
</div>

Git Easy is a Windows plugin for [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher) that clones, finds, opens, and fetches Git repositories without leaving the launcher.

Requires Flow Launcher 2.0.0 or later and Windows 10 or later.

## Features

- Clone repository into any configured repository root.
- Search repositories across multiple roots with fuzzy matching.
- Open repositories in File Explorer, Visual Studio Code, or Cursor.
- Fetch an existing repository with one action.

## Usage

| Command | Description | Example |
| --- | --- | --- |
| `ge` | Show available commands. | `ge` |
| `ge clone <url> [options]` | Show clone actions for every configured repository root. | `ge clone -b develop https://github.com/owner/project.git` |
| `ge open <name>` | Fuzzy-search and open a repository. | `ge open project` |
| `ge fetch <name>` | Fuzzy-search and fetch a repository. | `ge fetch project` |

Clone accepts HTTPS URLs such as `https://github.com/owner/project.git` and SCP-style SSH URLs such as `git@github.com:owner/project.git`.

Supported clone options:

| Option | Value | Purpose |
| --- | --- | --- |
| `-b`, `--branch` | Branch or tag name | Check out a specific branch or tag. |
| `--depth` | Positive integer | Create a shallow clone. |
| `--recurse-submodules` | None | Initialize submodules recursively. |
| `--single-branch` | None | Clone only the selected branch's history. |

Each repository root gets its own clone result, in the same order as the settings list. Additional results let you override the post-clone action with File Explorer, Visual Studio Code, or Cursor.

## Settings

| Setting | Description |
| --- | --- |
| Repository Paths | Ordered folders containing your repositories. Open and Fetch search every configured root; Clone shows an explicit action for each root. |
| Open Repository In | Default application for Open and the default post-clone action: `None`, `FileExplorer`, `VSCode`, or `Cursor`. For Clone, `None` performs no post-clone open. For Open, `None` falls back to File Explorer. |
| Git Path | Path to `git.exe`. Git Easy discovers Git from `PATH` and common Git for Windows locations; use this setting to override it. |

## Development

Prerequisites:

- Windows 10 or later
- .NET 9 SDK
- Git for Windows

From the repository root:

```powershell
dotnet restore Flow.Launcher.Plugin.GitEasy/Flow.Launcher.Plugin.GitEasy.csproj -r win-x64
dotnet format Flow.Launcher.Plugin.GitEasy/Flow.Launcher.Plugin.GitEasy.csproj whitespace --verify-no-changes --no-restore
dotnet build Flow.Launcher.Plugin.GitEasy/Flow.Launcher.Plugin.GitEasy.csproj -c Release --no-restore
dotnet publish Flow.Launcher.Plugin.GitEasy/Flow.Launcher.Plugin.GitEasy.csproj -c Release -r win-x64 --no-self-contained --no-restore
```

This started as a very personal project. You are very welcomed to contribute and adding your own ideas.