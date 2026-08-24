using System;
using System.Collections.Generic;
using System.IO;

namespace Flow.Launcher.Plugin.GitEasy.Utilities;

internal static class WindowsFileNameValidator
{
    private const int MaximumComponentLength = 255;

    private static readonly HashSet<string> ReservedDeviceNames = new(
        new[] { "CON", "PRN", "AUX", "NUL" },
        StringComparer.OrdinalIgnoreCase);

    public static bool IsValidLeafName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > MaximumComponentLength
            || value is "." or ".."
            || value.EndsWith(' ')
            || value.EndsWith('.')
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return false;
        }

        int extensionSeparatorIndex = value.IndexOf('.');
        string deviceStem = (extensionSeparatorIndex < 0
                ? value
                : value[..extensionSeparatorIndex])
            .TrimEnd(' ');

        return !IsReservedDeviceName(deviceStem);
    }

    private static bool IsReservedDeviceName(string value)
    {
        if (ReservedDeviceNames.Contains(value))
        {
            return true;
        }

        if (value.Length != 4
            || (!value.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                && !value.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return value[3] is >= '1' and <= '9'
            or '\u00B9'
            or '\u00B2'
            or '\u00B3';
    }
}