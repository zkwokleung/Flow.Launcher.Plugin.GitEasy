using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Flow.Launcher.Plugin.GitEasy.ViewModels
{
    public class SettingsPanelViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Settings m_settings;

        public string ReposPath
        {
            get => m_settings.ReposPath;
            set
            {
                m_settings.ReposPath = value;
                OnPropertyChanged();
            }
        }

        public SettingsPanelViewModel(Settings settings)
        {
            this.m_settings = settings;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
