using CSharpSamplesCutter.Core.Processors_V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CSharpSamplesCutter.Core.Processors_V2
{
    public static class AutoDrumLooper
    {
        /// <summary>
        /// Generate a 1-bar amen-style break loop from either a single drum loop (auto-sliced + rearranged)
        /// or from multiple one-shot drum samples (pattern-based). Fully async and parallelized where beneficial.
        /// </summary>
        public static async Task<AudioObj> GenerateLoopAsync(IEnumerable<AudioObj>? input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            var list = input.Where(s => s != null && s.Data != null && s.Data.Length > 0).ToList();
            if (list.Count == 0)
            {
                throw new ArgumentException("No valid audio inputs provided.");
            }

            // Heuristics
            const int bars = 1; // 1 bar, 4 beats
            const double defaultBpmIfUnknown = 170.0; // amen-ish default
            const double swing = 0.06;  // light swing for groove
            const double humanizeMs = 3.0; // very slight humanization

            if (list.Count == 1)
            {
                var src = list[0];

                // Determine BPM and timing if not present
                double bpm = src.Bpm > 0 ? src.Bpm : (src.ScannedBpm > 0 ? src.ScannedBpm : 0.0);
                if (bpm <= 0.0)
                {
                    try { bpm = await BeatScanner.ScanBpmAsync(src, autoGetTiming: true); } catch { bpm = 0.0; }
                }
                if (bpm <= 0.0) bpm = defaultBpmIfUnknown;

                // Slice and classify
                var sliced = await SliceLoopToDrumAtomsAsync(src, bpm);
                if (sliced.Count == 0)
                {
                    sliced = new List<AudioObj> { await ClassifyAndTagAsync(CloneRange(src, 0, (src.Data?.LongLength ?? 0) / Math.Max(1, src.Channels))) };
                }

                // Deterministic amen-style arrangement from atoms (less random, better groove)
                var amen = await ArrangeAmenFromAtomsAsync(sliced, src.SampleRate, src.Channels, bpm, bars).ConfigureAwait(false);
                if (amen != null)
                {
                    return amen;
                }

                // Fallback
                var loops = await DrumLooper.CreateDrumLoopsAsync(sliced, barsCount: bars, loopsCount: 1, bpmOverride: bpm, swing: swing, humanizeMs: humanizeMs);
                var loop = loops.FirstOrDefault();
                if (loop == null)
                {
                    throw new InvalidOperationException("Failed to generate loop from the provided audio.");
                }
                return loop;
            }
            else
            {
                var bpms = list.Select(s => s.Bpm > 0 ? s.Bpm : (s.ScannedBpm > 0 ? s.ScannedBpm : 0.0)).Where(b => b > 0.0).Select(b => (double)b).ToList();
                double bpm = bpms.Count > 0 ? bpms.OrderBy(x => x).ElementAt(bpms.Count / 2) : defaultBpmIfUnknown;

                // Classify tags in parallel
                var taggedTasks = list.Select(l => ClassifyAndTagAsync(l)).ToArray();
                await Task.WhenAll(taggedTasks);
                var tagged = taggedTasks.Select(t => t.Result).ToList();

                // Use DrumEngine for more controlled patterns
                var loops = await DrumEngine.GenerateLoopsAsync(tagged, smallestNote: "1/16", bpm: (float)bpm, bars: bars, count: 1);
                var loop = loops.FirstOrDefault();
                if (loop != null)
                {
                    return loop;
                }

                // Fallback to DrumLooper
                var loops2 = await DrumLooper.CreateDrumLoopsAsync(tagged, barsCount: bars, loopsCount: 1, bpmOverride: bpm, swing: swing, humanizeMs: humanizeMs);
                var loop2 = loops2.FirstOrDefault();
                if (loop2 == null)
                {
                    throw new InvalidOperationException("Failed to generate loop from the provided drum samples.");
                }
                return loop2;
            }
        }

        // Build a classic amen-style pattern deterministically and render it from atoms
        private static async Task<AudioObj?> ArrangeAmenFromAtomsAsync(List<AudioObj> atoms, int sampleRate, int channels, double bpm, int bars)
        {
            if (atoms == null || atoms.Count == 0 || sampleRate <= 0 || channels <= 0) return null;

            // Group by role and rank by RMS (desc)
            var group = atoms.GroupBy(a => (a.SampleTag ?? "").Trim()).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var kicks = RankByRms(group.TryGetValue("Kick", out var k) ? k : []);
            var snares = RankByRms(group.TryGetValue("Snare", out var s) ? s : []);
            var hats = RankByRms(group.TryGetValue("HiHatClosed", out var h) ? h : []);
            if (hats.Count == 0 && group.TryGetValue("HiHatOpen", out var ho)) hats = RankByRms(ho);
            var claps = RankByRms(group.TryGetValue("Clap", out var c) ? c : []);

            // If we don't have core elements, bail to generic engine
            if (kicks.Count == 0 || snares.Count == 0)
            {
                return null;
            }

            long framesPerBeat = (long)Math.Round(sampleRate * 60.0 / Math.Max(1.0, bpm));
            long framesPerStep = Math.Max(1, framesPerBeat / 4); // 1/16
            long stepsPerBar = 16;
            long totalSteps = stepsPerBar * Math.Max(1, bars);
            long totalFrames = totalSteps * framesPerStep;
            int ch = Math.Max(1, channels);

            // Deterministic pattern (indices in 0..15) typical amen flavour
            // K: kick, S: snare, H: hat, G: ghost snare
            var K = new int[] { 0, 6, 8 }; // strong kicks on 1, 1e, 3
            var S = new int[] { 4, 12 };   // snares on 2 and 4
            var G = new int[] { 2, 14 };   // soft ghost snares
            var H = Enumerable.Range(0, 16).Where(i => i % 2 == 0).ToArray(); // 1/8 hats

            // Events (startStep, role, velocity, maxLenSteps)
            var events = new List<(int step, string role, float vel, int lenSteps)>(64);
            foreach (var st in H) events.Add((st, "Hat", 0.55f, 1));
            foreach (var st in K) events.Add((st, "Kick", 1.00f, 2));
            foreach (var st in S) events.Add((st, "Snare", 0.95f, 2));
            foreach (var st in G) events.Add((st, "SnareGhost", 0.45f, 1));

            // Render
            var mix = new float[totalFrames * ch];
            foreach (var ev in events.OrderBy(e => e.step))
            {
                long startFrame = ev.step * framesPerStep;
                long maxLenFrames = ev.lenSteps * framesPerStep;

                AudioObj? pick = null;
                if (ev.role == "Kick") pick = kicks.FirstOrDefault();
                else if (ev.role == "Snare") pick = snares.FirstOrDefault();
                else if (ev.role == "SnareGhost") pick = snares.LastOrDefault() ?? snares.FirstOrDefault();
                else if (ev.role == "Hat") pick = hats.FirstOrDefault() ?? claps.FirstOrDefault() ?? snares.LastOrDefault();

                if (pick == null || pick.Data == null || pick.Data.Length == 0) continue;

                // Determine playable frames (clip to maxLen and keep before next main event to avoid overlap mush)
                long srcFrames = pick.Data.LongLength / Math.Max(1, pick.Channels);
                long playFrames = Math.Min(srcFrames, maxLenFrames);

                // Apply small fades (2ms) to avoid clicks
                long fadeFrames = Math.Max(1, sampleRate / 500); // ~2ms

                // Mix
                MixInterleaved(mix, pick.Data, startFrame, playFrames, ev.vel, ch, Math.Max(1, pick.Channels), fadeFrames);
            }

            // Wrap result
            var loop = new AudioObj
            {
                Id = Guid.NewGuid(),
                Name = $"Amen_{bpm:F0}bpm_{bars}bar",
                Data = mix,
                SampleRate = sampleRate,
                Channels = ch,
                BitDepth = 32,
                Bpm = (float)bpm,
                Length = mix.LongLength,
                Duration = TimeSpan.FromSeconds((double)(mix.LongLength / ch) / sampleRate),
                SampleTag = "Loop"
            };

            // Gentle normalize
            await loop.NormalizeAsync(0.98f);
            return loop;
        }

        private static List<AudioObj> RankByRms(List<AudioObj> items)
        {
            return items.OrderByDescending(a => SafeRms(a)).ToList();
        }

        private static double SafeRms(AudioObj a)
        {
            try
            {
                var d = a.Data;
                if (d == null || d.Length == 0) return 0.0;
                double acc = 0.0; int n = d.Length;
                for (int i = 0; i < n; i++) { double v = d[i]; acc += v * v; }
                return Math.Sqrt(acc / Math.Max(1, n));
            }
            catch { return 0.0; }
        }

        private static void MixInterleaved(float[] dst, float[] src, long startFrame, long playFrames, float gain, int dstCh, int srcCh, long fadeFrames)
        {
            long totalDstFrames = dst.LongLength / Math.Max(1, dstCh);
            if (startFrame >= totalDstFrames) return;
            long frames = Math.Min(playFrames, totalDstFrames - startFrame);
            if (frames <= 0) return;

            long dstBase = startFrame * dstCh;
            for (long f = 0; f < frames; f++)
            {
                double env = 1.0;
                if (f < fadeFrames) env *= (double)f / Math.Max(1, fadeFrames);
                if (frames - f <= fadeFrames) env *= (double)(frames - f) / Math.Max(1, fadeFrames);
                long sBase = f * srcCh;
                long dBase = dstBase + f * dstCh;
                for (int c = 0; c < dstCh; c++)
                {
                    float s = src[Math.Clamp((int)(sBase + Math.Min(c, srcCh - 1)), 0, src.Length - 1)];
                    dst[dBase + c] += (float)(s * gain * env);
                }
            }
        }

        // --- Helpers ---

        private static async Task<List<AudioObj>> SliceLoopToDrumAtomsAsync(AudioObj src, double bpm)
        {
            // Compute an amplitude envelope and novelty to find onsets, then segment with zero-crossing refinement
            int sr = Math.Max(1, src.SampleRate);
            int ch = Math.Max(1, src.Channels);
            long totalFrames = Math.Max(1, (src.Data?.LongLength ?? 0) / ch);
            if (totalFrames <= 8) return new List<AudioObj>();

            // Build mono for onset/zero-cross detection
            float[] mono = await Task.Run(() =>
            {
                var m = new float[totalFrames];
                var d = src.Data;
                if (ch == 1)
                {
                    Array.Copy(d, 0, m, 0, (int)Math.Min(d.LongLength, m.LongLength));
                    return m;
                }
                for (long f = 0; f < totalFrames; f++)
                {
                    double sum = 0.0;
                    long o = f * ch;
                    for (int c = 0; c < ch; c++) sum += d[o + c];
                    m[f] = (float)(sum / ch);
                }
                return m;
            }).ConfigureAwait(false);

            // Envelope (fast) über Frames (Mono)
            double[] env = await Task.Run(() =>
            {
                int win = Math.Max(16, sr / 1500); // ~0.67ms
                double[] e = new double[totalFrames];
                double acc = 0.0;
                for (long i = 0; i < totalFrames; i++)
                {
                    double v = Math.Abs(mono[i]);
                    acc += v;
                    if (i >= win) acc -= Math.Abs(mono[i - win]);
                    e[i] = acc / Math.Min((long)win, i + 1);
                }
                // Smooth
                double alpha = 0.25;
                for (int i = 1; i < e.Length; i++) e[i] = alpha * e[i] + (1 - alpha) * e[i - 1];
                return e;
            }).ConfigureAwait(false);

            // Novelty (positive derivative)
            double[] novelty = new double[env.Length];
            for (int i = 1; i < env.Length; i++)
            {
                double d = env[i] - env[i - 1];
                novelty[i] = d > 0 ? d : 0.0;
            }

            // Peak-pick with min spacing ~ 1/32
            long framesPerBeat = (long)Math.Round(sr * 60.0 / Math.Max(1.0, bpm));
            long framesPer16 = Math.Max(1, framesPerBeat / 4);
            long minDist = Math.Max(1, framesPer16 / 2);
            double peakThr = novelty.Max() * 0.2; // relative threshold

            var onsets = new List<long>();
            long last = -minDist;
            for (int i = 1; i < novelty.Length - 1; i++)
            {
                if (novelty[i] > peakThr && novelty[i] >= novelty[i - 1] && novelty[i] >= novelty[i + 1])
                {
                    if (i - last >= minDist)
                    {
                        onsets.Add(i);
                        last = i;
                    }
                }
            }
            if (onsets.Count == 0) return new List<AudioObj>();

            // Segmentierung mit Zero-Crossing-Refinement und Guard vor nächstem Onset
            var atoms = new List<AudioObj>();
            long guardFrames = Math.Max(sr / 200, (int)(framesPer16 / 6)); // ~5ms oder kleiner Anteil 1/16
            long minFrames = Math.Max(4, framesPer16 / 6);                 // Mindestlänge
            long maxFrames = Math.Max(framesPer16 * 4, framesPer16 + guardFrames); // max 1/4 Note

            for (int idx = 0; idx < onsets.Count; idx++)
            {
                long onset = onsets[idx];
                long nextOnset = (idx + 1 < onsets.Count) ? onsets[idx + 1] : totalFrames;

                // Start: suche Zero-Crossing VOR Onset innerhalb 1/16
                long startZc = FindZeroCrossingBefore(mono, onset, framesPer16);
                long startFrame = startZc >= 0 ? startZc : onset;

                // Ende-Kandidat: knapp vor nächstem Onset, gedeckelt durch Maximaldauer
                long endLimit = Math.Min(totalFrames, startFrame + maxFrames);
                long endCand = Math.Min(endLimit, Math.Max(startFrame + minFrames, nextOnset - guardFrames));

                // Ende: suche Zero-Crossing KURZ VOR endCand
                long endZc = FindZeroCrossingBefore(mono, endCand, Math.Min(framesPer16, guardFrames * 3));
                long endFrame = endZc >= 0 ? endZc : endCand;

                if (endFrame - startFrame < minFrames)
                {
                    continue; // zu kurz
                }

                var slice = CloneRange(src, startFrame, endFrame - startFrame);
                var tagged = await ClassifyAndTagAsync(slice).ConfigureAwait(false);
                atoms.Add(tagged);
            }

            return atoms;
        }

        private static long FindZeroCrossingBefore(float[] mono, long fromFrame, long maxLookback)
        {
            long start = Math.Clamp(fromFrame, 1, mono.LongLength - 1);
            long minIdx = -1; float minAbs = float.MaxValue;
            long a = Math.Max(1, start - maxLookback);
            for (long i = start; i >= a; i--)
            {
                float v = mono[i];
                float p = mono[i - 1];
                float abs = Math.Abs(v);
                if (abs < minAbs) { minAbs = abs; minIdx = i; }
                if ((v >= 0 && p <= 0) || (v <= 0 && p >= 0))
                {
                    return i; // echte Null-Überquerung
                }
            }
            return minIdx; // kleinste Amplitude in Reichweite
        }

        private static AudioObj CloneRange(AudioObj src, long startFrame, long lengthFrames)
        {
            int ch = Math.Max(1, src.Channels);
            long totalFrames = Math.Max(1, (src.Data?.LongLength ?? 0) / ch);
            startFrame = Math.Clamp(startFrame, 0, totalFrames - 1);
            lengthFrames = Math.Clamp(lengthFrames, 1, totalFrames - startFrame);

            long startIndex = startFrame * ch;
            long count = lengthFrames * ch;
            var dst = new float[count];
            Array.Copy(src.Data, startIndex, dst, 0, count);

            return new AudioObj
            {
                Id = Guid.NewGuid(),
                Name = $"{src.Name}_slice_{startFrame}",
                Data = dst,
                SampleRate = src.SampleRate,
                Channels = src.Channels,
                BitDepth = src.BitDepth,
                Bpm = src.Bpm > 0 ? src.Bpm : src.ScannedBpm,
                ScannedBpm = src.ScannedBpm > 0 ? src.ScannedBpm : src.Bpm,
                Length = dst.LongLength,
                Duration = TimeSpan.FromSeconds((double)lengthFrames / Math.Max(1, src.SampleRate)),
                Volume = src.Volume
            };
        }

        private static async Task<AudioObj> ClassifyAndTagAsync(AudioObj audio)
        {
            try
            {
                string tag = await DrumLooper.GetClosestDrumAsync(audio).ConfigureAwait(false);
                audio.SampleTag = MapClassifierTagToRole(tag);
                return audio;
            }
            catch
            {
                audio.SampleTag = "Perc";
                return audio;
            }
        }

        private static string MapClassifierTagToRole(string tag)
        {
            string t = (tag ?? string.Empty).ToLowerInvariant();
            if (t.Contains("kick")) return "Kick";
            if (t.Contains("snare")) return "Snare";
            if (t.Contains("hi-hat") || t.Contains("hihat"))
            {
                if (t.Contains("open")) return "HiHatOpen";
                return "HiHatClosed";
            }
            if (t.Contains("clap")) return "Clap";
            if (t.Contains("ride")) return "Ride";
            if (t.Contains("crash")) return "Crash";
            if (t.Contains("tom")) return "Tom";
            return "Perc";
        }

        public static async Task<AudioObj> GeneratePaletteAsync(IEnumerable<AudioObj>? input, double? bpmOverride = null, double gapBeatFraction = 0.25)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            var list = input.Where(s => s != null && s.Data != null && s.Data.Length > 0).ToList();
            if (list.Count == 0)
            {
                throw new ArgumentException("No valid audio inputs provided.");
            }

            // Target format: most common SR/Channels
            int targetRate = list.Where(s => s.SampleRate > 0).Select(s => s.SampleRate).GroupBy(r => r).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? 44100;
            int targetChannels = list.Where(s => s.Channels > 0).Select(s => Math.Max(1, s.Channels)).GroupBy(c => c).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? 1;
            targetChannels = Math.Clamp(targetChannels, 1, 2);

            // Determine BPM (for gap length)
            double bpm;
            if (bpmOverride.HasValue && bpmOverride.Value > 0)
            {
                bpm = bpmOverride.Value;
            }
            else
            {
                var bpms = list.Select(s => s.Bpm > 0 ? s.Bpm : (s.ScannedBpm > 0 ? s.ScannedBpm : 0.0)).Where(b => b > 0.0).Select(b => (double) b).ToList();
                if (bpms.Count > 0) bpm = bpms.OrderBy(x => x).ElementAt(bpms.Count / 2);
                else
                {
                    // Try scan from first
                    try { bpm = await BeatScanner.ScanBpmAsync(list[0]); } catch { bpm = 0.0; }
                    if (bpm <= 0.0) bpm = 170.0;
                }
            }

            // Build atoms
            var atoms = new List<AudioObj>();
            if (list.Count == 1)
            {
                var src = list[0];
                var sliced = await SliceLoopToDrumAtomsAsync(src, bpm).ConfigureAwait(false);
                atoms.AddRange(sliced);
            }
            else
            {
                atoms.AddRange(list);
            }

            // If still empty ⇒ fallback to entire first item as one atom
            if (atoms.Count == 0)
            {
                atoms.Add(list[0]);
            }

            // Prepare concatenation
            long framesPerBeat = (long) Math.Round(targetRate * 60.0 / Math.Max(1.0, bpm));
            long gapFrames = (long) Math.Max(1, Math.Round(framesPerBeat * Math.Clamp(gapBeatFraction, 0.05, 1.0))); // default 1/4 beat
            long fadeFrames = Math.Max(1, targetRate / 500); // ~2ms

            // Precompute unified atoms (resample/upmix) and their frame lengths
            var unified = new List<float[]>();
            var lengths = new List<long>();

            foreach (var a in atoms)
            {
                if (a.Data == null || a.Data.Length == 0 || a.Channels <= 0) continue;
                float[] data = a.Data;
                // Channels
                data = a.Channels == targetChannels ? data : (targetChannels == 2 ? UpmixToStereo(data, a.Channels) : DownmixToMono(data, a.Channels));
                // SampleRate
                if (a.SampleRate > 0 && a.SampleRate != targetRate)
                {
                    data = ResampleLinear(data, a.SampleRate, targetRate, targetChannels);
                }
                if (data.Length <= 0) continue;
                unified.Add(data);
                lengths.Add(data.LongLength / Math.Max(1, targetChannels));
            }

            if (unified.Count == 0)
            {
                throw new InvalidOperationException("No usable atoms to build the palette.");
            }

            // Total frames: sum(frames) + gaps between
            long totalFrames = 0;
            for (int i = 0; i < lengths.Count; i++)
            {
                totalFrames += lengths[i];
                if (i < lengths.Count - 1) totalFrames += gapFrames;
            }

            var mix = new float[totalFrames * targetChannels];

            // Write atoms sequentially with gap and small fades
            long cursor = 0;
            for (int i = 0; i < unified.Count; i++)
            {
                var src = unified[i];
                long frames = lengths[i];
                MixInterleaved(mix, src, cursor, frames, 1.0f, targetChannels, targetChannels, fadeFrames);
                cursor += frames;
                if (i < unified.Count - 1)
                {
                    cursor += gapFrames;
                }
            }

            var loop = new AudioObj
            {
                Id = Guid.NewGuid(),
                Name = $"DrumPalette_{bpm:F0}bpm_{atoms.Count}atoms",
                Data = mix,
                SampleRate = targetRate,
                Channels = targetChannels,
                BitDepth = 32,
                Bpm = (float) bpm,
                Length = mix.LongLength,
                Duration = TimeSpan.FromSeconds((double) (mix.LongLength / Math.Max(1, targetChannels)) / targetRate),
                SampleTag = "Palette"
            };

            await loop.NormalizeAsync(0.98f);
            return loop;
        }

        private static float[] UpmixToStereo(float[] data, int srcCh)
        {
            if (srcCh == 2) return data;
            long frames = data.LongLength / Math.Max(1, srcCh);
            var dst = new float[frames * 2];
            for (long f = 0; f < frames; f++)
            {
                float m = data[f * srcCh];
                long o = f * 2;
                dst[o] = m;
                dst[o + 1] = m;
            }
            return dst;
        }

        private static float[] DownmixToMono(float[] data, int srcCh)
        {
            if (srcCh == 1) return data;
            long frames = data.LongLength / Math.Max(1, srcCh);
            var dst = new float[frames];
            for (long f = 0; f < frames; f++)
            {
                double sum = 0.0;
                for (int c = 0; c < srcCh; c++) sum += data[f * srcCh + c];
                dst[f] = (float) (sum / srcCh);
            }
            return dst;
        }

        private static float[] ResampleLinear(float[] interleaved, int srcRate, int dstRate, int channels)
        {
            if (srcRate == dstRate || interleaved.Length == 0) return interleaved;
            long frames = interleaved.LongLength / Math.Max(1, channels);
            double ratio = (double) dstRate / srcRate;
            long outFrames = (long) Math.Max(1, Math.Round(frames * ratio));
            var dst = new float[outFrames * channels];
            for (long of = 0; of < outFrames; of++)
            {
                double srcPos = of / ratio; // in frames
                long i0 = (long) Math.Floor(srcPos);
                long i1 = Math.Min(frames - 1, i0 + 1);
                double t = srcPos - i0;
                for (int c = 0; c < channels; c++)
                {
                    double v0 = interleaved[Math.Clamp((int) (i0 * channels + c), 0, interleaved.Length - 1)];
                    double v1 = interleaved[Math.Clamp((int) (i1 * channels + c), 0, interleaved.Length - 1)];
                    dst[of * channels + c] = (float) (v0 + (v1 - v0) * t);
                }
            }
            return dst;
        }
    }
}
