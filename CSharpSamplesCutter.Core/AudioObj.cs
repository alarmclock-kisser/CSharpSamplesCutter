using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSamplesCutter.Core
{
    public class AudioObj : IDisposable
    {
        // Fields
        public Guid Id { get; set; } = Guid.NewGuid();
        public readonly DateTime CreatedAt = DateTime.UtcNow;
        public string Name { get; set; } = string.Empty;
        public string DisplayName => (this.Playing ? "▶" : this.Paused ? "||" : "") + (!string.IsNullOrEmpty(this.Name) ? this.Name : Path.GetFileNameWithoutExtension(this.FilePath) ?? "Unnamed");

        public string FilePath { get; set; } = string.Empty;

        public float[] Data { get; set; } = [];
        public int SampleRate { get; set; } = 0;
        public double SampleRateFactor { get; private set; } = 1.0;
        public int AdjustedSampleRate => (int) (this.SampleRate * this.SampleRateFactor);
        public int Channels { get; set; } = 0;
        public int BitDepth { get; set; } = 0;
        public long Length { get; set; } = 0;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;

        public float Bpm { get; set; } = 0.0f;
        public float ScannedBpm { get; set; } = 0.0f;
        public float Timing { get; set; } = 1.0f;
        public float ScannedTiming { get; set; } = 1.0f;

        public string Key { get; set; } = string.Empty;
        public string ScannedKey { get; set; } = string.Empty;
        public float Volume { get; set; } = 1.0f;

        public bool Playing { get; private set; } = false;
        public bool Paused { get; private set; } = false;

        public int ChunkSize { get; set; } = 0;
        public int OverlapSize { get; set; } = 0;
        public double StretchFactor { get; set; } = 1.0;

        public long SkippedPositionBytes { get; private set; } = 0;

        public long ScrollOffset { get; set; } = 0;
        public long StartingOffset { get; set; } = 0;
        public long SelectionStart { get; set; } = -1;
        public long SelectionEnd { get; set; } = -1;
        public int LastSamplesPerPixel { get; set; } = 0;
        public string SampleTag { get; set; } = string.Empty;
        public BindingList<AudioObj> PreviousSteps = [];
        public BindingList<AudioObj> NextSteps = [];

        // Loop support
        public long LoopStartFrames { get; set; } = 0;
        public long LoopEndFrames { get; set; } = 0;

        // Playback service (neu)
        private readonly AudioPlaybackService playback = new();

        // Properties
        private long positionOriginBytes = 0;
        private bool resumeFromSetPosition = false; // minimal: reinit on resume if position changed while paused
        private long pausedBaselineBytes = 0; // store baseline at pause for accurate delta after seek while paused

        // Enums
        public Dictionary<string, double> Metrics { get; private set; } = [];
        public double this[string metric]
        {
            get
            {
                // Find by tolower case
                if (this.Metrics.TryGetValue(metric, out double value))
                {
                    return value;
                }
                else
                {
                    var key = this.Metrics.Keys.FirstOrDefault(k => k.Equals(metric, StringComparison.OrdinalIgnoreCase));
                    if (key != null)
                    {
                        return this.Metrics[key];
                    }
                    else
                    {
                        // If not found, return 0.0
                        return 0.0;
                    }
                }
            }
            set
            {
                // Find by tolower case
                if (this.Metrics.ContainsKey(metric))
                {
                    this.Metrics[metric] = value;
                }
                else
                {
                    var key = this.Metrics.Keys.FirstOrDefault(k => k.Equals(metric, StringComparison.OrdinalIgnoreCase));
                    if (key != null)
                    {
                        this.Metrics[key] = value;
                    }
                    else
                    {
                        // Capitalize first letter and add to dictionary
                        string capitalizedMetric = char.ToUpper(metric[0]) + metric.Substring(1).ToLowerInvariant();
                        this.Metrics.Add(capitalizedMetric, value);
                    }
                }
            }
        }

        // Lambda
        public bool PlayerPlaying => this.Playing && !this.Paused;

        // Konsistente aktuelle Byte-Position (absolut)
        public long CurrentPlaybackPositionBytes
        {
            get
            {
                if (this.PlayerPlaying)
                {
                    long gp = 0;
                    try { gp = this.playback.GetPositionBytes(); } catch { gp = 0; }
                    long delta = gp - this.positionOriginBytes;
                    if (delta < 0) { delta = 0; }
                    return this.SkippedPositionBytes + delta;
                }
                return this.SkippedPositionBytes;
            }
            private set
            {
                // Clamp auf Datenlänge, ohne auf einen WaveStream angewiesen zu sein
                int ch = Math.Max(1, this.Channels);
                int bytesPerFrame = ch * sizeof(float);
                long totalFrames = (this.Data?.LongLength ?? 0L) / ch;
                long totalBytes = totalFrames * bytesPerFrame;

                long target = Math.Clamp(value, 0, totalBytes);
                this.SkippedPositionBytes = target;
            }
        }

        // ✅ Position in Frames - LOOPING-AWARE
        public long Position
        {
            get
            {
                if (!this.PlayerPlaying)
                {
                    long positionBytes = this.CurrentPlaybackPositionBytes;
                    int bytesPerFrame = Math.Max(1, this.Channels) * sizeof(float);
                    return bytesPerFrame > 0 ? positionBytes / bytesPerFrame : 0;
                }

                // ✅ LOOPING: Wenn Loop aktiv, Position relativ zu LoopStartFrames
                if (this.LoopEndFrames > this.LoopStartFrames)
                {
                    long positionBytes = this.CurrentPlaybackPositionBytes;
                    int bytesPerFrame = Math.Max(1, this.Channels) * sizeof(float);
                    long absFrame = bytesPerFrame > 0 ? positionBytes / bytesPerFrame : 0;

                    // ✅ Mapping: Absolute Position → Loop-relativ
                    long loopLength = this.LoopEndFrames - this.LoopStartFrames;
                    if (loopLength > 0)
                    {
                        long relFrame = absFrame - this.LoopStartFrames;
                        relFrame = ((relFrame % loopLength) + loopLength) % loopLength; // Handles negatives
                        return this.LoopStartFrames + relFrame;
                    }
                }

                // Normal (no loop)
                long normalBytes = this.CurrentPlaybackPositionBytes;
                int normalBytesPerFrame = Math.Max(1, this.Channels) * sizeof(float);
                return normalBytesPerFrame > 0 ? normalBytes / normalBytesPerFrame : 0;
            }
        }
        public TimeSpan CurrentTime => TimeSpan.FromSeconds((double) this.Position / Math.Max(1, this.SampleRate));
        public double SizeInKb => this.Data.Length * sizeof(float) / 1024.0;

        // Ctor
        public AudioObj()
        {
            // Empty
        }

        public AudioObj(string filePath, bool load = false)
        {
            this.FilePath = filePath;

            if (load)
            {
                if (this.LoadAudioFile())
                {
                    return;

                }
                else
                {
                    this.Dispose();
                }
            }
        }

        // Static factory
        public static AudioObj? FromFile(string filePath)
        {
            var obj = new AudioObj(filePath);
            if (obj.LoadAudioFile())
            {
                return obj;
            }
            else
            {
                return null;
            }
        }

        public static async Task<AudioObj?> FromFileAsync(string filePath)
        {
            var obj = await Task.Run(() => new AudioObj(filePath));
            if (obj.LoadAudioFile())
            {
                return obj;
            }
            else
            {
                obj?.Dispose();
                return null;
            }
        }

        // Clone
        public AudioObj Clone()
        {
            AudioObj clone = new()
            {
                Id = Guid.NewGuid(),
                Name = this.Name,
                FilePath = this.FilePath,
                Data = (float[]) this.Data.Clone(),
                SampleRate = this.SampleRate,
                Channels = this.Channels,
                BitDepth = this.BitDepth,
                Length = this.Length,
                Duration = this.Duration,
                Bpm = this.Bpm,
                Timing = this.Timing,
                Volume = this.Volume
            };

            return clone;
        }

        public async Task<AudioObj> CloneAsync()
        {
            return await Task.Run(() => this.Clone());
        }

        public async Task<AudioObj?> CloneFromSelectionAsync(long? startIndex = null, long? endIndex = null)
        {
            startIndex ??= this.SelectionStart;
            endIndex ??= this.SelectionEnd;

            if (this.Data == null || this.Data.LongLength <= 0 || this.SelectionEnd < 0 || this.SelectionStart < 0 || this.SelectionStart == this.SelectionEnd)
            {
                return null;
            }

            if (endIndex.Value < startIndex.Value)
            {
                long swap = endIndex.Value;
                endIndex = startIndex.Value;
                startIndex = swap;
            }

            int channels = Math.Max(1, this.Channels);
            long totalSamples = this.Data.LongLength; // interleaved samples
            long selStartSample = Math.Clamp(startIndex.Value, 0, totalSamples);
            long selEndSample = Math.Clamp(endIndex.Value, 0, totalSamples);
            long selSampleCount = selEndSample - selStartSample; // interleaved sample count

            if (selSampleCount <= 0)
            {
                return null;
            }

            AudioObj clone = new()
            {
                Name = this.Name + "_selection",
                Data = new float[selSampleCount],
                SampleRate = this.SampleRate,
                Channels = this.Channels,
                BitDepth = this.BitDepth,
                Bpm = this.Bpm,
                Timing = this.Timing,
                Volume = this.Volume,
                Length = selSampleCount,
                Duration = TimeSpan.FromSeconds((double) selSampleCount / (this.SampleRate * channels))
            };

            Buffer.BlockCopy(
                src: this.Data,
                srcOffset: (int) (selStartSample * sizeof(float)),
                dst: clone.Data,
                dstOffset: 0,
                count: (int) (selSampleCount * sizeof(float))
            );

            await Task.CompletedTask;

            return clone;
        }

        // IO Methods
        public void Dispose()
        {
            this.Playing = false;
            this.Paused = false;

            this.Data = [];

            // Player stoppen/aufräumen
            try { this.playback.Stop(); } catch { }

            GC.SuppressFinalize(this);
        }

        public bool LoadAudioFile()
        {
            if (string.IsNullOrEmpty(this.FilePath))
            {
                return false;
            }

            this.Name = Path.GetFileNameWithoutExtension(this.FilePath);

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                using var reader = new AudioFileReader(this.FilePath);
                this.SampleRate = reader.WaveFormat.SampleRate;
                this.Channels = reader.WaveFormat.Channels;
                this.BitDepth = reader.WaveFormat.BitsPerSample;

                long numSamples = 0;
                if (reader.Length > 0 && reader.WaveFormat.BitsPerSample > 0)
                {
                    numSamples = reader.Length / (reader.WaveFormat.BitsPerSample / 8);
                }
                if (numSamples > 0)
                {
                    try
                    {
                        float[] tmp = new float[numSamples];
                        int read = reader.Read(tmp, 0, (int) numSamples);
                        if (read != numSamples)
                        {
                            float[] resized = new float[read];
                            Array.Copy(tmp, resized, read);
                            this.Data = resized;
                        }
                        else
                        {
                            this.Data = tmp;
                        }
                    }
                    catch
                    {
                        this.Data = ReadAllSamplesStreaming(reader).ToArray();
                    }
                }
                else
                {
                    this.Data = ReadAllSamplesStreaming(reader).ToArray();
                }

                this.Length = this.Data.Length;
                this.Duration = reader.TotalTime;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading audio file: {ex.Message}");
                this.Dispose();
                return false;
            }

            this["Import"] = sw.Elapsed.TotalMilliseconds;
            sw.Restart();

            this.ReadBpmTag();

            this["ReadBpmTag"] = sw.Elapsed.TotalMilliseconds;
            sw.Stop();

            return true;
        }

        public float ReadBpmTag(string tag = "TBPM", bool set = true)
        {
            // Read bpm metadata if available
            float bpm = 0.0f;
            float roughBpm = 0.0f;

            try
            {
                if (!string.IsNullOrEmpty(this.FilePath) && File.Exists(this.FilePath))
                {
                    using var file = TagLib.File.Create(this.FilePath);
                    if (file.Tag.BeatsPerMinute > 0)
                    {
                        roughBpm = (float) file.Tag.BeatsPerMinute;
                    }
                    if (file.TagTypes.HasFlag(TagLib.TagTypes.Id3v2))
                    {
                        var id3v2Tag = (TagLib.Id3v2.Tag) file.GetTag(TagLib.TagTypes.Id3v2);

                        var tagTextFrame = TagLib.Id3v2.TextInformationFrame.Get(id3v2Tag, tag, false);

                        if (tagTextFrame != null && tagTextFrame.Text.Any())
                        {
                            string bpmString = tagTextFrame.Text.FirstOrDefault() ?? "0,0";
                            if (!string.IsNullOrEmpty(bpmString))
                            {
                                bpmString = bpmString.Replace(',', '.');

                                if (float.TryParse(bpmString, NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedBpm))
                                {
                                    bpm = parsedBpm;
                                }
                            }
                        }
                        else
                        {
                            bpm = 0.0f;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Lesen des Tags {tag.ToUpper()}: {ex.Message} ({ex.InnerException?.Message ?? " - "})");
            }

            // Take rough bpm if <= 0.0f
            if (bpm <= 0.0f && roughBpm > 0.0f)
            {
                Console.WriteLine($"No value found for '{tag.ToUpper()}', taking rough BPM value from legacy tag.");
                bpm = roughBpm;
            }

            if (set)
            {
                this.Bpm = bpm;
                if (this.Bpm <= 10)
                {
                    this.ReadBpmTagLegacy();
                }
            }

            return bpm;
        }

        public float ReadBpmTagLegacy()
        {
            // Read bpm metadata if available
            float bpm = 0.0f;

            try
            {
                if (!string.IsNullOrEmpty(this.FilePath) && File.Exists(this.FilePath))
                {
                    using var file = TagLib.File.Create(this.FilePath);
                    // Check for BPM in standard ID3v2 tag
                    if (file.Tag.BeatsPerMinute > 0)
                    {
                        bpm = (float) file.Tag.BeatsPerMinute;
                    }
                    // Alternative für spezielle Tags (z.B. TBPM Frame)
                    else if (file.TagTypes.HasFlag(TagLib.TagTypes.Id3v2))
                    {
                        var id3v2Tag = (TagLib.Id3v2.Tag) file.GetTag(TagLib.TagTypes.Id3v2);
                        var bpmFrame = TagLib.Id3v2.UserTextInformationFrame.Get(id3v2Tag, "BPM", false);

                        if (bpmFrame != null && float.TryParse(bpmFrame.Text.FirstOrDefault(), out float parsedBpm))
                        {
                            bpm = parsedBpm;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Lesen der BPM: {ex.Message}");
            }
            this.Bpm = bpm > 0 ? bpm / 100.0f : 0.0f;
            return this.Bpm;
        }

        public async Task CreateSnapshotAsync()
        {
            this.PreviousSteps.Add(await this.CloneAsync());
        }



        // Data Methods
        public async Task<byte[]> GetBytesAsync(int maxWorkers = 4)
        {
            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);

            if (this.Data == null || this.Data.Length == 0)
            {
                return [];
            }

            int bytesPerSample = this.BitDepth / 8;
            byte[] result = new byte[this.Data.Length * bytesPerSample];

            await Task.Run(() =>
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxWorkers
                };

                Parallel.For(0, this.Data.Length, options, i =>
                {
                    float sample = this.Data[i];

                    switch (this.BitDepth)
                    {
                        case 8:
                            result[i] = (byte) (sample * 127f);
                            break;

                        case 16:
                            short sample16 = (short) (sample * short.MaxValue);
                            Span<byte> target16 = result.AsSpan(i * 2, 2);
                            BitConverter.TryWriteBytes(target16, sample16);
                            break;

                        case 24:
                            int sample24 = (int) (sample * 8_388_607f); // 2^23 - 1
                            Span<byte> target24 = result.AsSpan(i * 3, 3);
                            target24[0] = (byte) sample24;
                            target24[1] = (byte) (sample24 >> 8);
                            target24[2] = (byte) (sample24 >> 16);
                            break;

                        case 32:
                            Span<byte> target32 = result.AsSpan(i * 4, 4);
                            BitConverter.TryWriteBytes(target32, sample);
                            break;
                    }
                });
            });

            return result;
        }

        public async Task<IEnumerable<float[]>> GetChunksAsync(int size = 2048, float overlap = 0.5f, bool keepData = false, int maxWorkers = 4)
        {
            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);

            // Input Validation (sync part for fast fail)
            if (this.Data == null || this.Data.Length == 0)
            {
                return [];
            }

            if (size <= 0 || overlap < 0 || overlap >= 1)
            {
                return [];
            }

            // Calculate chunk metrics (sync)
            this.ChunkSize = size;
            this.OverlapSize = (int) (size * overlap);
            int step = size - this.OverlapSize;
            int numChunks = (this.Data.Length - size) / step + 1;

            // Prepare result array
            float[][] chunks = new float[numChunks][];

            await Task.Run(() =>
            {
                // Parallel processing with optimal worker count
                Parallel.For(0, numChunks, new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxWorkers
                }, i =>
                {
                    int sourceOffset = i * step;
                    float[] chunk = new float[size];
                    Buffer.BlockCopy(
                        src: this.Data,
                        srcOffset: sourceOffset * sizeof(float),
                        dst: chunk,
                        dstOffset: 0,
                        count: size * sizeof(float));
                    chunks[i] = chunk;
                });
            });

            // Cleanup if requested
            if (!keepData)
            {
                this.Data = [];
            }

            return chunks;
        }

        public async Task AggregateStretchedChunksAsync(IEnumerable<float[]> chunks, double stretchFactor = 1.0, int maxWorkers = 4)
        {
            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);

            if (chunks == null || !chunks.Any())
            {
                return;
            }

            this.StretchFactor = stretchFactor;

            // Pre-calculate all values that don't change
            int chunkSize = this.ChunkSize;
            int overlapSize = this.OverlapSize;
            int originalHopSize = chunkSize - overlapSize;
            int stretchedHopSize = (int) Math.Round(originalHopSize * stretchFactor);
            int outputLength = (chunks.Count() - 1) * stretchedHopSize + chunkSize;

            // Create window function (cosine window)
            double[] window = await Task.Run(() =>
                Enumerable.Range(0, chunkSize)
                          .Select(i => 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (chunkSize - 1))))
                          .ToArray()
            ).ConfigureAwait(false);

            // Logging: Chunks-Infos
            var chunkList = chunks.ToList();
            Debug.WriteLine($"[AggregateStretchedChunks] Chunks: {chunkList.Count}, ChunkSize: {chunkSize}, OutputLength: {outputLength}");
            for (int c = 0; c < Math.Min(3, chunkList.Count); c++)
            {
                var arr = chunkList[c];
                Debug.WriteLine($"Chunk[{c}] Length: {arr.Length}, Min: {arr.Min()}, Max: {arr.Max()}, First10: {string.Join(", ", arr.Take(10))}");
            }

            // Initialize accumulators in parallel
            double[] outputAccumulator = new double[outputLength];
            double[] weightSum = new double[outputLength];

            await Task.Run(() =>
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxWorkers
                };

                // Phase 1: Process chunks in parallel
                Parallel.For(0, chunkList.Count, parallelOptions, chunkIndex =>
                {
                    var chunk = chunkList[chunkIndex];
                    int offset = chunkIndex * stretchedHopSize;

                    for (int j = 0; j < Math.Min(chunkSize, chunk.Length); j++)
                    {
                        int idx = offset + j;
                        if (idx >= outputLength)
                        {
                            break;
                        }

                        double windowedSample = chunk[j] * window[j];

                        // Using Interlocked for thread-safe accumulation
                        Interlocked.Exchange(ref outputAccumulator[idx], outputAccumulator[idx] + windowedSample);
                        Interlocked.Exchange(ref weightSum[idx], weightSum[idx] + window[j]);
                    }
                });

                // Phase 2: Normalize results
                float[] finalOutput = new float[outputLength];
                Parallel.For(0, outputLength, parallelOptions, i =>
                {
                    finalOutput[i] = weightSum[i] > 1e-6
                        ? (float) (outputAccumulator[i] / weightSum[i])
                        : 0.0f;
                });

                // Final assignment (thread-safe)
                this.Data = finalOutput;
            }).ConfigureAwait(true);

            // Logging: Output-Infos
            Debug.WriteLine($"[AggregateStretchedChunks] Output Min: {this.Data.Min()}, Max: {this.Data.Max()}, First10: {string.Join(", ", this.Data.Take(10))}");
        }

        public async Task EraseSelectionAsync(long? selectionStart = null, long? selectionEnd = null)
        {
            selectionStart ??= this.SelectionStart;
            selectionEnd ??= this.SelectionEnd;

            if (this.Data == null || this.Length <= 0 || selectionEnd.Value < 0 || selectionStart.Value < 0 || selectionStart.Value == selectionEnd.Value)
            {
                return;
            }

            if (selectionEnd < selectionStart)
            {
                long swap = selectionEnd.Value;
                selectionEnd = selectionStart.Value;
                selectionStart = swap;
            }

            await this.StopAsync();

            // Interleaved Samples (Floats), keine Frames
            long totalSamples = this.Data.LongLength;
            long selStart = Math.Clamp(selectionStart.Value, 0, totalSamples);
            long selEnd = Math.Clamp(selectionEnd.Value, 0, totalSamples);
            long selCount = selEnd - selStart;
            if (selCount <= 0)
            {
                return;
            }

            float[] newData = new float[this.Data.Length - selCount];
            await Task.Run(() =>
            {
                int bytesBefore = checked((int) (selStart * sizeof(float)));
                int srcAfterOffset = checked((int) (selEnd * sizeof(float)));
                int bytesAfter = checked((int) ((totalSamples - selEnd) * sizeof(float)));
                int dstAfterOffset = bytesBefore;

                // Validate bounds before copying
                if (srcAfterOffset + bytesAfter > this.Data.Length * sizeof(float))
                {
                    return;
                }

                if (bytesBefore > 0)
                {
                    Buffer.BlockCopy(this.Data, 0, newData, 0, bytesBefore);
                }

                if (bytesAfter > 0)
                {
                    Buffer.BlockCopy(this.Data, srcAfterOffset, newData, dstAfterOffset, bytesAfter);
                }
            });

            this.Data = newData;
            int channels = Math.Max(1, this.Channels);
            this.Length = this.Data.Length;
            this.Duration = TimeSpan.FromSeconds((double) this.Data.Length / (this.SampleRate * channels));

            this.SelectionStart = -1;
            this.SelectionEnd = -1;
        }

        public async Task CutOffBeforeAsync(long? sampleIndex = null)
        {
            sampleIndex ??= this.SelectionStart >= 0 ? this.SelectionStart : this.Playing ? this.Position : this.StartingOffset;

            // Erase 0 to sampleIndex
            await this.EraseSelectionAsync(0, sampleIndex.Value);

            // Adjust StartingOffset and SkippedPositionBytes
            this.StartingOffset = 0;
            this.SkippedPositionBytes = 0;
        }

        public async Task CutOffAfterAsync(long? sampleIndex = null)
        {
            sampleIndex ??= this.SelectionEnd >= 0 ? this.SelectionEnd : this.Playing ? this.Position : this.StartingOffset;

            // Erase sampleIndex to end
            await this.EraseSelectionAsync(sampleIndex.Value, this.Length);

            this.StartingOffset = 0;
            this.SkippedPositionBytes = 0;
        }

        // Playback Methods (PlaybackService)
        public async Task PlayAsync(CancellationToken cancellationToken, Action? onPlaybackStopped = null, float? initialVolume = null, int desiredLatency = 50, long loopStartSample = 0, long loopEndSample = 0)
        {
            initialVolume ??= this.Volume / 100f;

            if (this.Data == null || this.Data.Length == 0 || this.SampleRate <= 0 || this.Channels <= 0)
            {
                this.Playing = false;
                return;
            }

            try
            {
                // Stop any existing playback immediately to avoid overlapping output
                try { this.playback.Stop(); } catch { }

                this.Playing = true;
                this.Paused = false;

                // Start-Offset in Samples (interleaved) aus Byte-Offset berechnen
                int bytesPerFrame = Math.Max(1, this.Channels) * sizeof(float);
                long startSampleIndex = this.StartingOffset > 0 ? this.StartingOffset : 0;
                // Konsistente Ausgangsposition setzen (Bytes)
                long startFrames = Math.Max(0, this.StartingOffset > 0 ? this.StartingOffset / Math.Max(1, this.Channels) : 0);
                this.SkippedPositionBytes = startFrames * bytesPerFrame;

                // Player-Event binden (einmalig robust)
                EventHandler<StoppedEventArgs>? handler = null;
                handler = (sender, args) =>
                {
                    try { onPlaybackStopped?.Invoke(); }
                    finally
                    {
                        this.Playing = false;
                        this.Paused = false;
                        try { this.playback.PlaybackStopped -= handler!; } catch { }
                    }
                };
                this.playback.PlaybackStopped += handler;

                using (cancellationToken.Register(this.playback.Stop))
                {
                    // ✅ Loop-Grenzen weitergeben (in Samples/Floats, nicht Frames!)
                    await this.playback.InitializePlayback(
                        data: this.Data,
                        sampleRate: this.SampleRate,
                        channels: this.Channels,
                        startSampleIndex: startSampleIndex,
                        deviceSampleRate: this.SampleRate,
                        desiredLatency: desiredLatency,
                        initialVolume: initialVolume.Value,
                        loopStartSample: loopStartSample,
                        loopEndSample: loopEndSample);

                    // Rate anwenden (Varispeed)
                    if (Math.Abs(this.SampleRateFactor - 1.0) > double.Epsilon)
                    {
                        await this.playback.AdjustSampleRate((float) this.SampleRateFactor);
                    }

                    // Baseline setzen
                    this.positionOriginBytes = this.playback.GetPositionBytes();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Playback preparation was canceled");
                this.Playing = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Playback initialization failed: {ex.Message}");
                this.Playing = false;
                throw;
            }
        }

        public async Task PauseAsync()
        {
            if (this.PlayerPlaying)
            {
                long gp = 0;
                try { gp = this.playback.GetPositionBytes(); } catch { gp = 0; }
                long delta = gp - this.positionOriginBytes;
                if (delta < 0) { delta = 0; }
                long newAbsolute = this.SkippedPositionBytes + delta;
                int ch = Math.Max(1, this.Channels);
                int bytesPerFrame = ch * sizeof(float);
                long totalFrames = (this.Data?.LongLength ?? 0L) / ch;
                long totalBytes = totalFrames * bytesPerFrame;
                newAbsolute = Math.Clamp(newAbsolute, 0, totalBytes);
                this.SkippedPositionBytes = newAbsolute;
                this.playback.Pause();
                this.Playing = false;
                this.Paused = true;
                this.positionOriginBytes = gp;
                this.pausedBaselineBytes = this.SkippedPositionBytes; // remember start of pause
                this.resumeFromSetPosition = false;
                await Task.CompletedTask;
            }
            else if (this.Paused)
            {
                if (this.resumeFromSetPosition)
                {
                    int ch = Math.Max(1, this.Channels);
                    int bytesPerFrame = ch * sizeof(float);
                    long startFrames = this.SkippedPositionBytes / bytesPerFrame;
                    long startSampleIndex = startFrames * ch;
                    // fast seek by swapping pipeline only
                    this.playback.SeekSamples(startSampleIndex);
                    // Jetzt wirklich wieder starten
                    this.playback.Resume();
                    this.Playing = true;
                    this.Paused = false;
                    try { this.positionOriginBytes = this.playback.GetPositionBytes(); } catch { this.positionOriginBytes = 0; }
                    this.resumeFromSetPosition = false;
                    await Task.CompletedTask;
                    return;
                }
                try { this.positionOriginBytes = this.playback.GetPositionBytes(); } catch { this.positionOriginBytes = 0; }
                this.Playing = true;
                this.Paused = false;
                this.playback.Resume();
                await Task.CompletedTask;
            }
        }

        public async Task StopAsync()
        {
            this.Playing = false;
            this.Paused = false;

            this.playback.Stop();

            this.SkippedPositionBytes = 0;
            this.positionOriginBytes = 0;
            await Task.CompletedTask;
        }

        public void SetVolume(float volume)
        {
            volume = Math.Clamp(volume, 0.0f, 1.0f);
            this.Volume = volume;
            this.playback.SetVolume(volume);
        }

        // ✅ LIVE Loop-Grenzen aktualisieren während Playback läuft!
        public void UpdateLoopBoundsDuringPlayback(long loopStartSample, long loopEndSample)
        {
            this.playback.UpdateLoopBounds(loopStartSample, loopEndSample);
        }

        // ✅ Switch playback pipeline to LOOPING immediately (playing or paused)
        public void EnableLoopNow(long loopStartSample, long loopEndSample)
        {
            int ch = Math.Max(1, this.Channels);
            int bytesPerFrame = ch * sizeof(float);

            // Absolute aktuelle Bytes (ohne Loop-Mapping)
            long absoluteBytes = this.CurrentPlaybackPositionBytes;
            long absoluteFrames = bytesPerFrame > 0 ? absoluteBytes / bytesPerFrame : 0;
            long currentSampleIndex = absoluteFrames * ch;

            // Switch Provider
            this.playback.SwitchToLoop(currentSampleIndex, loopStartSample, loopEndSample);

            // Baselines neu setzen, damit Caret korrekt weiterläuft
            this.SkippedPositionBytes = absoluteFrames * bytesPerFrame;
            try { this.positionOriginBytes = this.playback.GetPositionBytes(); } catch { this.positionOriginBytes = 0; }
        }

        // ✅ Switch playback pipeline to LINEAR immediately (playing or paused)
        public void DisableLoopNow()
        {
            int ch = Math.Max(1, this.Channels);
            int bytesPerFrame = ch * sizeof(float);

            // Absolute aktuelle Bytes (ohne weitere Anpassungen)
            long absoluteBytes = this.CurrentPlaybackPositionBytes;
            long absoluteFrames = bytesPerFrame > 0 ? absoluteBytes / bytesPerFrame : 0;
            long currentSampleIndex = absoluteFrames * ch;

            // Switch Provider
            this.playback.SwitchToLinear(currentSampleIndex);

            // Reset Loop-Frames und Baselines
            long totalFrames = Math.Max(0, (this.Data?.LongLength ?? 0L) / Math.Max(1, this.Channels));
            this.LoopStartFrames = 0;
            this.LoopEndFrames = totalFrames;

            this.SkippedPositionBytes = absoluteFrames * bytesPerFrame;
            try { this.positionOriginBytes = this.playback.GetPositionBytes(); } catch { this.positionOriginBytes = 0; }
        }

        // SetPosition: Frames -> Bytes; BasiseOffset setzen, ggf. nur merken (Seek beim nächsten Play)
        public void SetPosition(long framePosition)
        {
            int channels = Math.Max(1, this.Channels);
            int bytesPerFrame = channels * sizeof(float);
            long totalFrames = (this.Data?.LongLength ?? 0L) / channels;
            long totalBytes = totalFrames * bytesPerFrame;

            long bytePosition = framePosition * (long) bytesPerFrame;
            bytePosition = Math.Clamp(bytePosition, 0, totalBytes);

            this.SkippedPositionBytes = bytePosition;

            // Minimal: Wenn pausiert, beim nächsten Resume neu starten
            if (this.Paused)
            {
                this.resumeFromSetPosition = true;
            }
        }

        public void Seek(double seconds)
        {
            long frames = (long) Math.Round(seconds * this.SampleRate);
            this.SetPosition(frames);
        }

        public async Task AdjustSampleRate(float factor)
        {
            this.SampleRateFactor = factor;

            if (this.PlayerPlaying)
            {
                await this.playback.AdjustSampleRate((float) this.SampleRateFactor);
            }
        }

        // Processing (basic) Methods

        public async Task NormalizeAsync(float maxAmplitude = 1.0f, int maxWorkers = 4)
        {
            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);

            if (this.Data == null || this.Data.Length == 0)
            {
                return;
            }

            // ---- Auswahlbereich bestimmen (in Sample-Indizes in this.Data) ----
            var (selStart, selEnd, hasSelection) = GetSelectionSampleSpan();

            // Wenn keine Auswahl, dann global
            long startIdx = hasSelection ? selStart : 0;
            long endIdx = hasSelection ? selEnd : this.Data.LongLength;

            if (endIdx <= startIdx)
            {
                return;
            }

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = maxWorkers };

            Stopwatch sw = Stopwatch.StartNew();

            // Phase 1: max in [startIdx, endIdx)
            float globalMax = await Task.Run(() =>
            {
                float max = 0f;
                object lockObj = new object();

                Parallel.For((long) startIdx, (long) endIdx, parallelOptions,
                    () => 0f,
                    (i, state, localMax) =>
                    {
                        float v = Math.Abs(this.Data[i]);
                        return v > localMax ? v : localMax;
                    },
                    localMax =>
                    {
                        lock (lockObj)
                        {
                            if (localMax > max)
                            {
                                max = localMax;
                            }
                        }
                    }
                );
                return max;
            }).ConfigureAwait(false);

            if (globalMax == 0f)
            {
                return;
            }

            // Phase 2: nur Auswahl skalieren
            float scale = maxAmplitude / globalMax;
            await Task.Run(() =>
            {
                Parallel.For((long) startIdx, (long) endIdx, parallelOptions, i =>
                {
                    this.Data[i] *= scale;
                });
            }).ConfigureAwait(false);

            sw.Stop();
            this["Normalize"] = sw.Elapsed.TotalMilliseconds;
        }

        public async Task<(long Start, long End)> TrimSilenceAsync(float? threshold = null, int? minSilenceMs = null, int maxWorkers = 4)
        {
            if (this.Data == null || this.Data.Length == 0)
            {
                return (0, 0);
            }

            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);

            // ---- Auswahlbereich bestimmen (in Sample-Indizes in this.Data) ----
            var (selStart, selEnd, hasSelection) = GetSelectionSampleSpan();

            // Wir arbeiten nur auf der Auswahl; ohne Auswahl => gesamtes Array
            long workStart = hasSelection ? selStart : 0;
            long workEnd = hasSelection ? selEnd : this.Data.LongLength;

            if (workEnd <= workStart)
            {
                return (workStart, workEnd);
            }

            // Blockgröße (≈10ms): in SAMPLES (interleaved)
            int blockSize = (int) (this.SampleRate * this.Channels * 0.01); // 10ms pro Block
            if (blockSize <= 0)
            {
                blockSize = Math.Max(1, this.Channels);
            }

            long workLen = workEnd - workStart;
            int numBlocks = (int) Math.Ceiling((double) workLen / blockSize);
            if (numBlocks <= 0)
            {
                return (workStart, workEnd);
            }

            float[] rmsBlocks = new float[numBlocks];

            // Phase 1: RMS pro Block (nur im Auswahlfenster)
            await Task.Run(() =>
            {
                Parallel.For(0, numBlocks, new ParallelOptions { MaxDegreeOfParallelism = maxWorkers }, bi =>
                {
                    long start = workStart + (long) bi * blockSize;
                    long end = Math.Min(start + blockSize, workEnd);

                    double sumOfSquares = 0.0;
                    long count = end - start;

                    for (long s = start; s < end; s++)
                    {
                        sumOfSquares += this.Data[s] * this.Data[s];
                    }

                    rmsBlocks[bi] = count > 0 ? (float) Math.Sqrt(sumOfSquares / count) : 0.0f;
                });
            }).ConfigureAwait(false);

            if (!rmsBlocks.Any())
            {
                return (workStart, workEnd);
            }

            // Phase 2: Threshold (relativ zum max in Auswahl)
            float maxRms = rmsBlocks.Max();
            float finalThreshold = threshold ?? (maxRms * 0.01f); // 1% vom Max

            // Phase 3: minSilenceMs heuristisch, falls nicht gesetzt
            if (minSilenceMs == null)
            {
                List<int> silentDurations = new();

                int currentSilent = 0;
                foreach (float rms in rmsBlocks)
                {
                    if (rms < finalThreshold)
                    {
                        currentSilent++;
                    }
                    else if (currentSilent > 0)
                    {
                        silentDurations.Add(currentSilent);
                        currentSilent = 0;
                    }
                }
                if (currentSilent > 0)
                {
                    silentDurations.Add(currentSilent);
                }

                if (silentDurations.Count == 0)
                {
                    minSilenceMs = 50; // fallback
                }
                else
                {
                    var ordered = silentDurations.OrderBy(x => x).ToArray();
                    int takeCount = Math.Max(1, (int) (ordered.Length * 0.1));
                    double avgTop = ordered.Skip(Math.Max(0, ordered.Length - takeCount)).Average();

                    double blockDurationMs = (1000.0 * blockSize) / (this.SampleRate * this.Channels);
                    minSilenceMs = (int) Math.Clamp(avgTop * blockDurationMs, 20, 1000);
                }
            }

            // minSilenceMs -> Blöcke
            double blockMs = 1000.0 * blockSize / (this.SampleRate * this.Channels);
            int minSilentBlocks = Math.Max(1, (int) Math.Ceiling(minSilenceMs.Value / blockMs));

            // Phase 4: Start/Ende im Blockraum (nur Auswahl)
            int startBlock = 0;
            int endBlock = rmsBlocks.Length - 1;

            // Start: erstes nicht-stilles Segment nach ausreichender Stille
            for (int i = 0; i < rmsBlocks.Length - minSilentBlocks; i++)
            {
                bool isSilentRun = true;
                for (int j = 0; j < minSilentBlocks; j++)
                {
                    if (rmsBlocks[i + j] > finalThreshold)
                    {
                        isSilentRun = false;
                        break;
                    }
                }

                if (!isSilentRun)
                {
                    startBlock = Math.Max(0, i - minSilentBlocks);
                    break;
                }
            }

            // Ende: letztes nicht-stilles Segment vor längerer Stille
            for (int i = rmsBlocks.Length - 1; i >= minSilentBlocks; i--)
            {
                bool isSilentRun = true;
                for (int j = 0; j < minSilentBlocks; j++)
                {
                    if (rmsBlocks[i - j] > finalThreshold)
                    {
                        isSilentRun = false;
                        break;
                    }
                }

                if (!isSilentRun)
                {
                    endBlock = Math.Min(rmsBlocks.Length - 1, i + minSilentBlocks);
                    break;
                }
            }

            if (endBlock <= startBlock)
            {
                return (workStart, workEnd);
            }

            // Zurück in Sample-Indizes — innerhalb der Auswahl
            long startIndex = workStart + (long) startBlock * blockSize;
            long endIndex = workStart + Math.Min((long) (endBlock + 1) * blockSize, workLen);

            startIndex = Math.Clamp(startIndex, workStart, workEnd);
            endIndex = Math.Clamp(endIndex, workStart, workEnd);

            return (startIndex, endIndex);
        }


        public async Task FadeInAsync(long? selectionStart = null, long? selectionEnd = null, bool logarithmic = false, float fadeLowAmplitude = 0.0f, int maxWorkers = 4)
        {
            selectionStart ??= this.SelectionStart;
            selectionStart = selectionStart.Value < 0 ? 0 : selectionStart.Value;
            selectionEnd ??= this.SelectionEnd;
            selectionEnd = selectionEnd.Value < 0 ? this.Length : selectionEnd.Value;

            if (selectionEnd < selectionStart)
            {
                long swap = selectionEnd.Value;
                selectionEnd = selectionStart.Value;
                selectionStart = swap;
            }

            if (this.Data == null || this.Length <= 0 || selectionEnd.Value <= selectionStart.Value || this.Channels <= 0)
            {
                return;
            }

            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);
            // selectionStart / selectionEnd sind in Samples (interleaved). Wir rechnen in Frames (multi-channel-Frame = Channels samples).
            long startSample = selectionStart.Value;
            long endSample = selectionEnd.Value;
            long totalSamples = endSample - startSample;
            long totalFrames = totalSamples / this.Channels; // floor; falls remainder bleibt, werden die letzten Samples nicht vollständig in Frame gerechnet

            if (totalFrames <= 0)
            {
                return;
            }

            // Ensure fadeLowAmplitude is reasonable
            fadeLowAmplitude = float.IsNaN(fadeLowAmplitude) ? 0.0f : fadeLowAmplitude;
            fadeLowAmplitude = Math.Clamp(fadeLowAmplitude, 0f, 1f);

            // Work by frames: each frame has this.Channels consecutive samples starting at sample index frameIndex * Channels + startSample
            await Task.Run(() =>
            {
                Parallel.For(0L, totalFrames, new ParallelOptions { MaxDegreeOfParallelism = maxWorkers }, frameIndex =>
                {
                    // relative position t in [0,1)
                    double t = (double) frameIndex / Math.Max(1.0, (double) (totalFrames - 1));
                    double gain;
                    if (!logarithmic)
                    {
                        // linear ramp from fadeLowAmplitude -> 1.0
                        gain = fadeLowAmplitude + (1.0 - fadeLowAmplitude) * t;
                    }
                    else
                    {
                        // simple perceptual "log-like" curve: ease-in using power 2 (t^2).
                        // t^2 starts slower and ramps faster near the end.
                        double eased = t * t;
                        gain = fadeLowAmplitude + (1.0 - fadeLowAmplitude) * eased;
                    }

                    long sampleBase = startSample + frameIndex * this.Channels;
                    // apply to each channel of the frame
                    for (int ch = 0; ch < this.Channels; ch++)
                    {
                        long idx = sampleBase + ch;
                        if (idx >= 0 && idx < this.Data.LongLength)
                        {
                            // multiply sample by gain
                            this.Data[idx] = (float) (this.Data[idx] * gain);
                        }
                    }
                });
            }).ConfigureAwait(false);
        }

        public async Task FadeOutAsync(long? selectionStart = null, long? selectionEnd = null, bool logarithmic = false, float fadeLowAmplitude = 0.0f, int maxWorkers = 4)
        {
            selectionStart ??= this.SelectionStart;
            selectionStart = selectionStart.Value < 0 ? 0 : selectionStart.Value;
            selectionEnd ??= this.SelectionEnd;
            selectionEnd = selectionEnd.Value < 0 ? this.Length : selectionEnd.Value;

            if (selectionEnd < selectionStart)
            {
                long swap = selectionEnd.Value;
                selectionEnd = selectionStart.Value;
                selectionStart = swap;
            }

            if (this.Data == null || this.Length <= 0 || selectionEnd.Value <= selectionStart.Value || this.Channels <= 0)
            {
                return;
            }

            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);

            long startSample = selectionStart.Value;
            long endSample = selectionEnd.Value;
            long totalSamples = endSample - startSample;
            long totalFrames = totalSamples / this.Channels;

            if (totalFrames <= 0)
            {
                return;
            }

            fadeLowAmplitude = float.IsNaN(fadeLowAmplitude) ? 0.0f : fadeLowAmplitude;
            fadeLowAmplitude = Math.Clamp(fadeLowAmplitude, 0f, 1f);

            await Task.Run(() =>
            {
                Parallel.For(0L, totalFrames, new ParallelOptions { MaxDegreeOfParallelism = maxWorkers }, frameIndex =>
                {
                    // For fade out we want gain from 1.0 -> fadeLowAmplitude as frameIndex increases
                    double t = (double) frameIndex / Math.Max(1.0, (double) (totalFrames - 1));
                    double gain;
                    if (!logarithmic)
                    {
                        // linear: 1 -> fadeLow
                        gain = 1.0 - (1.0 - fadeLowAmplitude) * t;
                    }
                    else
                    {
                        // ease-out variant: use (1 - (t^2)) so high values at start, faster drop near the end
                        double eased = 1.0 - (t * t);
                        gain = fadeLowAmplitude + (1.0 - fadeLowAmplitude) * eased;
                    }

                    long sampleBase = startSample + frameIndex * this.Channels;
                    for (int ch = 0; ch < this.Channels; ch++)
                    {
                        long idx = sampleBase + ch;
                        if (idx >= 0 && idx < this.Data.LongLength)
                        {
                            this.Data[idx] = (float) (this.Data[idx] * gain);
                        }
                    }
                });
            }).ConfigureAwait(false);
        }





        public async Task<float[]> ConvertToMonoAsync(bool set = false, int maxWorkers = 4)
        {
            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);

            if (this.Data == null || this.Data.Length == 0 || this.Channels <= 0)
            {
                return [];
            }

            // Calculate the number of samples in mono
            int monoSampleCount = this.Data.Length / this.Channels;
            float[] monoData = new float[monoSampleCount];

            await Task.Run(() =>
            {
                Parallel.For(0, monoSampleCount, new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxWorkers
                }, i =>
                {
                    float sum = 0.0f;
                    for (int channel = 0; channel < this.Channels; channel++)
                    {
                        sum += this.Data[i * this.Channels + channel];
                    }
                    monoData[i] = sum / this.Channels;
                });
            });

            if (set)
            {
                this.Data = monoData;
                this.Channels = 1;
            }

            return monoData;
        }

        public async Task<float[]> GetCurrentWindowAsync(int windowSize = 65536, int lookingRange = 2, bool mono = false, bool lookBackwards = false)
        {
            if (this.Data == null || this.Data.Length == 0 || this.SampleRate <= 0 || this.Channels <= 0)
            {
                return [];
            }

            // Parameter normalisieren
            windowSize = Math.Max(1, windowSize);
            lookingRange = Math.Max(1, lookingRange);
            // Für spätere FFTs oft sinnvoll: auf Potenz von 2 schnappen
            windowSize = (int) Math.Pow(2, Math.Ceiling(Math.Log(windowSize, 2)));

            // Position in Frames (position ist bereits frame-basiert)
            long posFrames = this.Position;

            // Fensterbreite in Frames: um pos herum jeweils die Hälfte
            int halfWindowFrames = (windowSize * lookingRange) / 2;
            int fullWindowFrames = halfWindowFrames * 2;
            if (fullWindowFrames <= 0)
            {
                return [];
            }

            if (mono)
            {
                // Mono-Daten (Frames == Samples)
                float[] data = await this.ConvertToMonoAsync(false);
                if (data.Length == 0)
                {
                    return [];
                }

                long startFrame = posFrames - (lookBackwards ? halfWindowFrames : 0);
                long endFrameExclusive = startFrame + fullWindowFrames;

                // Out-of-bounds vermeiden, verschieben
                while (endFrameExclusive > data.Length)
                {
                    startFrame -= windowSize;
                    endFrameExclusive -= windowSize;
                }

                while (startFrame < 0)
                {
                    startFrame += windowSize;
                    endFrameExclusive += windowSize;
                }

                if (endFrameExclusive > data.LongLength)
                {
                    return [];
                }

                float[] current = new float[fullWindowFrames];
                await Task.Run(() => Array.Copy(data, (int) startFrame, current, 0, fullWindowFrames));
                return current;
            }
            else
            {
                // Interleaved Mehrkanal-Daten (Floats)
                float[] data = this.Data;

                long startFloatIndex = (posFrames - (lookBackwards ? halfWindowFrames : 0)) * this.Channels;
                long endFloatIndexExclusive = startFloatIndex + ((long) fullWindowFrames * this.Channels);

                while (endFloatIndexExclusive > data.Length)
                {
                    startFloatIndex -= windowSize * this.Channels;
                    endFloatIndexExclusive -= windowSize * this.Channels;
                }

                while (startFloatIndex < 0)
                {
                    startFloatIndex += windowSize * this.Channels;
                    endFloatIndexExclusive += windowSize * this.Channels;
                }

                if (endFloatIndexExclusive > data.LongLength || startFloatIndex < 0)
                {
                    Debug.WriteLine("GetCurrentWindow: Out of bounds access prevented.");
                    // Out-of-bounds, verschieben
                    return [];
                }

                int lengthFloats = fullWindowFrames * this.Channels;
                float[] current = new float[lengthFloats];
                await Task.Run(() => Array.Copy(data, (int) startFloatIndex, current, 0, lengthFloats));
                return current;
            }
        }

        [SupportedOSPlatform("windows")]
        public async Task<Bitmap> DrawWaveformAsync(int width, int height, int samplesPerPixel = 128, bool drawEachChannel = false, int caretWidth = 1, long? offset = null, Color? waveColor = null, Color? backColor = null, Color? caretColor = null, Color? selectionColor = null, bool smoothen = false, double timingMarkersInterval = 0, float caretPosition = 0.0f, int maxWorkers = 2)
        {
            maxWorkers = Math.Clamp(maxWorkers, 1, Environment.ProcessorCount);
            waveColor ??= Color.Black;
            backColor ??= Color.White;
            caretColor ??= Color.Red;
            selectionColor ??= Color.FromArgb(64, waveColor.Value);

            width = Math.Max(1, width);
            height = Math.Max(1, height);
            samplesPerPixel = samplesPerPixel <= 0 ? this.CalculateSamplesPerPixelToFit(width) : samplesPerPixel;
            caretWidth = Math.Clamp(caretWidth, 0, width);

            long totalFrames = Math.Max(1, this.Length / Math.Max(1, this.Channels));
            long viewFrames = (long) width * samplesPerPixel;

            // Caret + View-Start bestimmen
            long caretFrame = this.Position > 0 ? this.Position : this.StartingOffset / Math.Max(1, this.Channels);
            caretPosition = Math.Clamp(caretPosition, 0f, 1f);
            long caretInViewFrames = (long) Math.Round(viewFrames * caretPosition);
            long maxOffset = Math.Max(0, totalFrames - viewFrames);

            // WICHTIG: offset strikt respektieren; nur bei null rezentrieren
            long viewStartFrames = offset.HasValue
                ? Math.Clamp(offset.Value, 0, maxOffset)
                : Math.Clamp(caretFrame - caretInViewFrames, 0, maxOffset);

            var bitmap = new Bitmap(width, height);

            int channelsToDraw = drawEachChannel ? this.Channels : 1;
            var minMaxPerChannel = new (int yTop, int yBottom)[channelsToDraw][];
            for (int c = 0; c < channelsToDraw; c++)
            {
                minMaxPerChannel[c] = new (int, int)[width];
            }

            await Task.Run(() =>
            {
                var po = new ParallelOptions { MaxDegreeOfParallelism = maxWorkers };
                int blockSize = 64;

                const int targetSamplesPerPixelBudget = 512;
                int stride = Math.Max(1, (int) Math.Ceiling((double) samplesPerPixel / targetSamplesPerPixelBudget));

                var data = this.Data!;
                long dataLength = data.LongLength;
                int channels = this.Channels;

                Parallel.For(0, (width + blockSize - 1) / blockSize, po, blockIndex =>
                {
                    int xStart = blockIndex * blockSize;
                    int xEnd = Math.Min(width, xStart + blockSize);

                    for (int x = xStart; x < xEnd; x++)
                    {
                        long sampleStart = viewStartFrames * channels + (long) x * samplesPerPixel * channels;

                        for (int ch = 0; ch < channelsToDraw; ch++)
                        {
                            float min = float.MaxValue;
                            float max = float.MinValue;

                            long sampleEnd = Math.Min(sampleStart + (long) samplesPerPixel * channels, dataLength);

                            for (long idx = sampleStart + ch; idx < sampleEnd; idx += channels * stride)
                            {
                                float val = data[idx];
                                if (val < min)
                                {
                                    min = val;
                                }

                                if (val > max)
                                {
                                    max = val;
                                }
                            }

                            if (min == float.MaxValue)
                            {
                                min = 0f;
                            }

                            if (max == float.MinValue)
                            {
                                max = 0f;
                            }

                            min = Math.Sign(min) * (float) Math.Sqrt(Math.Abs(min));
                            max = Math.Sign(max) * (float) Math.Sqrt(Math.Abs(max));

                            int channelHeight = height / channelsToDraw;
                            int centerY = channelHeight / 2 + ch * channelHeight;

                            int yTopPixel = centerY - (int) (max * channelHeight / 2f);
                            int yBottomPixel = centerY - (int) (min * channelHeight / 2f);

                            if (yTopPixel > yBottomPixel)
                            {
                                yTopPixel = yBottomPixel;
                            }

                            if (yTopPixel == yBottomPixel)
                            {
                                yBottomPixel = yTopPixel + 1;
                            }

                            minMaxPerChannel[ch][x] = (yTopPixel, yBottomPixel);
                        }
                    }
                });
            });

            using var g = Graphics.FromImage(bitmap);
            g.Clear(backColor.Value);

            using var waveBrush = new SolidBrush(waveColor.Value);
            for (int ch = 0; ch < channelsToDraw; ch++)
            {
                var topPoints = new PointF[width];
                var bottomPoints = new PointF[width];

                int channelHeight = height / channelsToDraw;
                int centerY = channelHeight / 2 + ch * channelHeight;

                for (int x = 0; x < width; x++)
                {
                    var (yTop, yBottom) = minMaxPerChannel[ch][x];
                    if (yTop == 0 && yBottom == 0)
                    {
                        yTop = yBottom = centerY;
                    }

                    if (yTop > yBottom)
                    {
                        yTop = yBottom;
                    }

                    if (yTop == yBottom)
                    {
                        yBottom = yTop + 1;
                    }

                    topPoints[x] = new PointF(x, yTop);
                    bottomPoints[x] = new PointF(x, yBottom);
                }

                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddLines(topPoints);
                path.AddLines(bottomPoints.Reverse().ToArray());
                path.CloseFigure();
                g.FillPath(waveBrush, path);
            }

            // Selection Overlay – referenziert viewStartFrames
            long selStart = this.SelectionStart;
            long selEnd = this.SelectionEnd;
            if (this.Channels > 0 && selStart >= 0 && selEnd >= 0 && selStart != selEnd)
            {
                if (selEnd < selStart)
                {
                    (selStart, selEnd) = (selEnd, selStart);
                }

                long selStartFrames = selStart / this.Channels;
                long selEndFrames = selEnd / this.Channels;
                long viewEnd = viewStartFrames + viewFrames;

                long highlightStartFrames = Math.Max(viewStartFrames, selStartFrames);
                long highlightEndFrames = Math.Min(viewEnd, selEndFrames);

                if (highlightEndFrames > highlightStartFrames)
                {
                    double invSPP = 1.0 / samplesPerPixel;
                    int x1 = (int) Math.Floor((highlightStartFrames - viewStartFrames) * invSPP);
                    int x2 = (int) Math.Ceiling((highlightEndFrames - viewStartFrames) * invSPP);

                    int rectX = Math.Clamp(x1, 0, width);
                    int rectW = Math.Clamp(x2 - rectX, 0, width - rectX);

                    if (rectW > 0)
                    {
                        using var selBrush = new SolidBrush(selectionColor.Value);
                        g.FillRectangle(selBrush, rectX, 0, rectW, height);
                    }
                }
            }

            // Caret
            if (caretWidth > 0)
            {
                using var caretPen = new Pen(caretColor.Value, caretWidth);
                double relX = (double) (caretFrame - viewStartFrames) / viewFrames;
                int caretX = (int) Math.Round(relX * (width - 1));
                if (caretX >= 0 && caretX < width)
                {
                    g.DrawLine(caretPen, caretX, 0, caretX, height);
                }
            }

            // Optional glattzeichnen
            if (smoothen)
            {
                var smoothBitmap = new Bitmap(width, height);
                using (var gSmooth = Graphics.FromImage(smoothBitmap))
                {
                    gSmooth.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    gSmooth.DrawImage(bitmap, 0, 0, width, height);
                }
                bitmap.Dispose();
                bitmap = smoothBitmap;
            }

            // Timing-Marker mit korrektem viewStartFrames
            if (timingMarkersInterval > 0)
            {
                Color inverseGraphColor = Color.FromArgb(255 - waveColor.Value.R, 255 - waveColor.Value.G, 255 - waveColor.Value.B);
                bitmap = await this.DrawTimingMarkersAsync(bitmap, samplesPerPixel, timingMarkersInterval, inverseGraphColor, false, viewStartFrames).ConfigureAwait(false);
            }

            return bitmap;
        }

        [SupportedOSPlatform("windows")]
        public async Task<Bitmap> DrawTimingMarkersAsync(Bitmap waveForm, int samplesPerPixel, double interval = 1, Color? color = null, bool drawTimes = false, long offsetFrames = 0)
        {
            color ??= Color.Gray;

            // Draw timing markers on existing bitmap considering current view offset & samplesPerPixel
            return await Task.Run(() =>
            {
                int width = waveForm.Width;
                int height = waveForm.Height;
                using (var g = Graphics.FromImage(waveForm))
                using (var pen = new Pen(color.Value))
                using (var font = new Font("Arial", 10))
                using (var brush = new SolidBrush(color.Value))
                {
                    double invSPP = 1.0 / Math.Max(1, samplesPerPixel);
                    long intervalFrames = (long) Math.Round(interval * Math.Max(1, this.SampleRate));
                    if (intervalFrames <= 0)
                    {
                        return waveForm;
                    }

                    double intervalInPixels = intervalFrames * invSPP;
                    // Erste Markierung relativ zum View-Start (offsetFrames)
                    long remainder = offsetFrames % intervalFrames;
                    double firstMarkerX = remainder == 0 ? 0.0 : (intervalFrames - remainder) * invSPP;
                    for (double x = firstMarkerX; x < width; x += intervalInPixels)
                    {
                        if (x >= 0 && x < width)
                        {
                            g.DrawLine(pen, (float) x, 0, (float) x, height);

                            if (drawTimes)
                            {
                                double seconds = (offsetFrames + (x * samplesPerPixel)) / (double) this.SampleRate;
                                TimeSpan time = TimeSpan.FromSeconds(seconds);
                                string timeLabel = time.ToString(@"mm\:ss");
                                g.DrawString(timeLabel, font, brush, (float) x + 2, 2);
                            }
                        }
                    }
                }
                return waveForm;
            }).ConfigureAwait(false);
        }

        // Info Methods
        public string GetInfoString(bool formatted = true)
        {
            List<string> infoLines = [
                $"{(this.SampleRate / 1000.0f):F1} Hz, {this.Channels} ch., {this.BitDepth} bits",
                $"Duration: {this.Duration:h\\:mm\\:ss\\.fff}",
                $"({this.Length} f32 ≙ {(this.SizeInKb / 1024.0f):F2} MB)",
                $"BPM-Tag: {this.Bpm:F3}",
                $"BPM Scanned: {this.ScannedBpm:F3}",
                ];

            return formatted ? string.Join(Environment.NewLine, infoLines) : string.Join(" | ", infoLines);
        }

        public string GetMetricsString(bool formatted = true)
        {
            if (this.Metrics.Count <= 0)
            {
                return "No metrics available.";
            }
            List<string> metricLines = [];
            foreach (var kvp in this.Metrics)
            {
                metricLines.Add($"{kvp.Key}: {kvp.Value:F2} ms");
            }

            return formatted ? string.Join(Environment.NewLine, metricLines) : string.Join(" | ", metricLines);
        }

        // Private Methods
        private static IEnumerable<float> ReadAllSamplesStreaming(AudioFileReader reader)
        {
            const int BlockSeconds = 1;
            int blockSize = reader.WaveFormat.SampleRate * reader.WaveFormat.Channels * BlockSeconds;
            float[] buffer = new float[blockSize];
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    yield return buffer[i];
                }
            }
        }

        private int CalculateSamplesPerPixelToFit(int width)
        {
            if (this.Data == null || this.Data.Length == 0 || width <= 0)
            {
                return 1;
            }
            int totalSamples = this.Data.Length / this.Channels;
            int samplesPerPixel = (int) Math.Ceiling((double) totalSamples / width);

            return Math.Max(1, samplesPerPixel);
        }


        private (long selStart, long selEnd, bool hasSelection) GetSelectionSampleSpan()
        {
            if (this.Data == null || this.Data.Length == 0 || this.Channels <= 0)
            {
                return (0, 0, false);
            }

            // WICHTIG:
            // Annahme: SelectionStart/End sind FRAME-Indizes (pro Kanalgruppe).
            // Falls deine Auswahl bereits "Sample"-Indizes (im float[]) sind:
            // -> selectionIsFrameIndices = false
            // const bool selectionIsFrameIndices = false;

            bool hasSelection = this.SelectionStart >= 0 && this.SelectionEnd >= 0 && this.SelectionEnd > this.SelectionStart;
            if (!hasSelection)
            {
                return (0, 0, false);
            }

            long selStart = this.SelectionStart;
            long selEnd = this.SelectionEnd;

            // Entfernt: if (selectionIsFrameIndices) { ... }
            // Der folgende Block ist nie erreichbar, da selectionIsFrameIndices immer false ist.
            // Korrigiert: Bedingung entfernt, Block entfernt.

            // Auf Kanalgrenzen ausrichten
            if (this.Channels > 1)
            {
                selStart = (selStart / this.Channels) * this.Channels;
                selEnd = (selEnd / this.Channels) * this.Channels;
            }

            selStart = Math.Clamp(selStart, 0, this.Data.LongLength);
            selEnd = Math.Clamp(selEnd, 0, this.Data.LongLength);

            if (selEnd <= selStart)
            {
                return (0, 0, false);
            }

            return (selStart, selEnd, true);
        }

        // Fast seek while playing without reinitializing output
        public void FastSeekWhilePlaying(long framePosition)
        {
            int ch = Math.Max(1, this.Channels);
            int bytesPerFrame = ch * sizeof(float);
            long totalFrames = Math.Max(0, (this.Data?.LongLength ?? 0L) / ch);
            long targetFrame = Math.Clamp(framePosition, 0, totalFrames);
            long targetSampleIndex = targetFrame * ch;

            // Switch pipeline to the new position
            this.playback.SeekSamples(targetSampleIndex);

            // Realign baselines so Position maps honestly from new point
            this.SkippedPositionBytes = targetFrame * bytesPerFrame;
            try { this.positionOriginBytes = this.playback.GetPositionBytes(); } catch { this.positionOriginBytes = 0; }
        }

    }
}