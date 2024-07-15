using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class DirectoryService : IDirectoryService
{
    public DirectoryService() { }

    public List<string> GetDirectories(string path)
    {
        return Directory.GetDirectories(path).ToList() ;
    }
}
