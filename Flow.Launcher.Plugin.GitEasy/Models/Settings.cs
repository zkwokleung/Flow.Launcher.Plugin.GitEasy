namespace Flow.Launcher.Plugin.GitEasy.Models;

public enum OpenOption
{
    None = 0,
    VSCode,
    FileExplorer,
}

public class Settings
{
    public string ReposPath { get; set; } = "C:\\Repos";
    public string GitPath { get; set; } = "C:\\Program Files\\Git\\bin\\git.exe";
    public OpenOption OpenReposIn { get; set; } = OpenOption.None;
}
