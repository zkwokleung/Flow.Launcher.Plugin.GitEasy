using System.Collections.Generic;

namespace Flow.Launcher.Plugin.GitEasy.Models;

public enum OpenOption
{
    None = 0,
    VSCode,
    FileExplorer,
}

public class Settings
{
    public string ReposPath
    {
        get => ReposPaths.Count > 0 ? ReposPaths[0] : _reposPathFallback;
        set
        {
            _reposPathFallback = value;
            if (ReposPaths.Count == 0)
            {
                ReposPaths.Add(value);
            }
            else
            {
                ReposPaths[0] = value;
            }
        }
    }

    private string _reposPathFallback = "C:\\Repos";

    public string GitPath { get; set; } = "C:\\Program Files\\Git\\bin\\git.exe";
    public OpenOption OpenReposIn { get; set; } = OpenOption.None;
    public List<string> ReposPaths { get; set; } = new();
}
