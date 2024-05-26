using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.GitEasy
{
    public enum OpenOption
    {
        None = 0,
        VSCode,
        FileExplorer,
    }

    public class Settings
    {
        internal const string CLONE_COMMAND = "clone";
        internal const string SEARCH_REPOS_COMMAND = "search";

        public string ReposPath { get; set; } = "C:\\Repos";
        public string GitPath { get; set; } = "C:\\Program Files\\Git\\bin";
        public OpenOption OpenReposIn { get; set; } = OpenOption.None;
    }
}
