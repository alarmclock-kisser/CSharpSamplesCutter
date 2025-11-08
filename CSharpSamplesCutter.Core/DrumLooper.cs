using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpSamplesCutter.Core
{
	public static class DrumLooper
	{
		private enum DrumRole { Kick, Snare, HiHatClosed, HiHatOpen, Clap, Ride, Crash, Tom, Perc }

		public static async Task<IEnumerable<AudioObj>> CreateDrumLoopsAsync(
			IEnumerable<AudioObj> samples,
			int barsCount = 16,
			int loopsCount = 4,
			double? bpmOverride = null,
			double swing = 0.0,                // [0..0.5] Anteil 1/16 Verzögerung für Offbeats
			double humanizeMs = 0.0,           // ± Jitter in Millisekunden
			int? seed = null,                  // reproduzierbar, falls gesetzt
			float targetPeak = 0.98f,          // Ziel-Normalisierung
			IProgress<double>? progress = null,
			CancellationToken? cancellationToken = null)
		{
			CancellationToken token = cancellationToken ?? CancellationToken.None;
			progress?.Report(0.0);

			if (samples == null)
			{
				return [];
			}

			var sampleList = samples.Where(s => s != null && s.Data != null && s.Data.Length > 0).ToList();
			if (sampleList.Count == 0)
			{
				progress?.Report(1.0);
				return [];
			}

			token.ThrowIfCancellationRequested();

			// 1) Ziel-Format bestimmen (Rate/Channels) und BPM ermitteln
			int targetRate = sampleList.Select(s => s.SampleRate).Where(r => r > 0).GroupBy(r => r).OrderByDescending(g => g.Count()).First().Key;
			int targetChannels = sampleList.Select(s => Math.Max(1, s.Channels)).GroupBy(c => c).OrderByDescending(g => g.Count()).First().Key;

			double bpm;
			if (bpmOverride.HasValue && bpmOverride.Value > 0)
			{
				bpm = bpmOverride.Value;
			}
			else
			{
				List<double> bpms = sampleList.Select(s =>
				{
					double b = s.Bpm > 0 ? s.Bpm : (s.ScannedBpm > 0 ? s.ScannedBpm : 0.0);
					return (double)b;
				})
				.Where(b => b > 0.0)
				.ToList();
				bpm = bpms.Count > 0 ? bpms.OrderBy(x => x).ElementAt(bpms.Count / 2) : 120.0; // Median oder Default
			}

			swing = Math.Clamp(swing, 0.0, 0.5);
			double humanizeMsClamped = Math.Max(0.0, humanizeMs);

			double phase1 = 0.1;
			progress?.Report(phase1);

			token.ThrowIfCancellationRequested();

			// 2) Samples normalisieren (Rate/Channels) und auf Rasterlängen zuschneiden/auffüllen
			// Erlaubte Dauern (in Bars): 1/16, 1/8, 1/4, 1/2, 1, 2, 4
			int[] num16Multipliers = new[] { 1, 2, 4, 8, 16, 32, 64 }; // Anzahl 1/16 pro Sample
			long framesPerBeat = (long)Math.Round(targetRate * 60.0 / bpm);
			long framesPerBar = framesPerBeat * 4;
			long framesPerSixteenth = Math.Max(1, framesPerBar / 16);

			var normalized = new ConcurrentBag<AudioObj>();
			await Task.Run(() =>
			{
				Parallel.ForEach(sampleList, new ParallelOptions { CancellationToken = token }, s =>
				{
					var s1 = EnsureRateAndChannels(s, targetRate, targetChannels);
					var s2 = FitToGridDurations(s1, framesPerSixteenth, num16Multipliers);
					normalized.Add(s2);
				});
			}, token).ConfigureAwait(false);

			double phase2 = 0.35;
			progress?.Report(phase2);

			token.ThrowIfCancellationRequested();

			// 3) Gruppieren nach Tags (DrumRole)
			var groups = normalized.GroupBy(s => CategorizeTag(s.SampleTag))
				.ToDictionary(g => g.Key, g => g.ToList());

			// 4) Loops generieren
			int totalSteps = Math.Max(1, barsCount) * 16; // 1/16 Raster
			Random rnd = seed.HasValue ? new Random(seed.Value) : new Random();
			long humanizeFrames = (long)Math.Round(humanizeMsClamped / 1000.0 * targetRate);

			List<AudioObj> loops = new List<AudioObj>(loopsCount);

			for (int li = 0; li < loopsCount; li++)
			{
				token.ThrowIfCancellationRequested();

				// Pattern pro Rolle erzeugen
				var rolePatterns = BuildRolePatterns(groups.Keys.ToHashSet(), totalSteps, rnd);

				// Per-Rolle-Buffer parallel befüllen
				int channels = targetChannels;
				long totalFrames = framesPerBar * Math.Max(1, barsCount);
				int outLen = checked((int)(totalFrames * channels));

				var roleBuffers = new ConcurrentDictionary<DrumRole, float[]>();
				var roleTasks = new List<Task>();
				foreach (var role in rolePatterns.Keys)
				{
					// Wenn keine Samples für Rolle vorhanden, überspringen
					if (!groups.TryGetValue(role, out var roleSamples) || roleSamples.Count == 0)
					{
						continue;
					}

					var steps = rolePatterns[role];
					roleTasks.Add(Task.Run(() =>
					{
						float[] buffer = new float[outLen];
						foreach (int step in steps)
						{
							// Sample zufällig aus Gruppe wählen
							var sample = roleSamples[rnd.Next(roleSamples.Count)];

							// Baseline (Grid)
							long startFrame = (long)step * framesPerSixteenth;

							// Swing (verzögere Offbeats – ungerade 1/16)
							if (swing > 0.0 && (step % 2 == 1))
							{
								startFrame += (long)Math.Round(swing * framesPerSixteenth);
							}

							// Humanize (Jitter)
							if (humanizeFrames > 0)
							{
								long jitter = rnd.NextInt64(-humanizeFrames, humanizeFrames + 1);
								startFrame = Math.Max(0, startFrame + jitter);
							}

							MixSample(buffer, sample, startFrame, framesPerSixteenth, framesPerBar, channels);
						}
						roleBuffers[role] = buffer;
					}, token));
				}

				await Task.WhenAll(roleTasks);

				// Summieren der Rollen in Endpuffer (parallel über Indizes)
				float[] finalBuffer = new float[outLen];
				await Task.Run(() =>
				{
					Parallel.For(0, outLen, new ParallelOptions { CancellationToken = token }, i =>
					{
						double acc = 0.0;
						foreach (var kv in roleBuffers)
						{
							var buf = kv.Value;
							if (i < buf.Length)
							{
								acc += buf[i];
							}
						}
						// soft clip
						finalBuffer[i] = (float)Math.Max(-1.0, Math.Min(1.0, acc));
					});
				}, token).ConfigureAwait(false);

				// Normalisieren leicht (optional via targetPeak)
				NormalizeInPlace(finalBuffer, targetPeak);

				// AudioObj erzeugen
				var loop = new AudioObj
				{
					Id = Guid.NewGuid(),
					Name = $"DrumLoop_{li + 1}_{bpm:F0}bpm",
					Data = finalBuffer,
					SampleRate = targetRate,
					Channels = targetChannels,
					BitDepth = 32,
					Bpm = (float)bpm,
					Length = finalBuffer.LongLength,
					Duration = TimeSpan.FromSeconds((double)(finalBuffer.LongLength / Math.Max(1, targetChannels)) / targetRate)
				};
				loops.Add(loop);

				progress?.Report(phase2 + (li + 1) * ((1.0 - phase2) / Math.Max(1, loopsCount)));
			}

			progress?.Report(1.0);
			return loops;
		}

		private static DrumRole CategorizeTag(string? tag)
		{
			string t = (tag ?? string.Empty).ToLowerInvariant();
			if (t.Contains("kick") || t.Contains("bd") || t.Contains("bassdrum"))
			{
				return DrumRole.Kick;
			}

			if (t.Contains("snare") || t.Contains("sd"))
			{
				return DrumRole.Snare;
			}

			if (t.Contains("hhc") || t.Contains("hihat c") || t.Contains("closed hat") || (t.Contains("hat") && t.Contains("closed")))
			{
				return DrumRole.HiHatClosed;
			}

			if (t.Contains("hho") || t.Contains("hihat o") || t.Contains("open hat") || (t.Contains("hat") && t.Contains("open")))
			{
				return DrumRole.HiHatOpen;
			}

			if (t.Contains("clap"))
			{
				return DrumRole.Clap;
			}

			if (t.Contains("ride"))
			{
				return DrumRole.Ride;
			}

			if (t.Contains("crash"))
			{
				return DrumRole.Crash;
			}

			if (t.Contains("tom"))
			{
				return DrumRole.Tom;
			}

			return DrumRole.Perc;
		}

		private static AudioObj EnsureRateAndChannels(AudioObj src, int targetRate, int targetChannels)
		{
			var data = src.Data ?? [];
			float[] resampled = src.SampleRate == targetRate ? data : ResampleLinear(data, src.SampleRate, targetRate, Math.Max(1, src.Channels));
			float[] chanMatched = MatchChannels(resampled, Math.Max(1, src.Channels), targetChannels);
			return new AudioObj
			{
				Id = Guid.NewGuid(),
				Name = src.Name,
				FilePath = src.FilePath,
				Data = chanMatched,
				SampleRate = targetRate,
				Channels = targetChannels,
				BitDepth = src.BitDepth,
				Bpm = src.Bpm,
				ScannedBpm = src.ScannedBpm,
				Timing = src.Timing,
				ScannedTiming = src.ScannedTiming,
				Volume = src.Volume,
				Length = chanMatched.LongLength,
				Duration = TimeSpan.FromSeconds((double)(chanMatched.LongLength / Math.Max(1, targetChannels)) / targetRate),
				SelectionStart = -1,
				SelectionEnd = -1,
				SampleTag = src.SampleTag
			};
		}

		private static AudioObj FitToGridDurations(AudioObj src, long framesPerSixteenth, int[] allowedSixteenthMultipliers)
		{
			int ch = Math.Max(1, src.Channels);
			long frames = src.Data.LongLength / ch;
			long[] allowedFrames = allowedSixteenthMultipliers.Select(m => Math.Max(1, framesPerSixteenth * m)).ToArray();
			long best = allowedFrames.Where(f => f <= frames).DefaultIfEmpty(allowedFrames[0]).Max();
			bool pad = false;
			if (best <= 0)
			{
				best = allowedFrames[0];
				pad = true;
			}
			else if (best < frames && !allowedFrames.Contains(frames))
			{
				pad = false;
			}
			else if (frames < allowedFrames[0])
			{
				best = allowedFrames[0];
				pad = true;
			}

			long outFrames = best;
			int outLen = checked((int)(outFrames * ch));
			float[] dst = new float[outLen];
			int copyLen = (int)Math.Min(dst.Length, src.Data.Length);
			Array.Copy(src.Data, 0, dst, 0, copyLen);

			return new AudioObj
			{
				Id = src.Id,
				Name = src.Name,
				FilePath = src.FilePath,
				Data = dst,
				SampleRate = src.SampleRate,
				Channels = src.Channels,
				BitDepth = src.BitDepth,
				Bpm = src.Bpm,
				ScannedBpm = src.ScannedBpm,
				Timing = src.Timing,
				ScannedTiming = src.ScannedTiming,
				Volume = src.Volume,
				Length = dst.LongLength,
				Duration = TimeSpan.FromSeconds((double)outFrames / Math.Max(1, src.SampleRate)),
				SampleTag = src.SampleTag
			};
		}

		private static float[] MatchChannels(float[] data, int srcCh, int dstCh)
		{
			if (srcCh == dstCh)
			{
				return data;
			}

			long frames = data.LongLength / Math.Max(1, srcCh);
			int outLen = checked((int)(frames * dstCh));
			float[] dst = new float[outLen];
			if (srcCh == 1 && dstCh > 1)
			{
				for (long f = 0; f < frames; f++)
				{
					float v = data[f];
					for (int c = 0; c < dstCh; c++)
					{
						dst[f * dstCh + c] = v;
					}
				}
			}
			else if (srcCh > 1 && dstCh == 1)
			{
				for (long f = 0; f < frames; f++)
				{
					double acc = 0;
					for (int c = 0; c < srcCh; c++)
					{
						acc += data[f * srcCh + c];
					}

					dst[f] = (float)(acc / srcCh);
				}
			}
			else
			{
				int minCh = Math.Min(srcCh, dstCh);
				for (long f = 0; f < frames; f++)
				{
					for (int c = 0; c < minCh; c++)
					{
						dst[f * dstCh + c] = data[f * srcCh + c];
					}

					for (int c = minCh; c < dstCh; c++)
					{
						dst[f * dstCh + c] = data[f * srcCh + (minCh - 1)];
					}
				}
			}
			return dst;
		}

		private static float[] ResampleLinear(float[] interleaved, int srcRate, int dstRate, int channels)
		{
			if (srcRate == dstRate || interleaved.Length == 0)
			{
				return interleaved;
			}

			long frames = interleaved.LongLength / Math.Max(1, channels);
			double ratio = (double)dstRate / srcRate;
			long outFrames = (long)Math.Round(frames * ratio);
			int outLen = checked((int)(outFrames * channels));
			float[] dst = new float[outLen];
			for (long of = 0; of < outFrames; of++)
			{
				double srcPos = of / ratio; // in frames
				long i0 = (long)Math.Floor(srcPos);
				long i1 = Math.Min(frames - 1, i0 + 1);
				double t = srcPos - i0;
				for (int c = 0; c < channels; c++)
				{
					double v0 = interleaved[Math.Clamp((int)(i0 * channels + c), 0, interleaved.Length - 1)];
					double v1 = interleaved[Math.Clamp((int)(i1 * channels + c), 0, interleaved.Length - 1)];
					dst[of * channels + c] = (float)(v0 + (v1 - v0) * t);
				}
			}
			return dst;
		}

		private static void MixSample(float[] buffer, AudioObj sample, long startFrame, long framesPerSixteenth, long framesPerBar, int channels)
		{
			int ch = Math.Max(1, channels);
			long totalFrames = buffer.LongLength / ch;
			long writeStart = Math.Max(0, startFrame);
			long writeEnd = Math.Min(totalFrames, writeStart + sample.Data.LongLength / Math.Max(1, sample.Channels));
			if (writeStart >= writeEnd)
			{
				return;
			}

			long framesToCopy = writeEnd - writeStart;

			var data = sample.Data;
			int sampleCh = Math.Max(1, sample.Channels);

			for (long f = 0; f < framesToCopy; f++)
			{
				long dstBase = (writeStart + f) * ch;
				long srcBase = f * sampleCh;
				for (int c = 0; c < ch; c++)
				{
					float s = data[Math.Clamp((int)(srcBase + Math.Min(c, sampleCh - 1)), 0, data.Length - 1)];
					buffer[dstBase + c] += s;
				}
			}
		}

		private static void NormalizeInPlace(float[] data, float targetPeak)
		{
			float max = 0f;
			for (int i = 0; i < data.Length; i++)
			{
				float a = Math.Abs(data[i]);
				if (a > max)
				{
					max = a;
				}
			}
			if (max <= 1e-6f)
			{
				return;
			}

			float scale = targetPeak / max;
			if (scale >= 1.0f)
			{
				return;
			}

			for (int i = 0; i < data.Length; i++)
			{
				data[i] *= scale;
			}
		}

		private static Dictionary<DrumRole, List<int>> BuildRolePatterns(HashSet<DrumRole> availableRoles, int totalSteps, Random rnd)
		{
			int bars = Math.Max(1, totalSteps / 16);
			var patterns = new Dictionary<DrumRole, List<int>>();

			bool hasKick = availableRoles.Contains(DrumRole.Kick);
			bool hasSnare = availableRoles.Contains(DrumRole.Snare);
			bool hasHat = availableRoles.Contains(DrumRole.HiHatClosed) || availableRoles.Contains(DrumRole.HiHatOpen);

			// Kick
			if (hasKick)
			{
				var steps = new List<int>();
				for (int b = 0; b < bars; b++)
				{
					int baseStep = b * 16;
					steps.Add(baseStep + 0); // 1
					steps.Add(baseStep + 8); // 3
					if (rnd.NextDouble() < 0.6)
					{
						steps.Add(baseStep + 6);
					}

					if (rnd.NextDouble() < 0.5)
					{
						steps.Add(baseStep + 10);
					}

					if (rnd.NextDouble() < 0.4)
					{
						steps.Add(baseStep + 12);
					}
				}
				patterns[DrumRole.Kick] = steps.Distinct().Where(i => i < totalSteps).OrderBy(i => i).ToList();
			}

			// Snare
			if (hasSnare)
			{
				var steps = new List<int>();
				for (int b = 0; b < bars; b++)
				{
					int baseStep = b * 16;
					steps.Add(baseStep + 4);  // 2
					steps.Add(baseStep + 12); // 4
					if (rnd.NextDouble() < 0.25)
					{
						steps.Add(baseStep + 14); // ghost
					}

					if (rnd.NextDouble() < 0.15)
					{
						steps.Add(baseStep + 2);
					}
				}
				patterns[DrumRole.Snare] = steps.Distinct().Where(i => i < totalSteps).OrderBy(i => i).ToList();
			}

			// Hi-Hat
			if (hasHat)
			{
				bool closed = availableRoles.Contains(DrumRole.HiHatClosed);
				var role = closed ? DrumRole.HiHatClosed : DrumRole.HiHatOpen;
				var steps = new List<int>();
				for (int b = 0; b < bars; b++)
				{
					int baseStep = b * 16;
					int density = rnd.NextDouble() < 0.5 ? 2 : 1; // 1/8 oder 1/16
					for (int s = 0; s < 16; s += density)
					{
						if (density == 1 || (density == 2 && (s % 4 != 0)))
						{
							steps.Add(baseStep + s);
						}
					}
				}
				patterns[role] = steps.Where(i => i < totalSteps).ToList();
			}

			// Clap
			if (availableRoles.Contains(DrumRole.Clap))
			{
				var steps = new List<int>();
				for (int b = 0; b < bars; b++)
				{
					int baseStep = b * 16;
					steps.Add(baseStep + 4);
					steps.Add(baseStep + 12);
					if (rnd.NextDouble() < 0.1)
					{
						steps.Add(baseStep + 6);
					}
				}
				patterns[DrumRole.Clap] = steps.Where(i => i < totalSteps).ToList();
			}

			// Ride / Crash
			if (availableRoles.Contains(DrumRole.Ride))
			{
				var steps = new List<int>();
				for (int b = 0; b < bars; b++)
				{
					steps.Add(b * 16 + 0);
					if (rnd.NextDouble() < 0.3)
					{
						steps.Add(b * 16 + 8);
					}
				}
				patterns[DrumRole.Ride] = steps;
			}
			if (availableRoles.Contains(DrumRole.Crash))
			{
				var steps = new List<int>();
				for (int b = 0; b < bars; b += Math.Max(1, rnd.Next(2, 5)))
				{
					steps.Add(b * 16 + 0);
				}
				patterns[DrumRole.Crash] = steps;
			}

			// Toms / Perc – sparsam
			if (availableRoles.Contains(DrumRole.Tom))
			{
				var steps = new List<int>();
				for (int b = 1; b <= bars; b += 4)
				{
					int baseStep = (b * 16) - 4;
					steps.Add(Math.Max(0, baseStep));
				}
				patterns[DrumRole.Tom] = steps.Where(i => i < totalSteps).ToList();
			}
			if (availableRoles.Contains(DrumRole.Perc))
			{
				var steps = new HashSet<int>();
				for (int i = 0; i < bars * 2; i++)
				{
					int pos = rnd.Next(0, 16 * bars);
					if (pos % 4 != 0)
					{
						steps.Add(pos);
					}
				}
				patterns[DrumRole.Perc] = steps.Where(i => i < totalSteps).OrderBy(i => i).ToList();
			}

			// Sonderfälle
			if (patterns.Count == 0 && availableRoles.Count > 0)
			{
				var only = availableRoles.First();
				patterns[only] = Enumerable.Range(0, bars).Select(b => b * 16 + (only == DrumRole.Kick ? 0 : 4)).ToList();
			}
			else if (availableRoles.Count == 2)								
			{
				var two = availableRoles.ToList();
				var r1 = two.Contains(DrumRole.Kick) ? DrumRole.Kick : two[0];
				var r2 = two.Contains(DrumRole.Snare) ? DrumRole.Snare : two[1];
				if (!patterns.ContainsKey(r1))
				{
					patterns[r1] = Enumerable.Range(0, bars).SelectMany(b => new[] { b * 16 + 0, b * 16 + 8 }).ToList();
				}
				if (!patterns.ContainsKey(r2))
				{
					patterns[r2] = Enumerable.Range(0, bars).SelectMany(b => new[] { b * 16 + 4, b * 16 + 12 }).ToList();
				}
			}

			return patterns;
		}
	}
}
