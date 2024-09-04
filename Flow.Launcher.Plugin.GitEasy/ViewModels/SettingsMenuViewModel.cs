using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Flow.Launcher.Plugin.GitEasy.Models;

namespace Flow.Launcher.Plugin.GitEasy.ViewModels;

public class SettingsMenuViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly Settings _settings;

    #region Properties
    public string ReposPath
    {
        get => _settings.ReposPath;
        set
        {
            _settings.ReposPath = value;
            OnPropertyChanged();
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

    public string ActiveRepo
    {
        get => _settings.ActiveRepo;
        set
        {
            _settings.ActiveRepo = value;
            OnPropertyChanged();
        }
    }
    #endregion

    public SettingsMenuViewModel(Settings settings)
    {
        _settings = settings;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
