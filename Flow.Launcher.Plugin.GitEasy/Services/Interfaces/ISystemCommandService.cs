using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces;

public interface ISystemCommandService
{
    public Task OpenExplorerAsync(string path);
    public Task OpenVsCodeAsync(string path);
    public Task OpenCursorAsync(string path);
}
