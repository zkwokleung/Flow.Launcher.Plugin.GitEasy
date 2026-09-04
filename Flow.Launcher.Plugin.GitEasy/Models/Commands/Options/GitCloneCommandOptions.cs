namespace Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;

using System;
using System.Collections.Generic;

public class GitCloneCommandOptions
{
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();
    public string Repo { get; set; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
}
