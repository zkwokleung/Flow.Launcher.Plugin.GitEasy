using Flow.Launcher.Plugin.GitEasy.Models.Commands.Results;
using System;

namespace Flow.Launcher.Plugin.GitEasy.Utilities;

public static class CommandErrorFormatter
{
    private const int MaxDiagnosticLength = 1000;

    public static string GetGitFailureDetails(GitCommandResult result, string exitCodeMessageFormat)
    {
        string details = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;
        details = NormalizeDiagnostic(details);

        return string.IsNullOrWhiteSpace(details)
            ? string.Format(exitCodeMessageFormat, result.ExitCode)
            : details;
    }

    public static string FormatWithDetails(string message, string details)
    {
        details = NormalizeDiagnostic(details);
        return string.IsNullOrWhiteSpace(details)
            ? message
            : $"{message}{Environment.NewLine}{details}";
    }

    public static string NormalizeDiagnostic(string details)
    {
        details = (details ?? string.Empty).Trim();
        return details.Length > MaxDiagnosticLength
            ? $"…{details[^(MaxDiagnosticLength - 1)..]}"
            : details;
    }
}
