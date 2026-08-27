using Flow.Launcher.Plugin.GitEasy.Models;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using Flow.Launcher.Plugin.GitEasy.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;

namespace Flow.Launcher.Plugin.GitEasy.Views;

public partial class SettingsMenu : UserControl
{
    private PluginInitContext _context;
    private readonly SettingsMenuViewModel _settingsPanelViewModel;

    public SettingsMenu(PluginInitContext context, Settings settings, Action saveSettings)
    {
        InitializeComponent();
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _settingsPanelViewModel = new SettingsMenuViewModel(
            settings,
            saveSettings,
            ShowSaveError);
        DataContext = _settingsPanelViewModel;
    }

    private void OnBtnBrowseReposPathClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: RepositoryPathRowViewModel row })
        {
            return;
        }

        using var fbd = new FolderBrowserDialog();
        if (Directory.Exists(row.Path))
        {
            fbd.SelectedPath = row.Path;
        }

        if (fbd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
        {
            _settingsPanelViewModel.UpdateRepositoryPath(row, fbd.SelectedPath);
        }
    }

    private void OnBtnAddReposPathClick(object sender, RoutedEventArgs e)
    {
        _settingsPanelViewModel.AddRepositoryPath();
    }

    private void OnBtnRemoveReposPathClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RepositoryPathRowViewModel row })
        {
            _settingsPanelViewModel.RemoveRepositoryPath(row);
        }
    }

    private void OnBtnMoveReposPathUpClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RepositoryPathRowViewModel row })
        {
            _settingsPanelViewModel.MoveRepositoryPathUp(row);
        }
    }

    private void OnBtnMoveReposPathDownClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RepositoryPathRowViewModel row })
        {
            _settingsPanelViewModel.MoveRepositoryPathDown(row);
        }
    }

    private void OnBtnBrowseGitPathClick(object sender, RoutedEventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = "Git executable (git.exe)|git.exe",
        };

        if (File.Exists(_settingsPanelViewModel.GitPath))
        {
            ofd.FileName = _settingsPanelViewModel.GitPath;
        }

        if (ofd.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(ofd.FileName))
        {
            _settingsPanelViewModel.GitPath = ofd.FileName;
            _settingsPanelViewModel.CommitChanges();
        }
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        await _settingsPanelViewModel.FlushPendingChangesAsync();
    }

    private void ShowSaveError(Exception exception)
    {
        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            exception.Message);
    }
}
