using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace Flow.Launcher.Plugin.GitEasy.Utilities;

public static class RepositoryPathNormalizer
{
    private static readonly HashSet<string> ReservedWindowsNames = new(
        new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        },
        StringComparer.OrdinalIgnoreCase);

    public static bool TryNormalize(string path, out string normalizedPath)
    {
        normalizedPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string trimmedPath = path.Trim();
        if (!Path.IsPathFullyQualified(trimmedPath))
        {
            return false;
        }

        try
        {
            normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmedPath));

            bool isWslPath = TryGetWslPathParts(normalizedPath, out string distribution, out _);
            if ((IsUncPath(normalizedPath) && !HasUncShareComponent(normalizedPath))
                || (isWslPath && HasInvalidWindowsComponent(distribution))
                || (!isWslPath && HasInvalidWindowsPathComponent(normalizedPath)))
            {
                normalizedPath = string.Empty;
                return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
                                          or NotSupportedException
                                          or PathTooLongException
                                          or SecurityException)
        {
            return false;
        }
    }

    public static List<string> NormalizeDistinct(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var normalizedPaths = new List<string>();

        foreach (string path in paths)
        {
            if (TryNormalize(path, out string normalizedPath)
                && !File.Exists(normalizedPath)
                && normalizedPaths.All(existingPath => !AreEquivalent(existingPath, normalizedPath)))
            {
                normalizedPaths.Add(normalizedPath);
            }
        }

        return normalizedPaths;
    }

    public static bool AreEquivalent(string firstPath, string secondPath)
    {
        bool firstIsWslPath = TryGetWslPathParts(firstPath, out string firstDistribution, out string firstLinuxPath);
        bool secondIsWslPath = TryGetWslPathParts(secondPath, out string secondDistribution, out string secondLinuxPath);

        if (firstIsWslPath || secondIsWslPath)
        {
            return firstIsWslPath
                   && secondIsWslPath
                   && string.Equals(firstDistribution, secondDistribution, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(firstLinuxPath, secondLinuxPath, StringComparison.Ordinal);
        }

        return string.Equals(firstPath, secondPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasInvalidWindowsPathComponent(string path)
    {
        string componentsPath;
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            componentsPath = path[2..];
        }
        else
        {
            string root = Path.GetPathRoot(path) ?? string.Empty;
            componentsPath = path[root.Length..];
        }

        return componentsPath.Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries)
            .Any(HasInvalidWindowsComponent);
    }

    private static bool HasInvalidWindowsComponent(string component)
    {
        string deviceName = component.Split('.')[0];

        return component.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
               || component.EndsWith(' ')
               || component.EndsWith('.')
               || ReservedWindowsNames.Contains(deviceName);
    }

    private static bool TryGetWslPathParts(
        string path,
        out string distribution,
        out string linuxPath)
    {
        const string legacyPrefix = @"\\wsl$\";
        const string localhostPrefix = @"\\wsl.localhost\";

        string prefix;
        if (path.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            prefix = legacyPrefix;
        }
        else if (path.StartsWith(localhostPrefix, StringComparison.OrdinalIgnoreCase))
        {
            prefix = localhostPrefix;
        }
        else
        {
            distribution = string.Empty;
            linuxPath = string.Empty;
            return false;
        }

        string remainder = path[prefix.Length..];
        int separatorIndex = remainder.IndexOfAny(new[] { '\\', '/' });
        if (separatorIndex < 0)
        {
            distribution = remainder;
            linuxPath = string.Empty;
        }
        else
        {
            distribution = remainder[..separatorIndex];
            linuxPath = remainder[(separatorIndex + 1)..];
        }

        return distribution.Length > 0;
    }

    private static bool IsUncPath(string path)
    {
        return path.StartsWith(@"\\", StringComparison.Ordinal);
    }

    private static bool HasUncShareComponent(string path)
    {
        string[] components = path[2..].Split(
            new[] { '\\', '/' },
            StringSplitOptions.RemoveEmptyEntries);

        return components.Length >= 2;
    }
}
