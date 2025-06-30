
<div align="center">
    <img src="https://github.com/zkwokleung/Flow.Launcher.Plugin.GitEasy/blob/main/Flow.Launcher.Plugin.GitEasy/Images/icon.png?raw=true" alt="logo" width="75">
    <h1>Git Easy <br> Easy access to repositories</h1>
    <br>
</div>

## Description

This is a plugin for the [Flow launcher](https://github.com/Flow-Launcher/Flow.Launcher), allows for quickly cloning repositories and open it, as well as opening existing repositories on command.

This started as a very personal project. You are very welcomed to contribute and adding your own ideas.

## Features

* Clone repositories and open in File Explorer/VS Code
* Supports **multiple repository root folders** – keep personal, work, OSS projects separated
* Clone command lets you pick which root folder to clone into (default is the first one)
* Open / Fetch commands search across **all configured repository paths**
* Open existing repositories under any configured folder
* Search and open existing repositories with Fuzzy Search

## Usage

| Command                    | Description                                | Example                                                                   |
|----------------------------|--------------------------------------------|---------------------------------------------------------------------------|
| `` ge ``                   | Show all commands                          |                                                                           |
| `` ge clone <url> ``       | Clone a repository. A separate result appears for each configured root folder so you can choose where to clone. | `` ge clone git@github.com:zkwokleung/Flow.Launcher.Plugin.GitEasy.git `` |
| `` ge open <repo name> ``  | Open a repository in File Explorer/VS Code | `` ge open Flow.Launcher.Plugin.GitEasy ``                                |
| `` ge fetch <repo name> `` | Fetch a repository                         | `` ge fetch Flow.Launcher.Plugin.GitEasy ``                               |

## Settings

| Setting            | Description                                                                                          |
|--------------------|------------------------------------------------------------------------------------------------------|
| Repository Paths   | One or more local folders that contain your repositories. You can add / remove paths in the plugin's settings panel. The **first** path is treated as default when no explicit choice is made. |
| Open Repository In | Specify what to do after cloning the repository. <br> Options: ``None``, ``VSCode``,``FileExplorer`` |
| Git Path           | Specify the path to your git.exe (set to default installation directory)                             |

## TODO

* Run other git commands, i.e., status
* Search for repositories on GitHub