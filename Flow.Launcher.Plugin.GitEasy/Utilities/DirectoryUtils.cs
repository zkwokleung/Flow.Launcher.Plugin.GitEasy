using System;

namespace Flow.Launcher.Plugin.GitEasy.Utilities;

public static class DirectoryUtils
{
    public static string ExtractRepositoryNameFromDirectory(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir))
        {
            return "";
        }

        int start = dir.LastIndexOf("\\", StringComparison.Ordinal);

        if (start == -1)
        {
            return dir;
        }

        return dir[(start + 1)..];
    }
}
