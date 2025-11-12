using CSharpSamplesCutter.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class DrumEngine
{
    public static async Task<List<AudioObj>> GenerateLoopsAsync(
        IEnumerable<AudioObj> inputs,
        string smallestNote,
        float bpm,
        int bars,
        int count,
        IProgress<double>? progress = null,
        int beatsPerBar = 4,
        int? targetSampleRate = null,
        int? targetChannels = null,
        CancellationToken cancellationToken = default)
    {
        if (inputs == null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        if (bpm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bpm), "BPM must be > 0.");
        }

        if (bars <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bars), "Bars must be > 0.");
        }

        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be > 0.");
        }

        if (beatsPerBar <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(beatsPerBar), "BeatsPerBar must be > 0.");
        }

        // Copy input list to avoid multiple enumeration
        var inputList = inputs.Where(s => s != null && s.Data != null && s.Data.Length > 0).ToList();
        if (inputList.Count == 0)
        {
            return new List<AudioObj>();
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Decide target SR/channels
        int decidedSampleRate = targetSampleRate ?? Math.Max(44100, inputList.Max(s => s.SampleRate > 0 ? s.SampleRate : 0));
        if (decidedSampleRate <= 0)
        {
            decidedSampleRate = 44100;
        }

        int decidedChannels = targetChannels ?? Math.Max(1, inputList.Max(s => s.Channels > 0 ? s.Channels : 1));
        decidedChannels = Math.Clamp(decidedChannels, 1, 2); // keep to mono/stereo for simplicity

        // Parse note division
        var grid = ParseGrid(smallestNote, bpm, decidedSampleRate, beatsPerBar);

        // Progress bookkeeping
        double totalWork =
            1.0  // preprocessing/variants
            + count * 0.5 // pattern gen weight
            + count * 2.0 // rendering weight
            + 1.0;        // final normalization / wrap-up
        double done = 0.0;
        void Report(double add) { done += add; progress?.Report(Math.Min(1.0, done / totalWork)); }

        // --------------------------
        // Preprocess: trim + unify + variants
        // --------------------------
        var bank = await Task.Run(() =>
        {
            return BuildVariantBank(inputList, decidedSampleRate, decidedChannels, grid, cancellationToken);
        }, cancellationToken);
        Report(1.0);

        // --------------------------
        // Loop generation (patterns + render)
        // --------------------------
        var results = new List<AudioObj>(capacity: count);
        var rnd = new ThreadSafeRandom();

        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1) Create pattern (grid events)
            var events = await Task.Run(() =>
            {
                var seed = rnd.Next();
                return GeneratePattern(bank, grid, bars, rnd, seed);
            }, cancellationToken);
            Report(0.5);

            // 2) Render audio buffer
            var rendered = await Task.Run(() =>
            {
                return RenderLoop(bank, events, grid, bars, decidedSampleRate, decidedChannels, cancellationToken);
            }, cancellationToken);
            Report(2.0);

            // 3) Wrap in AudioObj
            var loop = new AudioObj
            {
                Name = $"DrumLoop_{i + 1:D2}_{bpm}bpm_{bars}bars_{smallestNote}",
                Data = rendered,
                SampleRate = decidedSampleRate,
                Channels = decidedChannels,
                BitDepth = sizeof(float) * 8,
                Length = rendered.LongLength,
                Duration = TimeSpan.FromSeconds((double)rendered.LongLength / (decidedChannels * decidedSampleRate)),
                Volume = 1.0f,
                SampleTag = "Loop"
            };

            results.Add(loop);
        }

        // Final small report
        Report(1.0);

        return results;
    }



    private sealed class SampleVariant
    {
        public AudioObj Source { get; init; } = default!;
        public string Tag { get; init; } = "";
        public string VariantName { get; init; } = "";
        public float[] Data { get; init; } = [];
        public int SampleRate { get; init; }
        public int Channels { get; init; }
        public float BaseGain { get; init; } = 1.0f;
        public long LengthFrames => Data.LongLength / Math.Max(1, Channels);
    }

    private sealed class GridSpec
    {
        public string SmallestNote { get; init; } = "1/16";
        public float Bpm { get; init; }
        public int SampleRate { get; init; }
        public int BeatsPerBar { get; init; } = 4;
        public double SecondsPerBeat { get; init; }
        public int StepsPerBeat { get; init; }    // e.g., 4 for 1/16
        public double SamplesPerStepExact { get; init; }
        public int SamplesPerStepFloor { get; init; }
        public bool IsTriplet { get; init; }
        public bool IsSwing { get; init; }
        public double SwingRatio { get; init; } = 0.57; // 57%/43% swing default
    }

    private sealed class ScheduledEvent
    {
        public SampleVariant Variant { get; init; } = default!;
        public long StartSample { get; init; } // absolute samples (per channel frame index)
        public float Velocity { get; init; } = 1.0f; // 0..1-ish
    }

    private static GridSpec ParseGrid(string smallestNote, float bpm, int sampleRate, int beatsPerBar)
    {
        // Accept formats: "1/16", "1/8", "1/16T" (triplet), "1/16S" (swing)
        string s = (smallestNote ?? "1/16").Trim().ToUpperInvariant();
        bool isTriplet = s.EndsWith("T");
        bool isSwing = s.EndsWith("S");
        s = s.TrimEnd('T', 'S');

        int denom = 16; // default
        if (s.StartsWith("1/"))
        {
            if (!int.TryParse(s.AsSpan(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out denom))
            {
                denom = 16;
            }
        }

        // In 4/4: 1/4 = 1 beat; 1/8 = 2 steps per beat; 1/16 = 4 steps per beat; 1/32 = 8 steps per beat...
        int stepsPerBeat = Math.Max(1, denom / 4);

        // Triplet means 3 equal steps per beat
        if (isTriplet)
        {
            stepsPerBeat = 3;
        }

        double spb = 60.0 / bpm;
        double samplesPerBeat = spb * sampleRate;
        double samplesPerStep = samplesPerBeat / stepsPerBeat;

        return new GridSpec
        {
            SmallestNote = smallestNote ?? "1/16",
            Bpm = bpm,
            SampleRate = sampleRate,
            BeatsPerBar = beatsPerBar,
            SecondsPerBeat = spb,
            StepsPerBeat = stepsPerBeat,
            SamplesPerStepExact = samplesPerStep,
            SamplesPerStepFloor = (int)Math.Floor(samplesPerStep),
            IsTriplet = isTriplet,
            IsSwing = isSwing,
            SwingRatio = 0.57
        };
    }

    private static Dictionary<string, List<SampleVariant>> BuildVariantBank(
        List<AudioObj> inputs,
        int targetSampleRate,
        int targetChannels,
        GridSpec grid,
        CancellationToken ct)
    {
        // Normalize tags
        static string NormTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return "Perc";
            }

            tag = tag.Trim();
            // Map-friendly casing
            return tag switch
            {
                var t when t.Equals("Kick", StringComparison.OrdinalIgnoreCase) => "Kick",
                var t when t.Equals("Snare", StringComparison.OrdinalIgnoreCase) => "Snare",
                var t when t.Equals("HiHatClosed", StringComparison.OrdinalIgnoreCase) => "HiHatClosed",
                var t when t.Equals("HiHatOpen", StringComparison.OrdinalIgnoreCase) => "HiHatOpen",
                var t when t.Equals("Clap", StringComparison.OrdinalIgnoreCase) => "Clap",
                var t when t.Equals("Ride", StringComparison.OrdinalIgnoreCase) => "Ride",
                var t when t.Equals("Crash", StringComparison.OrdinalIgnoreCase) => "Crash",
                var t when t.Equals("Tom", StringComparison.OrdinalIgnoreCase) => "Tom",
                var t when t.Equals("Perc", StringComparison.OrdinalIgnoreCase) => "Perc",
                _ => "Perc"
            };
        }

        var groups = inputs.GroupBy(i => NormTag(i.SampleTag)).ToDictionary(g => g.Key, g => g.ToList());
        var bank = new Dictionary<string, List<SampleVariant>>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in groups)
        {
            ct.ThrowIfCancellationRequested();
            string tag = kv.Key;
            var list = kv.Value;

            var destList = new List<SampleVariant>();
            foreach (var s in list)
            {
                ct.ThrowIfCancellationRequested();

                // 1) unify channels + sample rate (copy)
                var unified = UnifySample(s, targetSampleRate, targetChannels);

                // 2) trim in/out (heuristic)
                var trimmed = TrimSilence(unified.data, targetChannels, thresholdDb: -50.0f, minActiveSamples: targetSampleRate / 100); // >10ms continuity

                if (trimmed.Length == 0)
                {
                    continue;
                }

                // 3) heuristics for "compatible duration" in steps
                int suggestedSteps = SuggestStepsForTag(tag, grid);
                long targetFrames = (long)Math.Max(grid.SamplesPerStepFloor * suggestedSteps, 1);
                long currentFrames = trimmed.LongLength / targetChannels;
                long fullFrames = Math.Min(currentFrames, targetFrames);

                float[] full = CopyFrames(trimmed, targetChannels, fullFrames);
                float[] half = CopyFrames(trimmed, targetChannels, Math.Max(1, fullFrames / 2));
                float[] quarter = CopyFrames(trimmed, targetChannels, Math.Max(1, fullFrames / 4));

                // For hats: add a "tight" short choke variant (e.g., 1/8 of full)
                if (tag is "HiHatClosed" or "HiHatOpen")
                {
                    float[] tight = CopyFrames(trimmed, targetChannels, Math.Max(1, fullFrames / 8));
                    destList.Add(new SampleVariant
                    {
                        Source = s,
                        Tag = tag,
                        VariantName = "Tight",
                        Data = tight,
                        Channels = targetChannels,
                        SampleRate = targetSampleRate,
                        BaseGain = Math.Max(0.05f, s.Volume)
                    });
                }

                destList.Add(new SampleVariant
                {
                    Source = s,
                    Tag = tag,
                    VariantName = "Full",
                    Data = full,
                    Channels = targetChannels,
                    SampleRate = targetSampleRate,
                    BaseGain = Math.Max(0.05f, s.Volume)
                });
                destList.Add(new SampleVariant
                {
                    Source = s,
                    Tag = tag,
                    VariantName = "Half",
                    Data = half,
                    Channels = targetChannels,
                    SampleRate = targetSampleRate,
                    BaseGain = Math.Max(0.05f, s.Volume)
                });
                destList.Add(new SampleVariant
                {
                    Source = s,
                    Tag = tag,
                    VariantName = "Quarter",
                    Data = quarter,
                    Channels = targetChannels,
                    SampleRate = targetSampleRate,
                    BaseGain = Math.Max(0.05f, s.Volume)
                });
            }

            if (destList.Count > 0)
            {
                bank[tag] = destList;
            }
        }

        return bank;
    }

    private static int SuggestStepsForTag(string tag, GridSpec grid)
    {
        // Heuristiken: in Steps (kleinste Note)
        // 1 Step = smallestNote (z.B. 1/16)
        return tag switch
        {
            "Kick" => 1,
            "Snare" => Math.Min(2, grid.StepsPerBeat), // max 1/8 bei 1/16 grid
            "Clap" => Math.Min(2, grid.StepsPerBeat),
            "HiHatClosed" => 1,
            "HiHatOpen" => Math.Min(4, grid.StepsPerBeat * 2), // kann länger ausklingen
            "Ride" => Math.Min(4, grid.StepsPerBeat * 2),
            "Crash" => grid.StepsPerBeat * grid.BeatsPerBar, // 1 bar sustain
            "Tom" => Math.Min(3, grid.StepsPerBeat),
            _ => Math.Min(2, grid.StepsPerBeat)
        };
    }

    private static (float[] data, int sr, int ch) UnifySample(AudioObj s, int targetSampleRate, int targetChannels)
    {
        var srcData = s.Data ?? [];
        int srcCh = Math.Max(1, s.Channels);
        int srcSr = s.SampleRate > 0 ? s.SampleRate : targetSampleRate;

        float[] chConverted = srcCh == targetChannels ? srcData
            : (targetChannels == 2 ? UpmixToStereo(srcData, srcCh) : DownmixToMono(srcData, srcCh));

        if (srcSr != targetSampleRate)
        {
            var res = ResampleLinear(chConverted, srcSr, targetSampleRate, targetChannels);
            return (res, targetSampleRate, targetChannels);
        }

        return (chConverted, srcSr, targetChannels);
    }

    private static float[] TrimSilence(float[] interleaved, int ch, float thresholdDb = -50.0f, int minActiveSamples = 512)
    {
        if (interleaved == null || interleaved.Length == 0)
        {
            return [];
        }

        ch = Math.Max(1, ch);
        float thr = (float)Math.Pow(10.0, thresholdDb / 20.0);

        long frames = interleaved.LongLength / ch;
        long start = 0;
        long end = frames - 1;

        // Leading
        for (long f = 0; f < frames; f++)
        {
            if (FrameAbove(interleaved, ch, f, thr))
            {
                start = Math.Max(0, f - 2); // keep tiny pre-transient
                break;
            }
        }

        // Trailing
        for (long f = frames - 1; f >= 0; f--)
        {
            if (FrameAbove(interleaved, ch, f, thr))
            {
                end = Math.Min(frames - 1, f + 2);
                break;
            }
        }

        if (end <= start)
        {
            return [];
        }

        long selFrames = end - start + 1;
        // ensure minimal length if wanted
        if (selFrames < minActiveSamples / ch)
        {
            selFrames = Math.Min(frames, start + (minActiveSamples / ch)) - start;
        }

        return CopyFrames(interleaved, ch, selFrames, start);
    }

    private static bool FrameAbove(float[] data, int ch, long frameIndex, float thr)
    {
        long offset = frameIndex * ch;
        float sum = 0f;
        for (int c = 0; c < ch; c++)
        {
            float v = Math.Abs(data[offset + c]);
            if (v > sum)
            {
                sum = v;
            }
        }
        return sum >= thr;
    }

    private static float[] CopyFrames(float[] interleaved, int ch, long frames, long startFrame = 0)
    {
        long totalFrames = interleaved.LongLength / ch;
        frames = Math.Clamp(frames, 0, totalFrames - startFrame);
        long count = frames * ch;
        if (count <= 0)
        {
            return [];
        }

        var dst = new float[count];
        Array.Copy(interleaved, startFrame * ch, dst, 0, count);
        return dst;
    }

    private static float[] UpmixToStereo(float[] data, int srcCh)
    {
        if (srcCh == 2)
        {
            return data;
        }
        // mono -> stereo duplicate
        long frames = data.LongLength / srcCh;
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
        if (srcCh == 1)
        {
            return data;
        }

        long frames = data.LongLength / srcCh;
        var dst = new float[frames];
        for (long f = 0; f < frames; f++)
        {
            float sum = 0f;
            for (int c = 0; c < srcCh; c++)
            {
                sum += data[f * srcCh + c];
            }

            dst[f] = sum / srcCh;
        }
        return dst;
    }

    private static float[] ResampleLinear(float[] data, int srcSr, int dstSr, int ch)
    {
        if (srcSr == dstSr)
        {
            return data;
        }

        long srcFrames = data.LongLength / ch;
        double ratio = (double)dstSr / srcSr;
        long dstFrames = (long)Math.Max(1, Math.Floor(srcFrames * ratio));
        var dst = new float[dstFrames * ch];

        for (long f = 0; f < dstFrames; f++)
        {
            double srcPos = f / ratio; // frame position in source
            long i = (long)Math.Floor(srcPos);
            double frac = srcPos - i;
            long i2 = Math.Min(i + 1, srcFrames - 1);

            for (int c = 0; c < ch; c++)
            {
                float a = data[i * ch + c];
                float b = data[i2 * ch + c];
                dst[f * ch + c] = (float)(a + (b - a) * frac);
            }
        }

        return dst;
    }

    // --------------- Pattern generation ---------------

    private static List<ScheduledEvent> GeneratePattern(
        Dictionary<string, List<SampleVariant>> bank,
        GridSpec grid,
        int bars,
        ThreadSafeRandom rnd,
        int seedForDeterminism)
    {
        long stepsPerBar = grid.StepsPerBeat * grid.BeatsPerBar;
        long totalSteps = stepsPerBar * bars;

        var events = new List<ScheduledEvent>(capacity: (int)(totalSteps * 3));

        bool hasKick = bank.TryGetValue("Kick", out var kicks) && kicks.Count > 0;
        bool hasSnare = bank.TryGetValue("Snare", out var snares) && snares.Count > 0;
        bool hasClap = bank.TryGetValue("Clap", out var claps) && claps.Count > 0;
        bool hasCH = bank.TryGetValue("HiHatClosed", out var chh) && chh.Count > 0;
        bool hasOH = bank.TryGetValue("HiHatOpen", out var ohh) && ohh.Count > 0;
        bool hasRide = bank.TryGetValue("Ride", out var rides) && rides.Count > 0;
        bool hasCrash = bank.TryGetValue("Crash", out var crashes) && crashes.Count > 0;
        bool hasTom = bank.TryGetValue("Tom", out var toms) && toms.Count > 0;
        bool hasPerc = bank.TryGetValue("Perc", out var percs) && percs.Count > 0;

        // Core backbeat (DnB-ish): Snare auf 2 & 4 (in 4/4)
        if (hasSnare && snares != null)
        {
            for (int b = 0; b < bars; b++)
            {
                long barStart = b * stepsPerBar;
                long s2 = barStart + grid.StepsPerBeat * 1; // Beat 2
                long s4 = barStart + grid.StepsPerBeat * 3; // Beat 4
                events.Add(MakeEvent(snares, rnd, StepToSample(s2, grid), 0.95f));
                events.Add(MakeEvent(snares, rnd, StepToSample(s4, grid), 0.95f));

                // Optional Clap layer on 2 & 4
                if (hasClap && rnd.Prob(0.5) && claps != null)
				{
                    events.Add(MakeEvent(claps, rnd, StepToSample(s2, grid), 0.85f));
                    events.Add(MakeEvent(claps, rnd, StepToSample(s4, grid), 0.85f));
                }
            }
        }

        // Kicks: sensible Startpunkte + DnB-typische Offbeats
        if (hasKick && kicks != null)
		{
            for (int b = 0; b < bars; b++)
            {
                long barStart = b * stepsPerBar;
                // Downbeat Kick
                events.Add(MakeEvent(kicks, rnd, StepToSample(barStart + 0, grid), 1.0f));

                // Offbeats
                var candidates = new List<long>
                {
                    barStart + grid.StepsPerBeat * 0 + grid.StepsPerBeat/2, // 1&
                    barStart + grid.StepsPerBeat * 1 - 1,                   // vor 2
                    barStart + grid.StepsPerBeat * 2 + 1,                   // nach 3
                    barStart + grid.StepsPerBeat * 3 - 1                    // vor 4
                };
                foreach (var st in candidates)
                {
                    if (rnd.Prob(0.6))
                    {
                        events.Add(MakeEvent(kicks, rnd, StepToSample(ClampStep(st, totalSteps), grid), rnd.Range(0.8f, 1.0f)));
                    }
                }
            }
        }

        // Closed hats: straight 1/8 oder 1/16 je nach grid, leichte Velocity‑Variation
        if (hasCH && chh != null)
		{
            int hatEvery = grid.StepsPerBeat >= 4 ? 1 : 2; // auf 1/16 grid: jede Stufe; auf 1/8 grid: jede Stufe
            for (long st = 0; st < totalSteps; st += hatEvery)
            {
                float vel = 0.6f + (float)((rnd.NextDouble() - 0.5) * 0.2); // +/-0.1
                events.Add(MakeEvent(chh, rnd, StepToSample(st, grid), vel));
            }

            // Ghost hats
            for (long st = 0; st < totalSteps; st++)
            {
                if (rnd.Prob(0.08))
                {
                    events.Add(MakeEvent(chh, rnd, StepToSample(st, grid), 0.35f));
                }
            }
        }

        // Open hats / rides on offbeats
        if (hasOH || hasRide || rides != null || ohh != null)
		{
            for (int b = 0; b < bars; b++)
            {
                long barStart = b * stepsPerBar;
                long[] positions =
                {
                    barStart + grid.StepsPerBeat/2,                        // 1&
                    barStart + grid.StepsPerBeat + grid.StepsPerBeat/2,    // 2&
                    barStart + 2*grid.StepsPerBeat + grid.StepsPerBeat/2,  // 3&
                    barStart + 3*grid.StepsPerBeat + grid.StepsPerBeat/2   // 4&
                };
                foreach (var st in positions)
                {
                    if (rnd.Prob(0.5))
                    {
                        if (hasOH && rnd.Prob(0.7))
                        {
                            events.Add(MakeEvent(ohh!, rnd, StepToSample(st, grid), 0.8f));
                        }
                        else if (hasRide)
                        {
                            events.Add(MakeEvent(rides!, rnd, StepToSample(st, grid), 0.75f));
                        }
                    }
                }
            }
        }

        // Crash at bar start
        if (hasCrash && rnd.Prob(0.6))
        {
            events.Add(MakeEvent(crashes!, rnd, StepToSample(0, grid), 0.9f));
        }

        // Toms / Perc fills near bar ends
        if ((hasTom || hasPerc) && grid.StepsPerBeat >= 4)
        {
            for (int b = 0; b < bars; b++)
            {
                if (!rnd.Prob(0.7))
                {
                    continue;
                }

                long barStart = b * stepsPerBar;
                long fillStart = barStart + grid.StepsPerBeat * 3; // letzte Zählzeit
                int fillLenSteps = (int)Math.Min(4, stepsPerBar - (fillStart - barStart));

                for (int i = 0; i < fillLenSteps; i++)
                {
                    long st = fillStart + i;
                    if (hasTom && rnd.Prob(0.7))
                    {
                        events.Add(MakeEvent(toms!, rnd, StepToSample(st, grid), rnd.Range(0.5f, 0.9f)));
                    }
                    else if (hasPerc && rnd.Prob(0.7))
                    {
                        events.Add(MakeEvent(percs!, rnd, StepToSample(st, grid), rnd.Range(0.4f, 0.8f)));
                    }
                }
            }
        }

        // Optional: snare rolls (breakcore-ish) kurz vor Taktende
        if (hasSnare && grid.StepsPerBeat >= 4 && rnd.Prob(0.35))
        {
            for (int b = 0; b < bars; b++)
            {
                long barStart = b * stepsPerBar;
                long rollStart = barStart + grid.StepsPerBeat * 3 + grid.StepsPerBeat / 2; // 4&
                int rollLen = Math.Min(3, grid.StepsPerBeat / 2); // ein paar 1/32 steps auf 1/16 grid
                for (int i = 0; i < rollLen; i++)
                {
                    long st = rollStart + i;
                    if (st < (b + 1) * stepsPerBar)
                    {
                        events.Add(MakeEvent(snares!, rnd, StepToSample(st, grid), 0.6f));
                    }
                }
            }
        }

        // Sort by start sample to ensure deterministic rendering order
        events.Sort((a, b) => a.StartSample.CompareTo(b.StartSample));

        return events;
    }

    private static long ClampStep(long st, long totalSteps) => Math.Max(0, Math.Min(st, totalSteps - 1));

    private static long StepToSample(long step, GridSpec grid)
    {
        // Swing support: delay every second step within beat
        if (grid.IsSwing && grid.StepsPerBeat >= 2)
        {
            long beatIndex = step / grid.StepsPerBeat;
            int posInBeat = (int)(step - beatIndex * grid.StepsPerBeat);
            bool isOff = (posInBeat % 2) == 1;
            double baseSamples = step * grid.SamplesPerStepExact;
            if (isOff)
            {
                double early = grid.SamplesPerStepExact * (1.0 - grid.SwingRatio);
                double late = grid.SamplesPerStepExact * (grid.SwingRatio);
                // shift by + (late - early)/2 to emulate swing-feel
                return (long)Math.Round(baseSamples + (late - early) * 0.5);
            }
            return (long)Math.Round(baseSamples);
        }
        return (long)Math.Round(step * grid.SamplesPerStepExact);
    }

    private static ScheduledEvent MakeEvent(List<SampleVariant> variants, ThreadSafeRandom rnd, long startSample, float velocity)
    {
        var variant = variants[rnd.Next(variants.Count)];
        // Small chance to pick shorter variant for tightness
        if (variants.Count > 1 && rnd.Prob(0.25))
        {
            var shorty = variants.FirstOrDefault(v => v.VariantName is "Quarter" or "Half" or "Tight");
            if (shorty != null)
            {
                variant = shorty;
            }
        }

        return new ScheduledEvent
        {
            Variant = variant,
            StartSample = Math.Max(0, startSample),
            Velocity = Math.Max(0.0f, Math.Min(1.2f, velocity))
        };
    }

    // --------------- Rendering ---------------

    private static float[] RenderLoop(
        Dictionary<string, List<SampleVariant>> bank,
        List<ScheduledEvent> events,
        GridSpec grid,
        int bars,
        int sampleRate,
        int channels,
        CancellationToken ct)
    {
        long stepsPerBar = grid.StepsPerBeat * grid.BeatsPerBar;
        long totalSteps = stepsPerBar * bars;
        long totalFrames = (long)Math.Ceiling(totalSteps * grid.SamplesPerStepExact);

        var mix = new float[totalFrames * channels];

        // Render events
        foreach (var ev in events)
        {
            ct.ThrowIfCancellationRequested();
            var v = ev.Variant;
            var data = v.Data;
            int ch = v.Channels;
            long frames = v.LengthFrames;
            long start = ev.StartSample;

            float gain = v.BaseGain * ev.Velocity;

            // Write interleaved with simple overlap-add
            long dstStart = start * channels;
            long srcIdx = 0;
            long dstIdx = dstStart;

            long framesToWrite = Math.Min(frames, totalFrames - ev.StartSample);
            long samplesToWrite = Math.Max(0, framesToWrite * channels);
            if (samplesToWrite <= 0)
            {
                continue;
            }

            if (channels == ch)
            {
                // fast path
                for (long i = 0; i < samplesToWrite; i++)
                {
                    mix[dstIdx + i] += data[srcIdx + i] * gain;
                }
            }
            else if (channels == 2 && ch == 1)
            {
                // mono source -> stereo dest
                for (long f = 0; f < framesToWrite; f++)
                {
                    float m = data[f] * gain;
                    long d = dstStart + f * 2;
                    mix[d] += m;
                    mix[d + 1] += m;
                }
            }
            else if (channels == 1 && ch == 2)
            {
                // stereo source -> mono dest
                for (long f = 0; f < framesToWrite; f++)
                {
                    float m = (data[f * 2] + data[f * 2 + 1]) * 0.5f * gain;
                    long d = dstStart + f;
                    mix[d] += m;
                }
            }
            // else: other channel combos are not expected
        }

        // Gentle peak normalization (avoid heavy limiting)
        float peak = 0f;
        for (int i = 0; i < mix.Length; i++)
        {
            float a = Math.Abs(mix[i]);
            if (a > peak)
            {
                peak = a;
            }
        }
        if (peak > 1.0f)
        {
            float scale = 1.0f / peak;
            for (int i = 0; i < mix.Length; i++)
            {
                mix[i] *= scale;
            }
        }

        return mix;
    }

    // --------------- Random helper ---------------

    private sealed class ThreadSafeRandom
    {
        private readonly object _lock = new();
        private readonly Random _rnd = new(Random.Shared.Next());
        public int Next() { lock (this._lock)
            {
                return this._rnd.Next();
            }
        }
        public int Next(int max) { lock (this._lock)
            {
                return this._rnd.Next(max);
            }
        }
        public double NextDouble() { lock (this._lock)
            {
                return this._rnd.NextDouble();
            }
        }
        public bool Prob(double p) => NextDouble() < p;
        public float Range(float a, float b)
        {
            var t = (float)NextDouble();
            return a + (b - a) * t;
        }
    }
}