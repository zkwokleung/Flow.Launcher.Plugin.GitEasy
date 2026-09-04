using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface ISystemCommandService
{
    Task OpenExplorerAsync(string path);
    Task OpenVsCodeAsync(string path);
    Task OpenCursorAsync(string path);
}
