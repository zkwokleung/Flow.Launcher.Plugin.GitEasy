using System;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface ISystemCommandService
{
    public void OpenExplorer(string path = "", Action OnCompleted = null);
    public void OpenVsCode(string path = "", Action OnCompleted = null);
}
