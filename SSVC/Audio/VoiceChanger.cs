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
                try
                {
                    // Equalizer
                    if (!viewModel.CheckOnlyPitch)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            ((Equalizer)layers[1]).SampleFilters[i + 2].Filters.ToList().ForEach(x => {
                                ((Equalizer)layers[1]).SampleFilters[i + 2].Filters[x.Key].GainDB = 
                                (double)(rnd.Next((int)(-viewModel.Distortion / 1.25 * multiplier), (int)(viewModel.Distortion / 1.25 * multiplier)) / multiplier);
                            });
                        }
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        ((Equalizer)layers[1]).SampleFilters[i + 1].Filters.ToList().ForEach(x =>
                        {
                            if (((Equalizer)layers[1]).SampleFilters[i + 1].Filters[x.Key].GainDB < 5)
                                ((Equalizer)layers[1]).SampleFilters[i + 1].Filters[x.Key].GainDB = viewModel.VoiceIncrease;
                        });
                    }

                    if (viewModel.CheckRemoveBackgroundNoise)
                    {
                        ((Equalizer)layers[3]).SampleFilters[9].Filters.ToList().ForEach(x =>
                        {
                            ((Equalizer)layers[3]).SampleFilters[9].Filters[x.Key].GainDB = -50;
                        });

                        for (int i = 0; i < 2; i++)
                        {
                            ((Equalizer)layers[3]).SampleFilters[9].Filters.ToList().ForEach(x =>
                            {
                                ((Equalizer)layers[3]).SampleFilters[i].Filters[x.Key].GainDB = -10;
                            });
                        }
                    }
                    else
                    {
                        ((Equalizer)layers[3]).SampleFilters[9].Filters.ToList().ForEach(x => {
                            ((Equalizer)layers[3]).SampleFilters[9].Filters[x.Key].GainDB = 0;
                        });

                        for (int i = 0; i < 2; i++)
                        {
                            ((Equalizer)layers[3]).SampleFilters[9].Filters.ToList().ForEach(x =>
                            {
                                ((Equalizer)layers[3]).SampleFilters[i].Filters[x.Key].GainDB = 0;
                            });
                        }
                    }

                    // Pitch shift
                    if (!viewModel.CheckOnlyEqualizer)
                    {
                        var shift = 1f + viewModel.Pitch / 100f;
                        var distortion = ((float)viewModel.Distortion) / 100.0;
                        var shiftDistortion = rnd.Next((int)((shift - distortion * 1.5) * multiplier), (int)((shift + distortion * 1.5) * multiplier)) / multiplier;
                        if (shiftDistortion < 0.3)
                            shiftDistortion = rnd.Next((int)(0.3 * multiplier), (int)(0.35 * multiplier)) / multiplier;
                        if (shiftDistortion != ((PitchShifter)layers[0]).PitchShiftFactor)
                            ((PitchShifter)layers[0]).PitchShiftFactor = shiftDistortion;
                    }
                }
                catch (Exception ex) { 
                    Console.WriteLine(ex.ToString());
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
