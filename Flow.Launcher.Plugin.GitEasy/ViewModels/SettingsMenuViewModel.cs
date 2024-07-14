using System;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Flow.Launcher.Plugin.GitEasy.Models;

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

        public string GitPath
        {
            get => m_settings.GitPath;
            set
            {
                m_settings.GitPath = value;
                OnPropertyChanged();
            }
        }

        public string[] OpenReposInOptions = Enum.GetValues(typeof(OpenOption)).Cast<OpenOption>().Select(v => v.ToString()).ToArray();

        public int SelectedOpenReposInIndex
        {
            get => (int)m_settings.OpenReposIn;
            set
            {
                m_settings.OpenReposIn = (OpenOption)value;
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
