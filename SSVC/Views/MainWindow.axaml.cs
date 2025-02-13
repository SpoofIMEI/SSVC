using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CSCore.CoreAudioAPI;
using SSVC.ViewModels;
using System;
using System.Collections.Generic;

namespace SSVC.Views
{
    public partial class MainWindow : Window
    {
        
        public  MainWindowViewModel _viewModelBase;
        public MainWindow()
        {
            _viewModelBase = new MainWindowViewModel();
            this.DataContext = _viewModelBase;

            InitializeComponent();

            seedDevices();
        }

        private void seedDevices()
        {
            foreach (KeyValuePair<int, MMDevice> device in _viewModelBase.Mics)
                MicsDropdown.Items.Add($"{device.Key}: {device.Value.FriendlyName}");
            foreach (KeyValuePair<int, MMDevice> device in _viewModelBase.Speakers)
                SpeakersDropdown.Items.Add($"{device.Key}: {device.Value.FriendlyName}");
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs args)
        {
            if (_viewModelBase.VoiceChangerInstace != null)
            {
                _viewModelBase.VoiceChangerInstace.StopVoiceChanger();
                ActivateButton.Background = Brush.Parse("lime");
                ActivateButtonText.Text = "ACTIVATE";
                _viewModelBase.VoiceChangerInstace = null;
                return;
            }

            var mic = MicsDropdown.SelectedIndex;
            var speaker = SpeakersDropdown.SelectedIndex;
            if (mic == -1)
            {
                Text.Text = "Please select a valid input device";
                return;
            }

            if (speaker == -1)
            {
                Text.Text = "Please select a valid output device"; 
                return;
            }

            Text.Text = string.Empty;
            try
            {
                _viewModelBase.VoiceChangerInstace = new(_viewModelBase.Mics[mic], _viewModelBase.Speakers[speaker], (MainWindowViewModel)this.DataContext);
                _viewModelBase.VoiceChangerInstace.StartVoiceChanger();
            }
            catch (Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
            ActivateButton.Background = Brush.Parse("red");
            ActivateButtonText.Text = "DEACTIVATE";
        }
    }
}