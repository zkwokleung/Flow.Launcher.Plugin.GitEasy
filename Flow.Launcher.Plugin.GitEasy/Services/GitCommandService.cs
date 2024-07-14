using Flow.Launcher.Plugin.GitEasy.Models;
using Flow.Launcher.Plugin.GitEasy.Models.Commands.Options;
using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace Flow.Launcher.Plugin.GitEasy.Services
{
    public class GitCommandService : IGitCommandService
    {
        private const string GIT_EXE = "git.exe";

        private ISettingsService m_settingService;

        public GitCommandService(ISettingsService settingsService)
        {
            m_settingService = settingsService;
        }

        public void CloneRepos(GitCloneCommandOptions options, Action OnCompleted = null)
        {
            if (string.IsNullOrWhiteSpace(options.Repo))
            {
                throw new ArgumentException("Repo can not be null or empty");
            }

            if (!File.Exists(GetGitExecutable(m_settingService.GetSettingsOrDefault().GitPath)))
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

            if (string.IsNullOrWhiteSpace(options.Options))
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
