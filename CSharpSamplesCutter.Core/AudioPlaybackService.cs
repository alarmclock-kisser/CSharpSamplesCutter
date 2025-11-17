using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSamplesCutter.Core
{
    internal sealed class SwitchingSampleProvider : ISampleProvider
    {
        private ISampleProvider? current;
        private readonly WaveFormat outputFormat;
        private readonly object gate = new();

        public SwitchingSampleProvider(WaveFormat outputFormat)
        {
            this.outputFormat = outputFormat ?? throw new ArgumentNullException(nameof(outputFormat));
        }

        public WaveFormat WaveFormat => this.outputFormat;

        public void SetCurrent(ISampleProvider? provider)
        {
            lock (this.gate)
            {
                this.current = provider;
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            ISampleProvider? p;
            lock (this.gate) { p = this.current; }
            if (p == null) { Array.Clear(buffer, offset, count); return count; }
            return p.Read(buffer, offset, count);
        }
    }

    // ✅ Loop-fähiger Provider: liest von loopStart bis loopEnd, dann zurück zu loopStart
    internal sealed class LoopingSampleProvider : ISampleProvider
    {
        private readonly float[] data;
        private long position; // in Samples (floats)
        private long loopStartSample;
        private long loopEndSample;
        private readonly object loopGate = new(); // ✅ Für thread-safe Updates
        public WaveFormat WaveFormat { get; }

        public long CurrentSampleIndex
        {
            get { return System.Threading.Volatile.Read(ref this.position); }
        }

        public LoopingSampleProvider(float[] data, int sampleRate, int channels, long startSampleIndex = 0, long loopStart = 0, long loopEnd = 0)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels));
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            this.position = Math.Clamp(startSampleIndex, 0, data.LongLength);

            // ✅ Loop-Grenzen setzen (in Samples/Floats)
            this.loopStartSample = Math.Clamp(loopStart, 0, data.LongLength);
            this.loopEndSample = Math.Clamp(loopEnd, 0, data.LongLength);

            // Wenn loopEnd == 0 oder ungültig: Loop deaktivieren (spielen bis Ende)
            if (this.loopEndSample <= this.loopStartSample)
            {
                this.loopEndSample = data.LongLength;
            }
        }

        // ✅ LIVE Loop-Grenzen aktualisieren während Playback läuft!
        public void UpdateLoopBounds(long newLoopStart, long newLoopEnd)
        {
            lock (this.loopGate)
            {
                this.loopStartSample = Math.Clamp(newLoopStart, 0, this.data.LongLength);
                this.loopEndSample = Math.Clamp(newLoopEnd, 0, this.data.LongLength);

                if (this.loopEndSample <= this.loopStartSample)
                {
                    this.loopEndSample = this.data.LongLength;
                }
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;

            while (samplesRead < count)
            {
                long loopStart, loopEnd;
                lock (this.loopGate)
                {
                    loopStart = this.loopStartSample;
                    loopEnd = this.loopEndSample;
                }

                // ✅ FIX: Nur loopen wenn Loop-Grenzen gültig sind
                if (loopEnd <= loopStart)
                {
                    // ❌ Loop NICHT aktiv → spielen bis zum Array-Ende
                    loopStart = 0;
                    loopEnd = this.data.LongLength;
                }

                // Wie viel können wir bis zum Loop-Ende lesen?
                long samplesUntilLoopEnd = loopEnd - this.position;

                if (samplesUntilLoopEnd <= 0)
                {
                    // ✅ Loop-Ende erreicht: zurück zu Loop-Start
                    this.position = loopStart;
                    samplesUntilLoopEnd = loopEnd - this.position;

                    // Safeguard: Wenn immer noch negativ, dann Playback beenden
                    if (samplesUntilLoopEnd <= 0)
                    {
                        Array.Clear(buffer, offset + samplesRead, count - samplesRead);
                        return samplesRead;
                    }
                }

                int samplesToRead = (int) Math.Min(samplesUntilLoopEnd, count - samplesRead);
                if (samplesToRead <= 0)
                {
                    Array.Clear(buffer, offset + samplesRead, count - samplesRead);
                    return samplesRead;
                }

                // Kopiere vom Data-Array
                Array.Copy(this.data, (int) this.position, buffer, offset + samplesRead, samplesToRead);
                this.position += samplesToRead;
                samplesRead += samplesToRead;
            }

            return samplesRead;
        }
    }

    // Einfacher Provider, der aus einem Float-Array (interleaved) liest
    internal sealed class ArraySampleProvider : ISampleProvider
    {
        private readonly float[] data;
        private long position; // in Samples (floats)
        public WaveFormat WaveFormat { get; }

        public ArraySampleProvider(float[] data, int sampleRate, int channels, long startSampleIndex = 0)
        {
            this.data = data ?? throw new ArgumentNullException(nameof(data));
            if (channels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channels));
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            this.position = Math.Clamp(startSampleIndex, 0, data.LongLength);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesAvailable = (int) Math.Min(count, this.data.LongLength - this.position);
            if (samplesAvailable <= 0)
            {
                Array.Clear(buffer, offset, count);
                return 0;
            }
            Array.Copy(this.data, (int) this.position, buffer, offset, samplesAvailable);
            this.position += samplesAvailable;
            return samplesAvailable;
        }
    }

    public sealed class AudioPlaybackService : IDisposable
    {
        private readonly WaveOutEvent player;
        private readonly bool ownsPlayer;
        private AudioFileReader? reader;
        private SwitchingSampleProvider? switching;
        private SampleToWaveProvider? waveProvider;
        private ISampleProvider? pipeline;
        private readonly object graphGate = new();
        private float[]? rawData;
        private int rawSampleRate;
        private int rawChannels;
        private LoopingSampleProvider? loopingProvider; // ✅ Speichere LoopingProvider für Updates
        private long loopStartCurrent = 0; // in samples (floats)
        private long loopEndCurrent = 0;   // in samples (floats)

        public float PlaybackRate { get; private set; } = 1.0f;
        public int DeviceSampleRate { get; private set; } = 44100;
        public int Channels { get; private set; } = 2;

        public bool IsLooping => this.loopingProvider != null;

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        // Nur intern für AudioObj.DisableLoopNow – aktuelles Sample (ohne Resampler-Verschiebung)
        internal long GetLoopingCurrentSampleIndexUnsafe()
        {
            if (this.loopingProvider != null)
            {
                try { return this.loopingProvider.CurrentSampleIndex; } catch { return 0; }
            }
            return 0;
        }
        public AudioPlaybackService()
        {
            this.player = new WaveOutEvent();
            this.ownsPlayer = true;
            this.player.PlaybackStopped += (s, e) => this.PlaybackStopped?.Invoke(this, e);
        }

        public AudioPlaybackService(WaveOutEvent player)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.ownsPlayer = false;
            this.player.PlaybackStopped += (s, e) => this.PlaybackStopped?.Invoke(this, e);
        }

        public async Task InitializePlayback(string filePath, int? deviceSampleRate = null, int desiredLatency = 50, float initialVolume = 1.0f)
        {
            this.ResetGraph();

            // Quelle laden (AudioFileReader liefert Float32, normalisiert)
            this.reader = new AudioFileReader(filePath);
            this.Channels = this.reader.WaveFormat.Channels;
            this.DeviceSampleRate = deviceSampleRate ?? this.reader.WaveFormat.SampleRate;

            // Konstantes Geräteformat
            var deviceFormat = WaveFormat.CreateIeeeFloatWaveFormat(this.DeviceSampleRate, this.Channels);

            // Umschaltbarer Provider mit konstantem Format
            this.switching = new SwitchingSampleProvider(deviceFormat);

            // Erste Pipeline (PlaybackRate =1.0)
            this.pipeline = BuildPipeline(this.reader, this.PlaybackRate, deviceFormat);
            this.switching.SetCurrent(this.pipeline);

            // WaveOut initialisieren (falls noch nicht)
            this.player.DesiredLatency = desiredLatency;
            this.player.Volume = Math.Clamp(initialVolume, 0f, 1f);
            this.waveProvider = new SampleToWaveProvider(this.switching);
            this.player.Init(this.waveProvider);

            // Start (nicht blocking)
            await Task.Run(() => this.player.Play());
        }

        public async Task InitializePlayback(float[] data, int sampleRate, int channels, long startSampleIndex = 0, int? deviceSampleRate = null, int desiredLatency = 50, float initialVolume = 1.0f, long loopStartSample = 0, long loopEndSample = 0)
        {
            this.ResetGraph();

            this.rawData = data; // keep reference
            this.rawSampleRate = sampleRate;
            this.rawChannels = channels;

            this.Channels = channels;
            this.DeviceSampleRate = deviceSampleRate ?? sampleRate;

            // ✅ Verwende LoopingSampleProvider wenn Loop-Grenzen gesetzt
            ISampleProvider source;
            if (loopEndSample > loopStartSample && loopEndSample <= data.LongLength)
            {
                this.loopStartCurrent = loopStartSample;
                this.loopEndCurrent = loopEndSample;
                this.loopingProvider = new LoopingSampleProvider(data, sampleRate, channels, startSampleIndex, loopStartSample, loopEndSample);
                source = this.loopingProvider;
            }
            else
            {
                this.loopStartCurrent = 0;
                this.loopEndCurrent = 0;
                this.loopingProvider = null;
                source = new ArraySampleProvider(data, sampleRate, channels, startSampleIndex);
            }

            var deviceFormat = WaveFormat.CreateIeeeFloatWaveFormat(this.DeviceSampleRate, this.Channels);

            this.switching = new SwitchingSampleProvider(deviceFormat);
            this.pipeline = BuildPipeline(source, this.PlaybackRate, deviceFormat);
            this.switching.SetCurrent(this.pipeline);

            this.player.DesiredLatency = desiredLatency;
            this.player.Volume = Math.Clamp(initialVolume, 0f, 1f);
            this.waveProvider = new SampleToWaveProvider(this.switching);
            this.player.Init(this.waveProvider);

            await Task.Run(() => this.player.Play());
        }

        // 🔁 Provider live zu Looping wechseln (ohne Stop)
        public void SwitchToLoop(long currentSampleIndex, long loopStartSample, long loopEndSample)
        {
            if (this.switching == null || this.rawData == null)
            {
                return;
            }

            this.loopStartCurrent = loopStartSample;
            this.loopEndCurrent = loopEndSample;

            var source = new LoopingSampleProvider(this.rawData, this.rawSampleRate, this.rawChannels, currentSampleIndex, loopStartSample, loopEndSample);
            var newPipeline = BuildPipeline(source, this.PlaybackRate, this.switching.WaveFormat);
            lock (this.graphGate)
            {
                this.loopingProvider = source;
                this.pipeline = newPipeline;
                this.switching.SetCurrent(this.pipeline);
            }
        }

        // 🔁 Provider live zurück auf linear (kein Loop)
        public void SwitchToLinear(long currentSampleIndex)
        {
            if (this.switching == null || this.rawData == null)
            {
                return;
            }

            // Wenn wir aus einem Loop kommen, nimm exakte aktuelle Position des Loop-Providers
            if (this.loopingProvider != null)
            {
                try { currentSampleIndex = this.loopingProvider.CurrentSampleIndex; } catch { }
            }

            this.loopStartCurrent = 0;
            this.loopEndCurrent = 0;

            var source = new ArraySampleProvider(this.rawData, this.rawSampleRate, this.rawChannels, currentSampleIndex);
            var newPipeline = BuildPipeline(source, this.PlaybackRate, this.switching.WaveFormat);
            lock (this.graphGate)
            {
                this.loopingProvider = null;
                this.pipeline = newPipeline;
                this.switching.SetCurrent(this.pipeline);
            }
        }

        // Nahtlose Anpassung der Geschwindigkeit (Pitch & Tempo ändern sich gemeinsam, "Varispeed")
        public async Task AdjustSampleRate(float factor)
        {
            if (this.switching == null)
            {
                return;
            }

            this.PlaybackRate = factor;

            // Neue Pipeline auf Basis derselben Quelle aufbauen und atomar umschalten
            ISampleProvider? currentSource;
            lock (this.graphGate)
            {
                currentSource = this.pipeline; // aktuelle Pipeline-Quelle ist die erste Stufe der Kette
            }
            if (currentSource == null)
            {
                return;
            }

            ISampleProvider baseSource = this.reader ?? currentSource;
            var newPipeline = BuildPipeline(baseSource, this.PlaybackRate, this.switching.WaveFormat);
            lock (this.graphGate)
            {
                this.pipeline = newPipeline;
                this.switching.SetCurrent(this.pipeline);
            }

            await Task.CompletedTask; // API bleibt async
        }

        // Seek in paused state without reinitializing WaveOut
        public void SeekSamples(long startSampleIndex)
        {
            if (this.switching == null || this.rawData == null || this.rawSampleRate <= 0 || this.rawChannels <= 0)
            {
                return;
            }
            startSampleIndex = Math.Clamp(startSampleIndex, 0, this.rawData.LongLength);
            ISampleProvider source;
            if (this.loopingProvider != null)
            {
                source = new LoopingSampleProvider(this.rawData, this.rawSampleRate, this.rawChannels, startSampleIndex, this.loopStartCurrent, this.loopEndCurrent);
            }
            else
            {
                source = new ArraySampleProvider(this.rawData, this.rawSampleRate, this.rawChannels, startSampleIndex);
            }
            var newPipeline = BuildPipeline(source, this.PlaybackRate, this.switching.WaveFormat);
            lock (this.graphGate)
            {
                if (source is LoopingSampleProvider lsp) this.loopingProvider = lsp; else this.loopingProvider = null;
                this.pipeline = newPipeline;
                this.switching.SetCurrent(this.pipeline);
            }
        }

        // Graph aufbauen: Quelle -> (Resample auf R*f) -> (Resample auf DeviceRate) -> konstant D
        private static ISampleProvider BuildPipeline(ISampleProvider source, double rate, WaveFormat deviceFormat)
        {
            int sourceRate = source.WaveFormat.SampleRate;
            int channels = source.WaveFormat.Channels;

            var spedUp = new WdlResamplingSampleProvider(source, Math.Max(8000, (int) Math.Round(sourceRate * rate)));
            var toDevice = new WdlResamplingSampleProvider(spedUp, deviceFormat.SampleRate);

            if (toDevice.WaveFormat.Channels != deviceFormat.Channels)
            {
                if (deviceFormat.Channels == 1 && channels > 1)
                {
                    toDevice = new WdlResamplingSampleProvider(
                        new StereoToMonoSampleProvider(spedUp) { LeftVolume = 0.5f, RightVolume = 0.5f },
                        deviceFormat.SampleRate);
                }
            }

            return toDevice;
        }

        public void Stop()
        {
            this.player?.Stop();
        }

        public void Pause()
        {
            if (this.player.PlaybackState == PlaybackState.Playing)
            {
                this.player.Pause();
            }
        }

        public void Resume()
        {
            if (this.player.PlaybackState == PlaybackState.Paused)
            {
                this.player.Play();
            }
        }

        public long GetPositionBytes()
        {
            try { return this.player.GetPosition(); } catch { return 0; }
        }

        public void SetVolume(float volume)
        {
            this.player.Volume = Math.Clamp(volume, 0f, 1f);
        }

        // ✅ LIVE Loop-Grenzen aktualisieren während Playback läuft!
        public void UpdateLoopBounds(long newLoopStartSample, long newLoopEndSample)
        {
            if (this.loopingProvider != null)
            {
                this.loopingProvider.UpdateLoopBounds(newLoopStartSample, newLoopEndSample);
            }
        }

        private void ResetGraph()
        {
            try
            {
                // Stop playback and wait briefly for device to settle — avoids "Can't re-initialize during playback" from NAudio
                try { this.player.Stop(); } catch { }

                // Wait up to 250ms for the player to reach Stopped state
                try
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (this.player.PlaybackState != PlaybackState.Stopped && sw.ElapsedMilliseconds < 250)
                    {
                        Thread.Sleep(5);
                    }
                }
                catch { }
            }
            catch { }

            this.reader?.Dispose();
            this.reader = null;
            this.switching = null;
            this.waveProvider = null;
            this.pipeline = null;
            this.rawData = null;
            this.rawSampleRate = 0;
            this.rawChannels = 0;
            this.loopingProvider = null;
        }

        public void Dispose()
        {
            try { this.player.Stop(); } catch { }
            this.reader?.Dispose();
            this.reader = null;
            this.switching = null;
            this.waveProvider = null;
            this.pipeline = null;
            if (this.ownsPlayer)
            {
                this.player.Dispose();
            }
        }
    }
}
