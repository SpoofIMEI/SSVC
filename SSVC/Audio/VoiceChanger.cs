using Avalonia.Threading;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using CSCore.Streams;
using CSCore.Streams.Effects;
using SSVC.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace SSVC.Audio
{
    public class VoiceChanger(MMDevice _input, MMDevice _output, MainWindowViewModel viewModel)
    {
        WasapiOut soundOut;
        WasapiCapture soundIn;
        readonly MMDevice input = _input;
        readonly MMDevice output = _output;

        public void StopVoiceChanger()
        {
            soundOut.Stop();
            soundIn.Stop();
        }

        public void StartVoiceChanger() {
            // Stop voice changer when avalonia exits to prevent it from running in the background
            Dispatcher.UIThread.ShutdownStarted += (object? sender, EventArgs e) =>
            {
                StopVoiceChanger();
            };

            // Setup input
            soundIn = new WasapiCapture();
            soundIn.Device = input;
            soundIn.Initialize();
            IWaveSource source = new SoundInSource(soundIn) { FillWithZeros = true };
            var sampleSource = source.ToSampleSource();

            // Audio processing
            List<object> layers = [];
            layers.Add(new CSCore.Streams.Effects.PitchShifter(sampleSource));
            layers.Add(Equalizer.Create10BandEqualizer((ISampleSource)layers[0]));
            layers.Add(Equalizer.Create10BandEqualizer((ISampleSource)layers[1]));
            layers.Add(Equalizer.Create10BandEqualizer((ISampleSource)layers[2]));

            var rnd = new Random();
            var multiplier = 1000000f;
            soundIn.DataAvailable += (s,e) => {
                // Equalizer
                if (!viewModel.CheckOnlyPitch)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ((Equalizer)layers[1]).SampleFilters[i+2].Filters[0].GainDB = (double)(rnd.Next((int)(-viewModel.Distortion/1.25*multiplier), (int)(viewModel.Distortion/1.25*multiplier)) /multiplier);
                        ((Equalizer)layers[1]).SampleFilters[i+2].Filters[1].GainDB = (double)(rnd.Next((int)(-viewModel.Distortion/1.25*multiplier), (int)(viewModel.Distortion/1.25*multiplier)) /multiplier);
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    if (((Equalizer)layers[1]).SampleFilters[i + 1].Filters[0].GainDB < 5)
                        ((Equalizer)layers[2]).SampleFilters[i + 1].Filters[0].GainDB = viewModel.VoiceIncrease;
                    if (((Equalizer)layers[1]).SampleFilters[i + 1].Filters[1].GainDB < 5)
                        ((Equalizer)layers[2]).SampleFilters[i + 1].Filters[1].GainDB = viewModel.VoiceIncrease;
                }

                if (viewModel.CheckRemoveBackgroundNoise)
                {

                    ((Equalizer)layers[3]).SampleFilters[9].Filters[0].GainDB = -50;
                    ((Equalizer)layers[3]).SampleFilters[9].Filters[1].GainDB = -50;
                    
                    for (int i = 0; i < 2; i++)
                    {
                        ((Equalizer)layers[3]).SampleFilters[i].Filters[0].GainDB = -10;
                        ((Equalizer)layers[3]).SampleFilters[i].Filters[1].GainDB = -10;
                    }
                }else
                {
                    ((Equalizer)layers[3]).SampleFilters[9].Filters[0].GainDB = 0;
                    ((Equalizer)layers[3]).SampleFilters[9].Filters[1].GainDB = 0;
                    
                    for (int i = 0; i < 2; i++)
                    {
                        ((Equalizer)layers[3]).SampleFilters[i].Filters[0].GainDB = 0;
                        ((Equalizer)layers[3]).SampleFilters[i].Filters[1].GainDB = 0;
                    }
                }

                // Pitch shift
                if (!viewModel.CheckOnlyEqualizer)
                {
                    var shift = 1f + viewModel.Pitch / 100f;
                    var distortion = ((float)viewModel.Distortion)/100.0;
                    var shiftDistortion = rnd.Next((int)((shift - distortion*1.5) *multiplier), (int)((shift + distortion*1.5) * multiplier))/multiplier;
                    if (shiftDistortion < 0.3)
                        shiftDistortion = rnd.Next((int)(0.3 * multiplier), (int)(0.35 * multiplier)) / multiplier;
                    if (shiftDistortion != ((PitchShifter)layers[0]).PitchShiftFactor)
                        ((PitchShifter)layers[0]).PitchShiftFactor = shiftDistortion;
                }
            };
            
            // Audio output
            soundOut = new WasapiOut();
            soundOut.Device = output;
            soundOut.Initialize(((ISampleSource)layers.Last()).ToWaveSource());
            
            // Start input and output
            soundOut.Play();
            soundIn.Start();
        }
    }
}
