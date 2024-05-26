using Flow.Launcher.Plugin.GitEasy.ViewModels;
using System.Windows;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;

namespace Flow.Launcher.Plugin.GitEasy.Views
{
    /// <summary>
    /// Interaction logic for SettingPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        private PluginInitContext m_context;
        private Settings m_settings;
        private SettingsPanelViewModel m_settingsPanelViewModel => DataContext as SettingsPanelViewModel;

        public SettingsPanel(PluginInitContext context, Settings settings)
        {
            InitializeComponent();
            m_context = context;
            m_settings = settings;

            DataContext = new SettingsPanelViewModel(m_settings);
        }

        private void OnBtnBrowseReposPathClick(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog fbd = new();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                m_settingsPanelViewModel.ReposPath = fbd.SelectedPath;
            }
        }

        private void OnBtnBrowseGitPathClick(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog fbd = new();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                m_settingsPanelViewModel.ReposPath = fbd.SelectedPath;
            }
        }
    }
}
