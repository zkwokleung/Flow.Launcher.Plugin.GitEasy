using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.GitEasy.ViewModels;

public enum RepositoryPathValidationState
{
    Valid,
    Empty,
    Invalid,
    File,
    Duplicate,
    Unavailable,
}

public sealed class RepositoryPathRowViewModel : INotifyPropertyChanged
{
    private string _path;
    private RepositoryPathValidationState _validationState;
    private bool _canMoveUp;
    private bool _canMoveDown;

    public RepositoryPathRowViewModel(string path)
    {
        _path = path ?? string.Empty;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public string Path
    {
        get => _path;
        set
        {
            string nextPath = value ?? string.Empty;
            if (_path == nextPath)
            {
                return;
            }

            _path = nextPath;
            OnPropertyChanged();
        }
    }

    public RepositoryPathValidationState ValidationState
    {
        get => _validationState;
        internal set
        {
            if (_validationState == value)
            {
                return;
            }

            _validationState = value;
            OnPropertyChanged();
        }
    }

    public bool CanMoveUp
    {
        get => _canMoveUp;
        internal set
        {
            if (_canMoveUp == value)
            {
                return;
            }

            _canMoveUp = value;
            OnPropertyChanged();
        }
    }

    public bool CanMoveDown
    {
        get => _canMoveDown;
        internal set
        {
            if (_canMoveDown == value)
            {
                return;
            }

            _canMoveDown = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
