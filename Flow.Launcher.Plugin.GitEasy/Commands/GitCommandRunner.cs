using Flow.Launcher.Plugin.SharedCommands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Commands
{
    public class GitCommandRunner
    {
        private const string GIT_EXE = "git.exe";

        private Settings m_settings;

        public GitCommandRunner(Settings settings)
        {
            m_settings = settings;
        }

        public void CloneRepos(GitCloneCommandOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Repo))
            {
                throw new ArgumentException("Repo can not be null or empty");
            }

            if (!File.Exists(GetGitExecutable(m_settings.GitPath)))
            {
                throw new Exception("git.exe not found");
            }

            ShellCommand.Execute(Process.Start, PrepareGitCloneProcessStartInfo(options));
        }

        private ProcessStartInfo PrepareGitCloneProcessStartInfo(GitCloneCommandOptions options)
        {
            return new()
            {
                Verb = "",
                WorkingDirectory = m_settings.GitPath,
                FileName = GIT_EXE,
                Arguments = $"clone {options.Options} {options.Repo} {options.Dir}",
                UseShellExecute = true
            };
        }

        private static string GetGitExecutable(string path)
        {
            return $"{path}\\{GIT_EXE}";
        }
    }
}
