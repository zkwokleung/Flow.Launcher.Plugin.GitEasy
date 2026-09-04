namespace Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;

public sealed record GitCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
