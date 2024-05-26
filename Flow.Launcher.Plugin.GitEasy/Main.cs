using Flow.Launcher.Plugin.GitEasy.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Flow.Launcher.Plugin.GitEasy
{
    public class Main : ISettingProvider, IAsyncPlugin, IPluginI18n, ISavable
    {
        private static PluginInitContext s_context { get; set; }
        private static Settings s_settings { get; set; }

        static Main()
        {
        }

        public async Task InitAsync(PluginInitContext context)
        {
            s_context = context;

            s_settings = s_context.API.LoadSettingJsonStorage<Settings>();
        }

        public void Save()
        {

        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            return null;
        }

        public string GetTranslatedPluginTitle()
        {
            return s_context.API.GetTranslation("flowlauncher_plugin_giteasy_plugin_title");
        }

        public string GetTranslatedPluginDescription()
        {
            return s_context.API.GetTranslation("flowlauncher_plugin_giteasy_plugin_description");
        }

        public static void StartProcess(Func<ProcessStartInfo, Process> runProcess, ProcessStartInfo info)
        {

        }

        public Control CreateSettingPanel()
        {
            return new SettingsPanel(s_context, s_settings);
        }
    }
}
