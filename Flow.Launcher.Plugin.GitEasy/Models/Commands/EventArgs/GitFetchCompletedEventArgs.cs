namespace Flow.Launcher.Plugin.GitEasy.Models.Commands.EventArgs;

public class GitFetchCompletedEventArgs : System.EventArgs
{
    public int ExitCode { get; set; }
    public string Output { get; set; }
}
