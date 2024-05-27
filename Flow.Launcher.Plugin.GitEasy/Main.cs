using Flow.Launcher.Plugin.GitEasy.Commands;
using Flow.Launcher.Plugin.GitEasy.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.GitEasy
{
    public partial class Main : ISettingProvider, IAsyncPlugin, IPluginI18n, ISavable
    {
        private static PluginInitContext s_context { get; set; }
        private static Settings s_settings { get; set; }
        private static GitCommandRunner s_gitCommandRunner { get; set; }

        static Main()
        {
        }

        #region Flow.Launcher Interface Functions
        public async Task InitAsync(PluginInitContext context)
        {
            s_context = context;

            s_settings = s_context.API.LoadSettingJsonStorage<Settings>();

            s_gitCommandRunner = new GitCommandRunner(s_settings);
        }

        public void Save()
        {

        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            return query.FirstSearch.ToLower() switch
            {
                Settings.CLONE_COMMAND => PrepareGitCloneResults(query.SecondToEndSearch),
                _ => new()
            };
        }

        public string GetTranslatedPluginTitle()
        {
            return s_context.API.GetTranslation("giteasy_plugin_title");
        }

        public string GetTranslatedPluginDescription()
        {
            return s_context.API.GetTranslation("giteasy_plugin_description");
        }

        public static void StartProcess(Func<ProcessStartInfo, Process> runProcess, ProcessStartInfo info)
        {

        }

        public Control CreateSettingPanel()
        {
            return new SettingsPanel(s_context, s_settings);
        }
        #endregion

        #region Public Functions
        public List<Result> PrepareGitCloneResults(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new();
            }

            List<string> terms = query.Split(' ').ToList();

            // Use regex to indentify the repo for the flexibility
            List<string> repos = terms.Where(t => ReposRegex().IsMatch(t)).ToList();

            if (repos.Count < 1)
            {
                return new()
                {
                    new Result{
                        Title= s_context.API.GetTranslation("giteasy_query_result_clone_no_repos"),
                        IcoPath = "Images\\icon.png",
                        Action = a => { return true; }
                    }
                };
            }

            string firstRepos = repos.First();
            string options = string.Join(" ", terms.Remove(firstRepos));

            // Extract the repos folder name from the repos url
            string location = ExtractRepoName(firstRepos);

            return new()
            {
                new Result{
                    Title= $"{s_context.API.GetTranslation("giteasy_query_result_clone")} {location}",
                    SubTitle =  string.Format(s_context.API.GetTranslation("giteasy_query_result_clone_msg"), firstRepos, s_settings.ReposPath),
                    IcoPath = "Images\\icon.png",
                    Action = a =>
                    {
                        try
                        {
                            s_gitCommandRunner.CloneRepos(new()
                            {
                                Options = options,
                                DestinationFolder = s_settings.ReposPath,
                                Repo = firstRepos
                            }, () =>
                            {
                                s_context.API.ShowMsg(
                                    s_context.API.GetTranslation("giteasy_query_clone_complete"),
                                    $"{s_context.API.GetTranslation("giteasy_query_clone_complete_msg")} {location}"
                                );

                                switch(s_settings.OpenReposIn)
                                {
                                    case OpenOption.FileExplorer:
                                        SystemCommand.OpenExplorer($"{s_settings.ReposPath}\\{location}");
                                        break;

                                    case OpenOption.VSCode:
                                        SystemCommand.OpenVsCode($"{s_settings.ReposPath}\\{location}");
                                        break;

                                    default:
                                        break;
                                }
                            });
                        }catch (Exception ex)
                        {
                            s_context.API.ShowMsgError("Error", ex.Message);
                        }

                        // TODO: Open explorer/vscode after execution
                        return true;
                    }
                }
            };
        }
        #endregion

        #region Private Functions
        private string ExtractRepoName(string url)
        {
            // Look for the last index of the last slash /
            int start = url.LastIndexOf('/') + 1;
            return url.Substring(start, url.Length - start - 4);
        }
        #endregion

        [GeneratedRegex("(git@|https:\\/\\/).*.git")]
        private static partial Regex ReposRegex();
    }
}
