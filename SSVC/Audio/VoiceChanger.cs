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
using System.CodeDom.Compiler;
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
            layers.Add(new SimpleMixer(sampleSource.WaveFormat.Channels, sampleSource.WaveFormat.SampleRate));
            layers.Add(new CSCore.Streams.Effects.PitchShifter((SimpleMixer)layers[0]));
            layers.Add(Equalizer.Create10BandEqualizer((ISampleSource)layers[1]));
            layers.Add(Equalizer.Create10BandEqualizer((ISampleSource)layers[2]));
            layers.Add(Equalizer.Create10BandEqualizer((ISampleSource)layers[3]));

            ((SimpleMixer)layers[0]).AddSource(sampleSource);
            Dictionary<double, ISampleSource> sources = new();
            Dictionary<double, SineGenerator> generators = new();
            var generateGenerators = () =>
            {
                double step = 5;
                for (double i = 50; i < 19000; i += step)
                {
                    Console.WriteLine(i);
                    var generator = new SineGenerator();
                    generator.Frequency = i;
                    generator.Amplitude = 0.0005;
                    generators[i] = generator;

                    var generatorSource = generator.ChangeSampleRate(sampleSource.WaveFormat.SampleRate);
                    if (sampleSource.WaveFormat.Channels == 1)
                        generatorSource = generatorSource.ToMono();
                    if (sampleSource.WaveFormat.Channels == 2)
                        generatorSource = generatorSource.ToStereo();

                    sources[generator.Frequency] = generatorSource;

                    step += 5;
                }
            };
            if (viewModel.CheckArtificialBackgroundNoise)
                generateGenerators();

            var rnd = new Random();
            var multiplier = 1000000f;
            soundIn.DataAvailable += (s,e) => {
                try
                {
                    // Artificial background noise
                    if (viewModel.CheckArtificialBackgroundNoise && (sources.Count == 0 || generators.Count == 0)) 
                        generateGenerators();
                    
                    foreach (KeyValuePair<double, SineGenerator> generator in generators)
                    {
                        if (!viewModel.CheckArtificialBackgroundNoise)
                        {
                            sources.ToList().ForEach(source => ((SimpleMixer)layers[0]).RemoveSource(source.Value));
                            sources = new();
                            generators = new();
                            break;
                        }
                        var amp = (double)generator.Value.Amplitude;
                        var freq = (int)generator.Key;
                        generator.Value.Frequency = (double)rnd.Next(freq-50, freq+50);
                        generator.Value.Amplitude = ((double)rnd.Next(5, 25)) / 10000;
                        var generatorSource = generator.Value.ChangeSampleRate(sampleSource.WaveFormat.SampleRate);
                        if (sampleSource.WaveFormat.Channels == 1)
                            generatorSource = generatorSource.ToMono();
                        if (sampleSource.WaveFormat.Channels == 2)
                            generatorSource = generatorSource.ToStereo();

                        ((SimpleMixer)layers[0]).RemoveSource(sources[freq]);
                        sources[freq] = generatorSource;
                        ((SimpleMixer)layers[0]).AddSource(sources[freq]);

                        Console.WriteLine("changed");
                    }

                    // Equalizer
                    if (!viewModel.CheckOnlyPitch)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            ((Equalizer)layers[2]).SampleFilters[i + 2].Filters.ToList().ForEach(x => {
                                ((Equalizer)layers[2]).SampleFilters[i + 2].Filters[x.Key].GainDB = 
                                (double)(rnd.Next((int)(-viewModel.Distortion / 1.25 * multiplier), (int)(viewModel.Distortion / 1.25 * multiplier)) / multiplier);
                            });
                        }
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        ((Equalizer)layers[2]).SampleFilters[i + 1].Filters.ToList().ForEach(x =>
                        {
                            if (((Equalizer)layers[2]).SampleFilters[i + 1].Filters[x.Key].GainDB < 5)
                                ((Equalizer)layers[2]).SampleFilters[i + 1].Filters[x.Key].GainDB = viewModel.VoiceIncrease;
                        });
                    }

                    if (viewModel.CheckRemoveBackgroundNoise)
                    {
                        ((Equalizer)layers[4]).SampleFilters[9].Filters.ToList().ForEach(x =>
                        {
                            ((Equalizer)layers[4]).SampleFilters[9].Filters[x.Key].GainDB = -50;
                        });

                        for (int i = 0; i < 2; i++)
                        {
                            ((Equalizer)layers[4]).SampleFilters[9].Filters.ToList().ForEach(x =>
                            {
                                ((Equalizer)layers[4]).SampleFilters[i].Filters[x.Key].GainDB = -10;
                            });
                        }
                    }
                    else
                    {
                        ((Equalizer)layers[4]).SampleFilters[9].Filters.ToList().ForEach(x => {
                            ((Equalizer)layers[4]).SampleFilters[9].Filters[x.Key].GainDB = 0;
                        });

                        for (int i = 0; i < 2; i++)
                        {
                            ((Equalizer)layers[4]).SampleFilters[9].Filters.ToList().ForEach(x =>
                            {
                                ((Equalizer)layers[4]).SampleFilters[i].Filters[x.Key].GainDB = 0;
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
                        if (shiftDistortion != ((PitchShifter)layers[1]).PitchShiftFactor)
                            ((PitchShifter)layers[1]).PitchShiftFactor = shiftDistortion;
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
