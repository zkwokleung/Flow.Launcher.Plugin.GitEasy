using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.GitEasy.Utilities
{
    public static partial class RegexUtils
    {
        [GeneratedRegex("(git@|https:\\/\\/).*.git")]
        public static partial Regex ReposRegex();
    }
}
