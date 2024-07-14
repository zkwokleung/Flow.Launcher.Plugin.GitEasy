using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services.Interfaces
{
    public interface IGitHubService
    {
        Task<User> GetUser(string username);
        Task<Repository> GetRepo(long reposId);
        Task<Repository> GetRepo(string username, string reposName);
    }
}
