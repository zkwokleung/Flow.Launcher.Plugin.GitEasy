using Flow.Launcher.Plugin.GitEasy.Services.Interfaces;
using Octokit;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services;

public class GitHubService : IGitHubService
{
    private GitHubClient _client;

    public void Init()
    {
        _client = new GitHubClient(new ProductHeaderValue("Flow.Launcher.Plugin.GitEasy"));
    }

    public async Task<User> GetUser(string username)
    {
        return await _client.User.Get(username);
    }

    public async Task<Repository> GetRepo(long reposId)
    {
        return await _client.Repository.Get(reposId);
    }

    public async Task<Repository> GetRepo(string username, string reposName)
    {
        return await _client.Repository.Get(username, reposName);
    }
}
