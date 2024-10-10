using Flow.Launcher.Plugin.GitEasy.Models;
using Flow.Launcher.Plugin.GitEasy.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using UserControl = System.Windows.Controls.UserControl;

namespace Flow.Launcher.Plugin.GitEasy.Views;

/// <summary>
/// Interaction logic for SettingPanel.xaml
/// </summary>
public partial class SettingsMenu : UserControl
{
    private PluginInitContext _context;
    private Settings _settings;
    private SettingsMenuViewModel _settingsPanelViewModel => DataContext as SettingsMenuViewModel;

    public SettingsMenu(PluginInitContext context, Settings settings)
    {
        InitializeComponent();
        _context = context;
        _settings = settings;

        DataContext = new SettingsMenuViewModel(_settings);

        cbOpenReposIn.ItemsSource = Enum.GetValues(typeof(OpenOption)).Cast<OpenOption>();
    }

    private void OnBtnBrowseReposPathClick(object sender, RoutedEventArgs e)
    {
        using FolderBrowserDialog fbd = new();
        DialogResult result = fbd.ShowDialog();

        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            _settingsPanelViewModel.ReposPath = fbd.SelectedPath;
        }
    }

    private void OnBtnBrowseGitPathClick(object sender, RoutedEventArgs e)
    {
        const string filter = "git.exe | git.exe";
        var ofd = new OpenFileDialog { Filter = filter };
        var result = ofd.ShowDialog();

        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName))
        {
            _settingsPanelViewModel.GitPath = ofd.FileName;
        }
    }
}
