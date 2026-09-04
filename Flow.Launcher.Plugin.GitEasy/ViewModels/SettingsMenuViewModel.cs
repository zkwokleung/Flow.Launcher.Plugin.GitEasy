using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Flow.Launcher.Plugin.GitEasy.Models;
using System.Collections.ObjectModel;
using Flow.Launcher.Plugin.GitEasy.Utilities;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Flow.Launcher.Plugin.GitEasy.ViewModels;

public class SettingsMenuViewModel : INotifyPropertyChanged
{
    private Settings _settings;
    private readonly Action _saveSettings;
    private readonly Action<Exception> _handleSaveError;
    private readonly DispatcherTimer _saveTimer;
    private readonly SettingsPathValidationCoordinator _pathValidationCoordinator;
    private readonly ObservableCollection<RepositoryPathRowViewModel> _repositoryPaths;
    private readonly Dictionary<RepositoryPathRowViewModel, string> _committedRepositoryPaths;
    private readonly Dictionary<string, FileSystemPathKind> _repositoryPathProbeResults = new(StringComparer.Ordinal);
    private GitPathProbeCache _gitPathProbeResult;
    private string _gitPath;
    private OpenOption _selectedOpenReposIn;
    private bool _hasGitPathError;

    public SettingsMenuViewModel(
        Settings settings,
        Action saveSettings,
        Action<Exception> handleSaveError)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
        _handleSaveError = handleSaveError ?? throw new ArgumentNullException(nameof(handleSaveError));
        _gitPath = settings.GitPath ?? string.Empty;
        _selectedOpenReposIn = settings.OpenReposIn;

        _repositoryPaths = new ObservableCollection<RepositoryPathRowViewModel>(
            (settings.ReposPaths ?? new List<string>()).Select(path => new RepositoryPathRowViewModel(path)));
        RepositoryPaths = new ReadOnlyObservableCollection<RepositoryPathRowViewModel>(_repositoryPaths);
        _committedRepositoryPaths = _repositoryPaths.ToDictionary(row => row, row => row.Path);

        foreach (RepositoryPathRowViewModel row in _repositoryPaths)
        {
            row.PropertyChanged += OnRepositoryPathChanged;
        }

        _repositoryPaths.CollectionChanged += OnRepositoryPathsCollectionChanged;
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _saveTimer.Tick += OnSaveTimerTick;
        _pathValidationCoordinator = new SettingsPathValidationCoordinator(
            TimeSpan.FromMilliseconds(250),
            CapturePathProbeRequest,
            ApplyPathProbeResult,
            _handleSaveError);

        RevalidateRepositoryPaths();
        ValidateGitPath();
        _pathValidationCoordinator.Schedule(saveAfterValidation: false);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public ReadOnlyObservableCollection<RepositoryPathRowViewModel> RepositoryPaths { get; }

    public IReadOnlyList<OpenOption> OpenReposInOptions { get; } = Enum.GetValues<OpenOption>();

    public string GitPath
    {
        get => _gitPath;
        set
        {
            string nextPath = value ?? string.Empty;
            if (_gitPath == nextPath)
            {
                return;
            }

            _gitPath = nextPath;
            OnPropertyChanged();
            ValidateGitPath();
            _pathValidationCoordinator.Schedule(saveAfterValidation: true);
            ScheduleSave();
        }
    }

    public bool HasGitPathError
    {
        get => _hasGitPathError;
        private set
        {
            if (_hasGitPathError == value)
            {
                return;
            }

            _hasGitPathError = value;
            OnPropertyChanged();
        }
    }

    public OpenOption SelectedOpenReposIn
    {
        get => _selectedOpenReposIn;
        set
        {
            if (_selectedOpenReposIn == value)
            {
                return;
            }

            _selectedOpenReposIn = value;
            OnPropertyChanged();
            CommitChanges();
        }
    }

    public RepositoryPathRowViewModel AddRepositoryPath()
    {
        var row = new RepositoryPathRowViewModel(string.Empty);
        _repositoryPaths.Add(row);
        return row;
    }

    public void RemoveRepositoryPath(RepositoryPathRowViewModel row)
    {
        ArgumentNullException.ThrowIfNull(row);
        _repositoryPaths.Remove(row);
    }

    public void UpdateRepositoryPath(RepositoryPathRowViewModel row, string path)
    {
        ArgumentNullException.ThrowIfNull(row);
        row.Path = path;
        CommitChanges();
    }

    public void MoveRepositoryPathUp(RepositoryPathRowViewModel row)
    {
        MoveRepositoryPath(row, -1);
    }

    public void MoveRepositoryPathDown(RepositoryPathRowViewModel row)
    {
        MoveRepositoryPath(row, 1);
    }

    public async Task FlushPendingChangesAsync()
    {
        _saveTimer.Stop();
        bool latestSnapshotApplied = await _pathValidationCoordinator.FlushAsync();
        _saveTimer.Stop();

        if (latestSnapshotApplied)
        {
            CommitChanges();
        }
    }

