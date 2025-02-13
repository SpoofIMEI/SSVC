using CSCore.CoreAudioAPI;
using SSVC.Audio;
using System.Collections.Generic;
using System.ComponentModel;

namespace SSVC.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public Dictionary<int, MMDevice> Mics { get => Utils.GetMics(); private set; }
        public Dictionary<int, MMDevice> Speakers { get => Utils.GetSpeakers(); private set; }
        public VoiceChanger? VoiceChangerInstace;

        private int _pitch { get; set; }
        public int Pitch
        {
            get => _pitch; 
            set {
                _pitch = value;
                OnPropertyChanged(nameof(Pitch));
            }
        }

        private int _distortion { get; set; }
        public int Distortion
        {
            get => _distortion;
            set
            {
                _distortion = value;
                OnPropertyChanged(nameof(Distortion));
            }
        }

        private int _voiceIncrease { get; set; }
        public int VoiceIncrease
        {
            get => _voiceIncrease;
            set
            {
                _voiceIncrease = value;
                OnPropertyChanged(nameof(VoiceIncrease));
            }
        }

        private bool _checkRemoveBackgroundNoise { get; set; }
        public bool CheckRemoveBackgroundNoise
        {
            get => _checkRemoveBackgroundNoise;
            set
            {
                _checkRemoveBackgroundNoise = value;
                OnPropertyChanged(nameof(CheckRemoveBackgroundNoise));
            }
        }

        private bool _checkOnlyPitch { get; set; }
        public bool CheckOnlyPitch
        {
            get => _checkOnlyPitch;
            set
            {
                _checkOnlyPitch = value;
                OnPropertyChanged(nameof(CheckOnlyPitch));
            }
        }

        private bool _checkOnlyEqualizer { get; set; }
        public bool CheckOnlyEqualizer
        {
            get => _checkOnlyEqualizer;
            set
            {
                _checkOnlyEqualizer = value;
                OnPropertyChanged(nameof(CheckOnlyEqualizer));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
