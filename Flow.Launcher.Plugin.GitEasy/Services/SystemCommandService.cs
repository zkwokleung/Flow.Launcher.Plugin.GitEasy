using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        OpenEditor(path, "code", OnCompleted);
    }

    public void OpenCursor(string path = "", Action OnCompleted = null)
    {
        OpenEditor(path, "cursor", OnCompleted);
    }

    private static void OpenEditor(string path, string editorCommand, Action OnCompleted)
    {
        ProcessStartInfo info;

        if (path.StartsWith(@"\\wsl.localhost\", StringComparison.OrdinalIgnoreCase))
        {
            var withoutPrefix = path.Substring(@"\\wsl.localhost\".Length);
            var parts = withoutPrefix.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var distro = parts[0];
            var linuxPath = "/" + string.Join('/', parts.Skip(1));

            info = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = $"-d {distro} -- {editorCommand} \"{linuxPath}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
        }
        else
        {
            info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {editorCommand} \"{path}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
        }

        Process.Start(info).WaitForExit();

        OnCompleted?.Invoke();
    }
}
