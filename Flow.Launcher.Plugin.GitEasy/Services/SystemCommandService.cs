using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class SystemCommandService : ISystemCommandService
{
    public void OpenExplorer(string path = "", Action OnCompleted = null)
    {
        ProcessStartInfo info = new()
        {
            FileName = "explorer.exe",
            Arguments = Path.GetFullPath(path)
        };

        Process.Start(info).WaitForExit();

        OnCompleted?.Invoke();
    }

    public void OpenVsCode(string path = "", Action OnCompleted = null)
    {
        ProcessStartInfo info = new()
        {
            FileName = "cmd.exe",
            Arguments = $"/c code \"{path}\""
        };

        Process.Start(info).WaitForExit();

        OnCompleted?.Invoke();
    }


}
