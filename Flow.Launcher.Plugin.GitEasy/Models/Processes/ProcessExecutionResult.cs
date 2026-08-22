namespace Flow.Launcher.Plugin.GitEasy.Models.Processes;

public sealed record ProcessExecutionResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
