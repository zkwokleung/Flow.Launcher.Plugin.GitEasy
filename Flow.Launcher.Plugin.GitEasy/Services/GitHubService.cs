using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.GitEasy.Services
{
    public static class GitHubService
    {
        private static GitHubClient _client;

        public static void Init()
        {
            _client = new GitHubClient(new ProductHeaderValue("Flow.Launcher.Plugin.GitEasy"));
        }

        public static async Task<User> GetUser(string username)
        {
            return await _client.User.Get(username);
        }

        public static async Task<Repository> GetRepos(long reposId)
        {
            return await _client.Repository.Get(reposId);
        }

        public static async Task<Repository> GetRepos(string username, string reposName)
        {
            return await _client.Repository.Get(username, reposName);
        }
    }
}
