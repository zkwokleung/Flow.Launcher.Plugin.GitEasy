
<div align="center">
    <img src="https://github.com/zkwokleung/Flow.Launcher.Plugin.GitEasy/blob/main/Flow.Launcher.Plugin.GitEasy/Images/icon.png?raw=true" alt="logo" width="75">
    <h1>Git Easy <br> Easy access to repositories</h1>
    <br>
</div>

## Description

This is a plugin for the [Flow launcher](https://github.com/Flow-Launcher/Flow.Launcher), allows for quickly cloning repositories and open it, as well as opening existing repositories on command.

## Features

* Clone repositories and open in File Explorer/VS Code
* Open exisitng repositories under your project folder
* Search and open existing repositories with Fuzzy Search

## Usage

| Command                   | Description                                | Example                                                                   |
|---------------------------|--------------------------------------------|---------------------------------------------------------------------------|
| `` ge ``                  | Show all commands                          |                                                                           |
| `` ge clone <url> ``      | Clone a repository into the project folder | `` ge clone git@github.com:zkwokleung/Flow.Launcher.Plugin.GitEasy.git `` |
| `` ge open <repo name> `` | Open a repository in File Explorer/VS Code | `` ge open giteasy ``                                                     |

## Settings

| Setting            | Description                                                                                          |
|--------------------|------------------------------------------------------------------------------------------------------|
| Repositories Path  | The local project folder, containing all the repositories.                                           |
| Open Repository In | Specify what to do after cloning the repository. <br> Options: ``None``, ``VSCode``,``FileExplorer`` |

## TODO

* Run other git commands, i.e., status
* Search for repositories on GitHub