    public void CommitChanges()
    {
        _saveTimer.Stop();

        List<string> previousPaths = _settings.ReposPaths.ToList();
        string previousGitPath = _settings.GitPath;
        OpenOption previousOpenOption = _settings.OpenReposIn;
        var nextCommittedPaths = new Dictionary<RepositoryPathRowViewModel, string>();

        _settings.ReposPaths = BuildRepositoryPaths(nextCommittedPaths);

        if (TryGetGitPathCandidate(out string gitPath)
            && _gitPathProbeResult != null
            && string.Equals(_gitPathProbeResult.Path, gitPath, StringComparison.OrdinalIgnoreCase)
            && _gitPathProbeResult.Exists)
        {
            _settings.GitPath = gitPath;
        }

        _settings.OpenReposIn = _selectedOpenReposIn;

        try
        {
            _saveSettings();

            _committedRepositoryPaths.Clear();
            foreach ((RepositoryPathRowViewModel row, string path) in nextCommittedPaths)
            {
                _committedRepositoryPaths.Add(row, path);
            }
        }
        catch (Exception exception)
        {
            _settings.ReposPaths = previousPaths;
            _settings.GitPath = previousGitPath;
            _settings.OpenReposIn = previousOpenOption;
            _handleSaveError(exception);
        }
    }

    private void OnRepositoryPathsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (RepositoryPathRowViewModel row in e.OldItems)
            {
                row.PropertyChanged -= OnRepositoryPathChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (RepositoryPathRowViewModel row in e.NewItems)
            {
                row.PropertyChanged += OnRepositoryPathChanged;
            }
        }

