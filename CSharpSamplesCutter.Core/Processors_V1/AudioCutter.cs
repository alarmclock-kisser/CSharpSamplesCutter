using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpSamplesCutter.Core.Processors_V1
{
    public static class AudioCutter
    {
        // Represents a detected segment (interleaved sample indexes)
        private readonly record struct Segment(long StartSample, long EndSample, double Energy)
        {
            public long Length => this.EndSample - this.StartSample;
            public double DurationSeconds(int sampleRate, int channels) => (double) this.Length / (sampleRate * channels);
        }

        private sealed class SegmentCluster
        {
            public Segment Representative { get; set; }
            public List<Segment> Members { get; } = [];
            public float[] Fingerprint { get; }
            public SegmentCluster(Segment rep, float[] fp)
            {
                this.Representative = rep;
                this.Fingerprint = fp;
                this.Members.Add(rep);
            }
        }

        // Backward-compatible overload used by Forms project
        public static Task<IEnumerable<AudioObj>> AutoCutSamplesAsync(AudioObj audio, int cutSamplesCount, CancellationToken? ct = null, IProgress<double>? progress = null)
        {
            return AutoCutSamplesAsync(audio, null, null, null, null, cutSamplesCount, ct, progress);
        }

        public static async Task<IEnumerable<AudioObj>> AutoCutSamplesAsync(
            AudioObj audio,
            double? startSeconds = null,
            double? endSeconds = null,
            double? minDuration = null,
            double? maxDuration = null,
            int cutSamplesCount = 4,
            CancellationToken? ct = null,
            IProgress<double>? progress = null)
        {
            CancellationToken token = ct ?? CancellationToken.None;
            progress?.Report(0.0);
            if (audio == null || audio.Data == null || audio.Data.Length == 0 || audio.SampleRate <= 0)
            {
                progress?.Report(1.0);
                return [];
            }

            // Normalize requested seconds range
            startSeconds = startSeconds == null ? 0 : Math.Clamp(startSeconds.Value, 0, audio.Duration.TotalSeconds);
            endSeconds = endSeconds == null ? audio.Duration.TotalSeconds : Math.Clamp(endSeconds.Value, startSeconds.Value, audio.Duration.TotalSeconds);
            if (endSeconds < startSeconds)
            {
                (double? a, double? b) = (endSeconds, startSeconds);
                startSeconds = a; endSeconds = b;
            }

            // Normalize min/max duration (user preference for segment sizing)
            // If not supplied: choose defaults relative to full length (min ~100ms, max ~1/4 duration capped)
            double effectiveMin = minDuration.HasValue
                ? Math.Clamp(minDuration.Value, 0.05, Math.Max(0.05, audio.Duration.TotalSeconds))
                : Math.Clamp(0.1, 0.05, audio.Duration.TotalSeconds / 10.0);
            double effectiveMax = maxDuration.HasValue
                ? Math.Clamp(maxDuration.Value, effectiveMin + 0.05, audio.Duration.TotalSeconds)
                : Math.Clamp(Math.Min(8.0, audio.Duration.TotalSeconds / 4.0), effectiveMin + 0.05, audio.Duration.TotalSeconds);
            if (effectiveMax < effectiveMin)
            {
                (effectiveMin, effectiveMax) = (effectiveMax, effectiveMin);
            }

            // Trim audio first (phase ~5%)
            AudioObj work = audio;
            if ((endSeconds - startSeconds) > 0.0 && (endSeconds != audio.Duration.TotalSeconds || startSeconds != 0))
            {
                work = await CreateSampleFromSecondsAsync(audio, startSeconds.Value, endSeconds.Value, 0, true);
            }
            progress?.Report(0.05);
            token.ThrowIfCancellationRequested();

            // Convert to mono for analysis (phase -> 15%)
            float[] mono = await work.ConvertToMonoAsync(false);
            if (mono == null || mono.Length == 0)
            {
                progress?.Report(1.0);
                return [];
            }
            progress?.Report(0.15);
            token.ThrowIfCancellationRequested();

            // Compute envelope & onset curve (phase -> 55%)
            var envelope = await ComputeEnvelopeAsync(mono, work.SampleRate, token, p => progress?.Report(0.15 + 0.40 * p));
            token.ThrowIfCancellationRequested();
            // Detect rough segments using user-specified duration bounds
            var rawSegments = await DetectSegmentsAsync(envelope, work.SampleRate, minSeconds: effectiveMin, maxSeconds: effectiveMax, token: token);
            progress?.Report(0.55);
            token.ThrowIfCancellationRequested();
            if (rawSegments.Count == 0)
            {
                progress?.Report(1.0);
                return [];
            }

            // Cluster similar segments (phase -> 75%)
            var clusters = await ClusterSegmentsAsync(mono, rawSegments, work.SampleRate, similarityThreshold: 0.90f, token: token, progress: p => progress?.Report(0.55 + 0.20 * p));
            token.ThrowIfCancellationRequested();
            progress?.Report(0.75);

            // Branch A: explicit count requested -> representatives (no duration enforcement beyond detection bounds)
            if (cutSamplesCount > 0)
            {
                var selected = SelectRepresentativeSegments(clusters, cutSamplesCount);
                progress?.Report(0.95);
                token.ThrowIfCancellationRequested();

                List<AudioObj> resultA = new(selected.Count);
                int idxA = 1;
                foreach (var seg in selected)
                {
                    AudioObj s = await CreateSampleFromIndexesAsync(work, seg.StartSample * work.Channels, seg.EndSample * work.Channels, idxA);
                    // Annotate name with segment duration for clarity
                    double segDur = seg.DurationSeconds(work.SampleRate, work.Channels);
                    s.Name = $"{work.Name}_SEG{idxA:D2}_{segDur:0.000}s";
                    resultA.Add(s);
                    idxA++;
                }
                progress?.Report(1.0);
                return resultA;
            }

            // Branch B: auto generation honoring effectiveMin/effectiveMax as preference window
            // Choose preferred duration mid-point (can be biased later if needed)
            double preferredSmallSec = Math.Clamp((effectiveMin + effectiveMax) / 2.0, effectiveMin, effectiveMax);
            long monoTotal = work.Data.LongLength / Math.Max(1, work.Channels);
            var smallCandidates = BuildSmallSegmentsFromSegments(rawSegments, work.SampleRate, work.Channels, monoTotal, effectiveMin, effectiveMax, preferredSmallSec);
            var smallClusters = await ClusterSegmentsAsync(mono, smallCandidates, work.SampleRate, similarityThreshold: 0.95f, token: token);
            var finalSmall = SelectRepresentativeSegments(smallClusters, smallClusters.Count);
            int maxAuto = 32;
            var limited = finalSmall.Take(Math.Min(maxAuto, finalSmall.Count)).ToList();
            progress?.Report(0.95);
            token.ThrowIfCancellationRequested();

            List<AudioObj> result = new(limited.Count);
            int index = 1;
            foreach (var seg in limited)
            {
                AudioObj s = await CreateSampleFromIndexesAsync(work, seg.StartSample * work.Channels, seg.EndSample * work.Channels, index);
                double segDur = seg.DurationSeconds(work.SampleRate, work.Channels);
                s.Name = $"{work.Name}_AUTO{index:D2}_{segDur:0.000}s";
                result.Add(s);
                index++;
            }

            LogCollection.Log($"Created {result.Count} auto-cut samples from '{audio.Name}'");
            progress?.Report(1.0);
            return result;
        }

        // Envelope via short-time energy + smoothing
        private static async Task<double[]> ComputeEnvelopeAsync(float[] mono, int sampleRate, CancellationToken token, Action<double>? progress = null)
        {
            int window = Math.Clamp(sampleRate / 200, 32, 4096); // ~5ms
            int hop = window / 2;
            int frames = (mono.Length - window + hop) / hop;
            double[] env = new double[Math.Max(1, frames)];
            await Task.Run(() =>
            {
                for (int i = 0; i < frames; i++)
                {
                    token.ThrowIfCancellationRequested();
                    int start = i * hop;
                    double sum = 0;
                    for (int j = 0; j < window; j++)
                    {
                        float v = mono[start + j];
                        sum += v * v;
                    }
                    env[i] = sum / window;
                    if ((i & 0x3F) == 0)
                    {
                        progress?.Invoke(i / (double) frames * 0.7); // coarse progress
                    }
                }
                // Smooth (moving average)
                int smooth = 8;
                if (smooth > 1)
                {
                    double[] copy = new double[env.Length];
                    for (int i = 0; i < env.Length; i++)
                    {
                        int from = Math.Max(0, i - smooth);
                        int to = Math.Min(env.Length - 1, i + smooth);
                        double s = 0; int c = 0;
                        for (int k = from; k <= to; k++) { s += env[k]; c++; }
                        copy[i] = s / c;
                        if ((i & 0x3F) == 0)
                        {
                            progress?.Invoke(0.7 + i / (double) env.Length * 0.3);
                        }
                    }
                    env = copy;
                }
            }, token);
            return env;
        }

        // Basic onset/segment detection based on envelope rising edges & local minima separation
        private static async Task<List<Segment>> DetectSegmentsAsync(double[] envelope, int sampleRate, double minSeconds, double maxSeconds, CancellationToken token)
        {
            return await Task.Run(() =>
            {
                List<Segment> segments = [];
                if (envelope.Length < 4)
                {
                    return segments;
                }
                // Normalize envelope
                double max = envelope.Max();
                if (max <= 0)
                {
                    return segments;
                }

                double[] envNorm = envelope.Select(v => v / max).ToArray();
                // Find onsets by threshold & derivative
                double threshold = 0.15; // relative energy
                int minBins = (int) Math.Max(1, minSeconds * sampleRate / (sampleRate / 200.0 * 0.5));
                int maxBins = (int) Math.Max(minBins + 1, maxSeconds * sampleRate / (sampleRate / 200.0 * 0.5));
                List<int> onsetBins = [];
                for (int i = 2; i < envNorm.Length - 2; i++)
                {
                    double diff = envNorm[i] - envNorm[i - 1];
                    if (diff > 0.02 && envNorm[i] > threshold)
                    {
                        if (onsetBins.Count == 0 || i - onsetBins[^1] > minBins)
                        {
                            onsetBins.Add(i);
                        }
                    }
                }
                if (onsetBins.Count == 0)
                {
                    onsetBins.Add(0);
                }
                // Build segments as ranges until next onset or max length
                for (int i = 0; i < onsetBins.Count; i++)
                {
                    int startBin = onsetBins[i];
                    int endBin = (i + 1 < onsetBins.Count) ? onsetBins[i + 1] : Math.Min(envNorm.Length - 1, startBin + maxBins);
                    int binsLen = Math.Clamp(endBin - startBin, minBins, maxBins);
                    long startSample = ConvertBinToSample(startBin, sampleRate);
                    long endSample = ConvertBinToSample(startBin + binsLen, sampleRate);
                    if (endSample - startSample <= 0)
                    {
                        continue;
                    }

                    double energy = envNorm.Skip(startBin).Take(binsLen).Sum();
                    segments.Add(new Segment(startSample, endSample, energy));
                }
                // Merge very short consecutive segments
                long minSamples = (long) (minSeconds * sampleRate);
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    if (segments[i].Length < minSamples)
                    {
                        var merged = new Segment(segments[i].StartSample, segments[i + 1].EndSample, segments[i].Energy + segments[i + 1].Energy);
                        segments[i] = merged;
                        segments.RemoveAt(i + 1);
                        i--;
                    }
                }
                return segments;
            }, token);
            static long ConvertBinToSample(int bin, int sampleRate)
            {
                int window = Math.Clamp(sampleRate / 200, 32, 4096);
                int hop = window / 2;
                long samples = (long) bin * hop + window; // approximate center
                return samples;
            }
        }

        private static async Task<List<SegmentCluster>> ClusterSegmentsAsync(float[] mono, List<Segment> segments, int sampleRate, float similarityThreshold, CancellationToken token, Action<double>? progress = null)
        {
            if (segments.Count == 0)
            {
                return [];
            }

            return await Task.Run(() =>
            {
                List<SegmentCluster> clusters = [];
                int fingerprintSize = 128;
                int total = segments.Count;
                int processed = 0;
                foreach (var seg in segments)
                {
                    token.ThrowIfCancellationRequested();
                    var fp = BuildFingerprint(mono, seg, fingerprintSize);
                    SegmentCluster? best = null;
                    float bestSim = 0f;
                    foreach (var c in clusters)
                    {
                        float sim = Cosine(c.Fingerprint, fp);
                        if (sim > bestSim)
                        {
                            bestSim = sim; best = c;
                        }
                    }
                    if (best != null && bestSim >= similarityThreshold)
                    {
                        best.Members.Add(seg);
                        // adjust representative to longest, or highest energy
                        if (seg.Energy > best.Representative.Energy)
                        {
                            best.Representative = seg;
                        }
                    }
                    else
                    {
                        clusters.Add(new SegmentCluster(seg, fp));
                    }
                    processed++;
                    if ((processed & 0x3) == 0)
                    {
                        progress?.Invoke(processed / (double) total);
                    }
                }
                progress?.Invoke(1.0);
                return clusters;
            }, token);
        }

        private static float[] BuildFingerprint(float[] mono, Segment seg, int size)
        {
            long start = Math.Clamp(seg.StartSample, 0, mono.LongLength - 1);
            long end = Math.Clamp(seg.EndSample, start + 1, mono.LongLength);
            long len = end - start;
            float[] fp = new float[size];
            if (len <= size)
            {
                int intLen = (int) len;
                int iStart = (int) start;
                for (int i = 0; i < intLen; i++)
                {
                    fp[i] = mono[iStart + i];
                }
            }
            else
            {
                double step = len / (double) size;
                int iStart = (int) start;
                for (int i = 0; i < size; i++)
                {
                    int idx = iStart + (int) (i * step);
                    fp[i] = mono[idx];
                }
            }
            // Normalize
            float max = fp.Select(Math.Abs).DefaultIfEmpty(0f).Max();
            if (max > 0)
            {
                for (int i = 0; i < fp.Length; i++)
                {
                    fp[i] /= max;
                }
            }
            return fp;
        }

        private static float Cosine(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                return 0f;
            }

            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na == 0 || nb == 0)
            {
                return 0f;
            }

            return (float) (dot / Math.Sqrt(na * nb));
        }

        private static List<Segment> SelectRepresentativeSegments(List<SegmentCluster> clusters, int count)
        {
            // Order clusters by representative energy, then length diversity
            var ordered = clusters
                .OrderByDescending(c => c.Representative.Energy)
                .ThenByDescending(c => c.Representative.Length)
                .ToList();
            return ordered.Select(c => c.Representative).Take(Math.Max(0, count)).ToList();
        }

        // Build small segments (0.5s - 2s) by trimming/expanding/splitting detected segments
        private static List<Segment> BuildSmallSegmentsFromSegments(List<Segment> segments, int sampleRate, int channels, long monoTotalSamples, double minSeconds, double maxSeconds, double preferredSeconds)
        {
            long minSamples = (long) Math.Round(minSeconds * sampleRate);
            long maxSamples = (long) Math.Round(maxSeconds * sampleRate);
            long preferredSamples = (long) Math.Round(Math.Clamp(preferredSeconds * sampleRate, minSamples, maxSamples));
            List<Segment> output = [];

            foreach (var seg in segments.OrderBy(s => s.StartSample))
            {
                long segLen = seg.Length;
                // segLen is in mono samples
                if (segLen <= 0)
                {
                    continue;
                }

                if (segLen < minSamples)
                {
                    long start = seg.StartSample;
                    long end = Math.Min(start + preferredSamples, monoTotalSamples);
                    long len = end - start;
                    if (len < minSamples)
                    {
                        long shortage = minSamples - len;
                        long expandBack = shortage / 2;
                        long newStart = Math.Max(0, start - expandBack);
                        long newEnd = Math.Min(monoTotalSamples, newStart + minSamples);
                        output.Add(new Segment(newStart, newEnd, seg.Energy));
                    }
                    else
                    {
                        output.Add(new Segment(start, end, seg.Energy));
                    }
                }
                else if (segLen <= maxSamples)
                {
                    // Center a preferred-sized window if segment is a bit larger
                    if (segLen > preferredSamples)
                    {
                        long offset = (segLen - preferredSamples) / 2;
                        long start = seg.StartSample + offset;
                        output.Add(new Segment(start, start + preferredSamples, seg.Energy));
                    }
                    else
                    {
                        output.Add(seg);
                    }
                }
                else
                {
                    // Split long segments into multiple preferred-sized windows (limit per long segment)
                    int maxPerLongSeg = 4;
                    long start = seg.StartSample;
                    int created = 0;
                    while (created < maxPerLongSeg)
                    {
                        long s = start + created * preferredSamples;
                        long e = s + preferredSamples;
                        long segEnd = seg.EndSample;
                        if (e > segEnd)
                        {
                            break;
                        }

                        output.Add(new Segment(s, e, seg.Energy));
                        created++;
                    }
                }
            }

            // De-duplicate overlapping windows that are extremely close
            output = output
                .OrderBy(s => s.StartSample)
                .Aggregate(new List<Segment>(), (list, s) =>
                {
                    if (list.Count == 0) { list.Add(s); return list; }
                    var last = list[^1];
                    if (s.StartSample <= last.EndSample && (s.EndSample - last.StartSample) < (long) (0.2 * preferredSamples))
                    {
                        // too similar/overlapping, keep the earlier/longer
                        if (s.Length > last.Length)
                        {
                            list[^1] = s;
                        }
                    }
                    else
                    {
                        list.Add(s);
                    }

                    return list;
                });

            return output;
        }

        // Existing helper: create sample from raw indices (interleaved float index)
        internal static async Task<AudioObj> CreateSampleFromIndexesAsync(AudioObj original, long startIndex, long endIndex, int sampleIndex, bool mono = false)
        {
            long lengthSamples = endIndex - startIndex;
            if (lengthSamples <= 0)
            {
                return original;
            }

            float[] data = new float[lengthSamples];
            Array.Copy(original.Data, startIndex, data, 0, lengthSamples);
            try
            {
                return await Task.Run(() =>
                {
                    return new AudioObj
                    {
                        Name = $"{original.Name}_{sampleIndex:D3}",
                        FilePath = original.FilePath,
                        Data = data,
                        SampleRate = original.SampleRate,
                        Channels = original.Channels, // nicht auf 1 setzen, solange nicht wirklich nach Mono konvertiert wird
                        BitDepth = original.BitDepth,
                        Length = data.Length,         // FIX: tatsächliche Länge in interleavten Samples
                        Duration = TimeSpan.FromSeconds((double) data.Length / (original.Channels * original.SampleRate)),
                        Volume = original.Volume,
                        Bpm = original.Bpm > 0 ? original.Bpm : original.ScannedBpm > 0 ? original.ScannedBpm : 0,
                        Timing = original.Timing > 0 ? original.Timing : original.ScannedTiming > 0 ? original.ScannedTiming : 1.0f,
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return original;
            }
        }

        internal static async Task<AudioObj> CreateSampleFromSecondsAsync(AudioObj original, double startSeconds, double endSeconds, int sampleIndex, bool mono = false)
        {
            long startIndex = (long) (startSeconds * original.SampleRate) * original.Channels;
            long endIndex = (long) (endSeconds * original.SampleRate) * original.Channels;
            return await CreateSampleFromIndexesAsync(original, startIndex, endIndex, sampleIndex, mono);
        }





        // Generic processing
        internal static async Task<IEnumerable<AudioObj>> MergeSimilarAudiosAsync(IEnumerable<AudioObj> audios, float? threshold = null, bool mixSimilarAudios = false)
        {
            // Validate inputs
            if (audios == null)
            {
                LogCollection.Log("MergeSimilarAudiosAsync: input is null.");
                return [];
            }

            var list = audios.Where(a => a != null && a.Data != null && a.Data.Length > 0 && a.SampleRate > 0)
                              .DistinctBy(a => a.Id)
                              .ToList();
            if (list.Count <= 1)
            {
                return list;
            }

            LogCollection.Log($"MergeSimilarAudiosAsync: Preparing {list.Count} audio items...");

            // Step 1: Compute mono + fingerprints in parallel (bounded concurrency)
            int concurrency = Math.Max(2, Environment.ProcessorCount - 1);
            var sem = new SemaphoreSlim(concurrency, concurrency);
            int fpSize = 256; // higher resolution for better precision
            var fingerprintMap = new ConcurrentDictionary<Guid, float[]>();
            var infoMap = new ConcurrentDictionary<Guid, (int sampleRate, int channels, long length, string name)>();

            var prepTasks = list.Select(async a =>
            {
                await sem.WaitAsync();
                try
                {
                    float[] mono = await a.ConvertToMonoAsync(false);
                    if (mono == null || mono.Length == 0)
                    {
                        LogCollection.Log($"MergeSimilarAudiosAsync: '{a.Name}' mono conversion yielded no data; skipping.");
                        return;
                    }
                    // Whole-clip segment
                    var seg = new Segment(0, mono.LongLength, mono.Sum(v => Math.Abs(v)));
                    var fp = BuildFingerprint(mono, seg, fpSize);
                    fingerprintMap[a.Id] = fp;
                    infoMap[a.Id] = (a.SampleRate, a.Channels, a.Length, a.Name);
                }
                catch (Exception ex)
                {
                    LogCollection.Log($"MergeSimilarAudiosAsync: Error preparing '{a.Name}': {ex.Message}");
                }
                finally
                {
                    sem.Release();
                }
            });
            await Task.WhenAll(prepTasks);

            var ids = list.Where(a => fingerprintMap.ContainsKey(a.Id)).Select(a => a.Id).ToList();
            if (ids.Count <= 1)
            {
                return list.Where(a => fingerprintMap.ContainsKey(a.Id)).ToList();
            }

            // Step 2: Determine similarity threshold (if not supplied)
            float simThreshold = threshold.HasValue ? Math.Clamp(threshold.Value, 0f, 1f) : AutoDetectSimilarityThreshold(ids, fingerprintMap);
            LogCollection.Log($"MergeSimilarAudiosAsync: Using similarity threshold = {simThreshold:0.000} (input={(threshold.HasValue ? "user" : "auto")}).");

            // Step 3: Build similarity graph and cluster (connected components)
            var adjacency = new Dictionary<Guid, HashSet<Guid>>();
            foreach (var id in ids)
            {
                adjacency[id] = new HashSet<Guid>();
            }

            // Pairwise similarity (parallelized)
            var pairTasks = new List<Task>();
            for (int i = 0; i < ids.Count; i++)
            {
                int ii = i;
                pairTasks.Add(Task.Run(() =>
                {
                    var idA = ids[ii];
                    var fpA = fingerprintMap[idA];
                    for (int j = ii + 1; j < ids.Count; j++)
                    {
                        var idB = ids[j];
                        var fpB = fingerprintMap[idB];
                        float sim = Cosine(fpA, fpB);
                        if (sim >= simThreshold)
                        {
                            lock (adjacency)
                            {
                                adjacency[idA].Add(idB);
                                adjacency[idB].Add(idA);
                            }
                        }
                    }
                }));
            }
            await Task.WhenAll(pairTasks);

            var clusters = FindConnectedComponents(adjacency);
            LogCollection.Log($"MergeSimilarAudiosAsync: Found {clusters.Count} cluster(s).");

            // Step 4: For each cluster -> mix or choose representative
            var idToAudio = list.ToDictionary(a => a.Id, a => a);
            var outputs = new List<AudioObj>();

            foreach (var cluster in clusters)
            {
                try
                {
                    var members = cluster.Where(idToAudio.ContainsKey).Select(id => idToAudio[id]).ToList();
                    if (members.Count == 0) { continue; }

                    if (members.Count == 1)
                    {
                        outputs.Add(members[0]);
                        continue;
                    }

                    if (mixSimilarAudios)
                    {
                        var mixed = await TryMixClusterAsync(members);
                        if (mixed != null)
                        {
                            outputs.Add(mixed);
                            LogCollection.Log($"MergeSimilarAudiosAsync: Mixed cluster of {members.Count} into '{mixed.Name}'.");
                            continue;
                        }
                        else
                        {
                            LogCollection.Log($"MergeSimilarAudiosAsync: Mixing cluster of {members.Count} failed or not possible. Selecting representative instead.");
                        }
                    }

                    // Choose representative with a robust score
                    var rep = ChooseRepresentative(members);
                    outputs.Add(rep);
                    LogCollection.Log($"MergeSimilarAudiosAsync: Cluster of {members.Count} -> representative '{rep.Name}'.");
                }
                catch (Exception ex)
                {
                    LogCollection.Log($"MergeSimilarAudiosAsync: Error in cluster processing: {ex.Message}");
                }
            }

            return outputs;
        }

        // --- Helpers for MergeSimilarAudiosAsync ---
        private static float AutoDetectSimilarityThreshold(List<Guid> ids, ConcurrentDictionary<Guid, float[]> fps)
        {
            try
            {
                var vals = new List<float>();
                // Sample pairs if too many
                int n = ids.Count;
                int maxPairs = Math.Clamp(n * (n - 1) / 2, 1, 2000);
                var rnd = new Random(1337);
                for (int k = 0; k < maxPairs; k++)
                {
                    int i = rnd.Next(0, n);
                    int j = rnd.Next(0, n);
                    if (i == j) { j = (j + 1) % n; }
                    var a = fps[ids[i]];
                    var b = fps[ids[j]];
                    vals.Add(Cosine(a, b));
                }
                if (vals.Count == 0)
                {
                    return 0.92f; // fallback default
                }
                // 2-means on similarity values
                float c1 = vals.Min();
                float c2 = vals.Max();
                for (int iter = 0; iter < 12; iter++)
                {
                    var g1 = new List<float>();
                    var g2 = new List<float>();
                    foreach (var v in vals)
                    {
                        float d1 = Math.Abs(v - c1);
                        float d2 = Math.Abs(v - c2);
                        if (d1 <= d2)
                        {
                            g1.Add(v);
                        }
                        else
                        {
                            g2.Add(v);
                        }
                    }
                    c1 = g1.Count > 0 ? g1.Average() : c1;
                    c2 = g2.Count > 0 ? g2.Average() : c2;
                }
                float thr = (c1 + c2) / 2f;
                // Constrain to a sane band
                thr = Math.Clamp(thr, 0.80f, 0.99f);
                return thr;
            }
            catch
            {
                return 0.92f; // safe default
            }
        }

        private static List<List<Guid>> FindConnectedComponents(Dictionary<Guid, HashSet<Guid>> adjacency)
        {
            var visited = new HashSet<Guid>();
            var comps = new List<List<Guid>>();

            foreach (var node in adjacency.Keys)
            {
                if (visited.Contains(node))
                {
                    continue;
                }

                var comp = new List<Guid>();
                var q = new Queue<Guid>();
                q.Enqueue(node);
                visited.Add(node);
                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    comp.Add(cur);
                    foreach (var nei in adjacency[cur])
                    {
                        if (!visited.Contains(nei))
                        {
                            visited.Add(nei);
                            q.Enqueue(nei);
                        }
                    }
                }
                comps.Add(comp);
            }
            return comps;
        }

        private static AudioObj ChooseRepresentative(List<AudioObj> members)
        {
            // Score: prefer higher sample rate, more channels, longer length, higher RMS energy
            double Score(AudioObj a)
            {
                double len = Math.Max(1, a.Length);
                double rms = 0.0;
                if (a.Data != null && a.Data.Length > 0)
                {
                    // sample up to 200k samples to estimate RMS
                    int step = Math.Max(1, a.Data.Length / 200_000);
                    double sumsq = 0; int cnt = 0;
                    for (int i = 0; i < a.Data.Length; i += step)
                    {
                        float v = a.Data[i];
                        sumsq += v * v; cnt++;
                    }
                    rms = cnt > 0 ? Math.Sqrt(sumsq / cnt) : 0.0;
                }
                return a.SampleRate * (1 + 0.1 * a.Channels) * Math.Log10(10 + len) * (1 + rms);
            }

            return members.OrderByDescending(Score).First();
        }

        private static async Task<AudioObj?> TryMixClusterAsync(List<AudioObj> members)
        {
            if (members == null || members.Count == 0)
            {
                return null;
            }
            // Mix only if sample rate and channels match across members
            int sr = members[0].SampleRate;
            int ch = members[0].Channels;
            if (members.Any(m => m.SampleRate != sr || m.Channels != ch))
            {
                return null; // mixing would require resampling/rechanneling; skip for now
            }

            // Determine max length (interleaved samples)
            long maxLen = members.Max(m => m.Data?.LongLength ?? 0L);
            if (maxLen <= 0)
            {
                return null;
            }

            float[] mixed = new float[maxLen];
            int count = members.Count;

            // Parallel mix with bounded concurrency
            int concurrency = Math.Max(2, Environment.ProcessorCount - 1);
            var sem = new SemaphoreSlim(concurrency, concurrency);
            var mixTasks = members.Select(async m =>
            {
                await sem.WaitAsync();
                try
                {
                    var src = m.Data ?? Array.Empty<float>();
                    int n = src.Length;
                    for (int i = 0; i < n; i++)
                    {
                        // Interlocked for float is not available; accumulate locally then merge? Simpler: lock per chunk.
                        // Use a striped locking approach for better perf.
                        int stripe = (i & 0xFF); // 256 stripes
                        lock (MixLocks[stripe])
                        {
                            mixed[i] += src[i] / count;
                        }
                    }
                }
                finally
                {
                    sem.Release();
                }
            });

            await Task.WhenAll(mixTasks);

            // Normalize if clipping risk
            float maxAbs = mixed.Select(Math.Abs).DefaultIfEmpty(0f).Max();
            if (maxAbs > 1.0f && maxAbs < 1000f)
            {
                float scale = 1.0f / maxAbs;
                for (int i = 0; i < mixed.Length; i++)
                {
                    mixed[i] *= scale;
                }
            }

            // Build new AudioObj
            var first = members[0];
            string mixName = $"MIX_{members.Count:D2}_of_{members.Select(m => m.Name).FirstOrDefault() ?? "sample"}";
            var bpmVals = members.Select(m => m.Bpm > 0 ? m.Bpm : (m.ScannedBpm > 0 ? m.ScannedBpm : 0)).Where(v => v > 0).ToList();
            var timingVals = members.Select(m => m.Timing > 0 ? m.Timing : (m.ScannedTiming > 0 ? m.ScannedTiming : 0)).Where(v => v > 0).ToList();

            var result = await Task.Run(() => new AudioObj
            {
                Name = mixName,
                FilePath = first.FilePath,
                Data = mixed,
                SampleRate = sr,
                Channels = ch,
                BitDepth = first.BitDepth,
                Length = mixed.LongLength,
                Duration = TimeSpan.FromSeconds((double) mixed.LongLength / (sr * Math.Max(1, ch))),
                Volume = members.Average(m => m.Volume),
                Bpm = bpmVals.Count > 0 ? (float) bpmVals.Average() : 0f,
                Timing = timingVals.Count > 0 ? (float) timingVals.Average() : 1.0f,
            });

            return result;
        }

        // 256 striped locks for mixing accumulation
        private static readonly object[] MixLocks = Enumerable.Range(0, 256).Select(_ => new object()).ToArray();
    }
}
