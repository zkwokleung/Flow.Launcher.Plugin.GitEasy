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

        public void CloneRepos(GitCloneCommandOptions options, Action OnCompleted = null)
        {
            if (string.IsNullOrWhiteSpace(options.Repo))
            {
                throw new ArgumentException("Repo can not be null or empty");
            }

            if (!File.Exists(GetGitExecutable(m_settings.GitPath)))
            {
                throw new Exception("git.exe not found");
            }

            Process.Start(PrepareGitCloneProcessStartInfo(options)).WaitForExit();

            OnCompleted?.Invoke();
        }

        private ProcessStartInfo PrepareGitCloneProcessStartInfo(GitCloneCommandOptions options)
        {
            ProcessStartInfo info = new()
            {
                FileName = "git.exe",
                WorkingDirectory = options.DestinationFolder
            };

            info.ArgumentList.Add("clone");

            if(string.IsNullOrWhiteSpace(options.Options))
            {
                info.ArgumentList.Add(options.Options);
            }

            info.ArgumentList.Add(options.Repo);

            return info;
        }

        private static string GetGitExecutable(string path)
        {
            return $"{path}\\{GIT_EXE}";
        }
    }
}