        RevalidateRepositoryPaths();
        _pathValidationCoordinator.Schedule(saveAfterValidation: true);
        CommitChanges();
    }

    private void OnRepositoryPathChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RepositoryPathRowViewModel.Path))
        {
            return;
        }

        RevalidateRepositoryPaths();
        _pathValidationCoordinator.Schedule(saveAfterValidation: true);
        ScheduleSave();
    }

    private void RevalidateRepositoryPaths()
    {
        var normalizedPaths = new Dictionary<RepositoryPathRowViewModel, string>();

        for (int index = 0; index < _repositoryPaths.Count; index++)
        {
            RepositoryPathRowViewModel row = _repositoryPaths[index];
            row.CanMoveUp = index > 0;
            row.CanMoveDown = index < _repositoryPaths.Count - 1;

            if (string.IsNullOrWhiteSpace(row.Path))
            {
                row.ValidationState = RepositoryPathValidationState.Empty;
            }
            else if (!RepositoryPathNormalizer.TryNormalize(row.Path, out string normalizedPath))
            {
                row.ValidationState = RepositoryPathValidationState.Invalid;
            }
            else
            {
                row.ValidationState = TryGetRepositoryPathProbe(normalizedPath, out FileSystemPathKind kind)
                    ? kind switch
                    {
                        FileSystemPathKind.Directory => RepositoryPathValidationState.Valid,
                        FileSystemPathKind.File => RepositoryPathValidationState.File,
                        _ => RepositoryPathValidationState.Unavailable,
                    }
                    : RepositoryPathValidationState.Unavailable;

                if (row.ValidationState != RepositoryPathValidationState.File)
                {
                    normalizedPaths.Add(row, normalizedPath);
                }
            }
        }

        MarkDuplicateRepositoryPaths(normalizedPaths);
    }

    private List<string> BuildRepositoryPaths(
        IDictionary<RepositoryPathRowViewModel, string> nextCommittedPaths)
    {
        var paths = new List<string>();

        foreach (RepositoryPathRowViewModel row in _repositoryPaths)
        {
            string path;
            if (!IsBlockingRepositoryDraft(row)
                && RepositoryPathNormalizer.TryNormalize(row.Path, out string normalizedPath)
                && TryGetRepositoryPathProbe(normalizedPath, out FileSystemPathKind kind)
                && kind != FileSystemPathKind.File)
            {
                path = normalizedPath;
            }
            else if (!_committedRepositoryPaths.TryGetValue(row, out path)
                     || IsCommittedRepositoryPathConfirmedFile(path))
            {
                continue;
            }

            if (paths.Any(existingPath => RepositoryPathNormalizer.AreEquivalent(existingPath, path)))
            {
                continue;
            }

            paths.Add(path);
            nextCommittedPaths.Add(row, path);
        }

        return paths;
    }

    private void MarkDuplicateRepositoryPaths(
        IReadOnlyDictionary<RepositoryPathRowViewModel, string> normalizedPaths)
    {
        Dictionary<RepositoryPathRowViewModel, string> effectivePaths = normalizedPaths
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (RepositoryPathRowViewModel row in _repositoryPaths)
        {
            if (!effectivePaths.ContainsKey(row)
                && _committedRepositoryPaths.TryGetValue(row, out string committedPath))
            {
                effectivePaths.Add(row, committedPath);
            }
        }

        var processedRows = new HashSet<RepositoryPathRowViewModel>();

        foreach (RepositoryPathRowViewModel row in _repositoryPaths)
        {
            if (processedRows.Contains(row) || !effectivePaths.TryGetValue(row, out string path))
            {
                continue;
            }

            List<RepositoryPathRowViewModel> equivalentRows = _repositoryPaths
                .Where(candidate => effectivePaths.TryGetValue(candidate, out string candidatePath)
                                    && RepositoryPathNormalizer.AreEquivalent(path, candidatePath))
                .ToList();

            foreach (RepositoryPathRowViewModel equivalentRow in equivalentRows)
            {
                processedRows.Add(equivalentRow);
            }

            if (equivalentRows.Count < 2)
            {
                continue;
            }

            RepositoryPathRowViewModel preferredRow = equivalentRows.FirstOrDefault(candidate =>
                _committedRepositoryPaths.TryGetValue(candidate, out string committedPath)
                && RepositoryPathNormalizer.AreEquivalent(committedPath, effectivePaths[candidate]))
                ?? equivalentRows[0];

            foreach (RepositoryPathRowViewModel duplicateRow in equivalentRows.Where(candidate => candidate != preferredRow))
            {
                if (normalizedPaths.ContainsKey(duplicateRow))
                {
                    duplicateRow.ValidationState = RepositoryPathValidationState.Duplicate;
                }
            }
        }
    }

    private void ValidateGitPath()
    {
        if (!TryGetGitPathCandidate(out string candidate))
        {
            HasGitPathError = true;
            return;
        }

        HasGitPathError = _gitPathProbeResult != null
                          && string.Equals(_gitPathProbeResult.Path, candidate, StringComparison.OrdinalIgnoreCase)
                          && !_gitPathProbeResult.Exists;
    }

    private bool TryGetGitPathCandidate(out string candidate)
    {
        candidate = _gitPath.Trim();
        return Path.IsPathFullyQualified(candidate)
               && string.Equals(Path.GetFileName(candidate), "git.exe", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetRepositoryPathProbe(
        string normalizedPath,
        out FileSystemPathKind kind)
    {
        return _repositoryPathProbeResults.TryGetValue(normalizedPath, out kind);
    }

    private bool IsCommittedRepositoryPathConfirmedFile(string committedPath)
    {
        return RepositoryPathNormalizer.TryNormalize(committedPath, out string normalizedPath)
               && TryGetRepositoryPathProbe(normalizedPath, out FileSystemPathKind kind)
               && kind == FileSystemPathKind.File;
    }

    private bool HasConfirmedCommittedRepositoryFile()
    {
        return _committedRepositoryPaths.Values.Any(IsCommittedRepositoryPathConfirmedFile);
    }

    private SettingsPathProbeRequest CapturePathProbeRequest()
    {
        string gitPath = TryGetGitPathCandidate(out string gitPathCandidate)
            ? gitPathCandidate
            : string.Empty;
        string[] repositoryPaths = _repositoryPaths
            .Select(row => row.Path)
            .Concat(_committedRepositoryPaths.Values)
            .Select(path => RepositoryPathNormalizer.TryNormalize(path, out string normalizedPath)
                ? normalizedPath
                : null)
            .Where(path => path != null)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new SettingsPathProbeRequest(gitPath, repositoryPaths);
    }

    private void ApplyPathProbeResult(
        SettingsPathProbeResult result,
        bool saveAfterValidation)
    {
        _repositoryPathProbeResults.Clear();

        foreach ((string path, FileSystemPathKind kind) in result.RepositoryPaths)
        {
            _repositoryPathProbeResults.Add(path, kind);
        }

        _gitPathProbeResult = new GitPathProbeCache(result.GitPath, result.GitPathExists);
        RevalidateRepositoryPaths();
        ValidateGitPath();

        if (saveAfterValidation || HasConfirmedCommittedRepositoryFile())
        {
            ScheduleSave();
        }
    }

    private void MoveRepositoryPath(RepositoryPathRowViewModel row, int offset)
    {
        ArgumentNullException.ThrowIfNull(row);
        int currentIndex = _repositoryPaths.IndexOf(row);
        int targetIndex = currentIndex + offset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= _repositoryPaths.Count)
        {
            return;
        }

        _repositoryPaths.Move(currentIndex, targetIndex);
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnSaveTimerTick(object sender, EventArgs e)
    {
        CommitChanges();
    }

    private static bool IsBlockingRepositoryDraft(RepositoryPathRowViewModel row)
    {
        return row.ValidationState is RepositoryPathValidationState.Invalid
            or RepositoryPathValidationState.Empty
            or RepositoryPathValidationState.File
            or RepositoryPathValidationState.Duplicate;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed record GitPathProbeCache(string Path, bool Exists);
}
