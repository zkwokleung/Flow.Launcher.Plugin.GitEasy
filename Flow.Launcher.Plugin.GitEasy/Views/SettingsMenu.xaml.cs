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
    private readonly PluginInitContext _context;
    private readonly SettingsMenuViewModel _viewModel;

    public SettingsMenu(PluginInitContext context, Settings settings, Action saveSettings)
    {
        InitializeComponent();
        _context = context ?? throw new ArgumentNullException(nameof(context));

        _viewModel = new SettingsMenuViewModel(
            settings,
            saveSettings,
            ShowSaveError);
        DataContext = _viewModel;
    }

    private void OnBtnBrowseReposPathClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: RepositoryPathRowViewModel row })
        {
            return;
        }

        using var dialog = new FolderBrowserDialog();
        if (Directory.Exists(row.Path))
        {
            dialog.SelectedPath = row.Path;
        }

        if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            _viewModel.UpdateRepositoryPath(row, dialog.SelectedPath);
        }
    }

    private void OnBtnAddReposPathClick(object sender, RoutedEventArgs e)
    {
        _viewModel.AddRepositoryPath();
    }

    private void OnBtnRemoveReposPathClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RepositoryPathRowViewModel row })
        {
            _viewModel.RemoveRepositoryPath(row);
        }
    }

    private void OnBtnMoveReposPathUpClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RepositoryPathRowViewModel row })
        {
            _viewModel.MoveRepositoryPathUp(row);
        }
    }

    private void OnBtnMoveReposPathDownClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RepositoryPathRowViewModel row })
        {
            _viewModel.MoveRepositoryPathDown(row);
        }
    }

    private void OnBtnBrowseGitPathClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = "Git executable (git.exe)|git.exe",
        };

        if (File.Exists(_viewModel.GitPath))
        {
            dialog.FileName = _viewModel.GitPath;
        }

        if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
        {
            _viewModel.GitPath = dialog.FileName;
            _viewModel.CommitChanges();
        }
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.FlushPendingChangesAsync();
    }

    private void ShowSaveError(Exception exception)
    {
        _context.API.ShowMsgError(
            _context.API.GetTranslation(Translations.Error),
            exception.Message);
    }
}
