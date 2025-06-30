using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Flow.Launcher.Plugin.GitEasy.Models;
using System.Collections.ObjectModel;

namespace Flow.Launcher.Plugin.GitEasy.ViewModels;

public class SettingsMenuViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private Settings _settings;

    private ObservableCollection<string> _reposPaths;

    /// <summary>
    /// List of repository root folders. Two-way bound to the UI. Any change is immediately reflected
    /// back to the underlying <see cref="Settings"/> instance for persistence.
    /// </summary>
    public ObservableCollection<string> ReposPaths
    {
        get => _reposPaths;
        private set
        {
            if (_reposPaths != null)
            {
                _reposPaths.CollectionChanged -= ReposPaths_CollectionChanged;
            }

            _reposPaths = value;

            if (_reposPaths != null)
            {
                _reposPaths.CollectionChanged += ReposPaths_CollectionChanged;
            }

            OnPropertyChanged();
        }
    }

    private void ReposPaths_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _settings.ReposPaths = _reposPaths.ToList();

        // Keep legacy single path in sync (first item or empty)
        _settings.ReposPath = _reposPaths.FirstOrDefault() ?? string.Empty;
    }

    public void AddRepoPath(string path = "")
    {
        ReposPaths.Add(path);
    }

    public void RemoveRepoPath(string path)
    {
        if (ReposPaths.Contains(path))
        {
            ReposPaths.Remove(path);
        }
    }

    public void UpdateRepoPath(string oldPath, string newPath)
    {
        int index = ReposPaths.IndexOf(oldPath);
        if (index >= 0)
        {
            ReposPaths[index] = newPath;
        }
    }

    public string GitPath
    {
        get => _settings.GitPath;
        set
        {
            _settings.GitPath = value;
            OnPropertyChanged();
        }
    }

    public string[] OpenReposInOptions = Enum.GetValues(typeof(OpenOption)).Cast<OpenOption>().Select(v => v.ToString()).ToArray();

    public int SelectedOpenReposInIndex
    {
        get => (int)_settings.OpenReposIn;
        set
        {
            _settings.OpenReposIn = (OpenOption)value;
            OnPropertyChanged();
        }
    }

    public SettingsMenuViewModel(Settings settings)
    {
        _settings = settings;

        // Initialise collection and hook change event so we can keep Settings in sync.
        ReposPaths = new ObservableCollection<string>(_settings.ReposPaths);
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
