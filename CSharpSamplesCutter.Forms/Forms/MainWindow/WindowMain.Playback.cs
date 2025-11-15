using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private async void button_playback_Click(object sender, EventArgs e)
        {
            if (this.SelectedGuids.Count > 1)
            {
                await this.AudioC.PlayManyAsync(this.SelectedGuids, this.Volume);
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                MessageBox.Show("No track selected to play.", "Play Track", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var onPlaybackStopped = new Action(() =>
            {
                this.Invoke(() => { this.button_playback.Text = "▶"; });
            });

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                await this.AudioC.StopAllAsync();
                await this.AudioC_res.StopAllAsync();
                this.PlaybackCancellationTokens.Clear();
                LogCollection.Log("All tracks stopped.");
                return;
            }

            if (!track.Playing && !track.Paused)
            {
                if (this.SoloPlayback)
                {
                    await this.AudioC.StopAllAsync(false);
                    await this.AudioC_res.StopAllAsync(false);
                }

                // Loop-Grenzen vor PlayAsync setzen
                if (this.loopState.LoopEnabled)
                {
                    this.UpdateLoopBounds();
                }

                this.PlaybackCancellationTokens.AddOrUpdate(track.Id, new CancellationToken(), (key, oldValue) => new CancellationToken());
                this.button_playback.Text = "■";

                long loopStartSample = track.LoopStartFrames * track.Channels;
                long loopEndSample = track.LoopEndFrames * track.Channels;

                // Nur wenn aktuelle Position außerhalb der Loop liegt, an den Loop-Anfang springen
                if (this.loopState.LoopEnabled && track.Position >= 0 && (track.Position < track.LoopStartFrames || track.Position >= track.LoopEndFrames))
                {
                    try { track.SetPosition(track.LoopStartFrames); } catch { }
                }

                await track.PlayAsync(this.PlaybackCancellationTokens.GetValueOrDefault(track.Id), onPlaybackStopped, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
            }
            else if (track.Paused)
            {
                // Resume mit/ohne Loop-Info
                if (this.loopState.LoopEnabled)
                {
                    long loopStartSample = track.LoopStartFrames * track.Channels;
                    long loopEndSample = track.LoopEndFrames * track.Channels;
                    await track.PlayAsync(this.PlaybackCancellationTokens.GetValueOrDefault(track.Id), null, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
                }
                else
                {
                    await track.PlayAsync(this.PlaybackCancellationTokens.GetValueOrDefault(track.Id), null, this.Volume);
                }
            }
            else
            {
                await track.StopAsync();
                this.PlaybackCancellationTokens.TryRemove(track.Id, out _);
                this.button_playback.Text = "▶";
            }
        }

        private async void button_pause_Click(object sender, EventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                LogCollection.Log("No track selected to pause.");
                return;
            }

            await track.PauseAsync();
        }

        private async void Form_Space_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Space || e.Handled)
            {
                return;
            }

            if (this.IsEditingContext())
            {
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (this.SpaceKeyDebounceActive || (now - this.LastSpaceToggleUtc).TotalMilliseconds < 180)
            {
                return;
            }

            this.SpaceKeyDebounceActive = true;
            this.LastSpaceToggleUtc = now;

            try
            {
                if (track.Playing)
                {
                    await track.PauseAsync();
                    this.button_pause.ForeColor = Color.Black;
                }
                else if (track.Paused)
                {
                    // Resume: Loop-Grenzen weitergeben
                    if (this.loopState.LoopEnabled)
                    {
                        long loopStartSample = track.LoopStartFrames * track.Channels;
                        long loopEndSample = track.LoopEndFrames * track.Channels;

                        await track.PlayAsync(
                            CancellationToken.None,
                            null,
                            this.Volume,
                            loopStartSample: loopStartSample,
                            loopEndSample: loopEndSample,
                            desiredLatency: 120);
                    }
                    else
                    {
                        await track.PlayAsync(CancellationToken.None, null, this.Volume, desiredLatency: 120);
                    }
                    this.button_pause.ForeColor = Color.Black;
                }
                else
                {
                    // Start: ggf. Loop-Grenzen aktualisieren
                    if (this.loopState.LoopEnabled)
                    {
                        this.UpdateLoopBounds();
                    }

                    var onPlaybackStopped = new Action(() =>
                    {
                        this.button_playback.Invoke(() => this.button_playback.Text = "▶");
                        this.PlaybackCancellationTokens.TryRemove(track.Id, out _);
                    });

                    var cts = new CancellationTokenSource();
                    this.PlaybackCancellationTokens[track.Id] = cts.Token;
                    this.button_playback.Text = "■";

                    long loopStartSample = track.LoopStartFrames * track.Channels;
                    long loopEndSample = track.LoopEndFrames * track.Channels;

                    // Start NICHT erzwingen: nur falls außerhalb der Loop-Grenzen zur Loop-Startposition springen
                    if (this.loopState.LoopEnabled && track.Position >= 0 && (track.Position < track.LoopStartFrames || track.Position >= track.LoopEndFrames))
                    {
                        try { track.SetPosition(track.LoopStartFrames); } catch { }
                    }

                    await track.PlayAsync(cts.Token, onPlaybackStopped, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample, desiredLatency: 120);
                }
            }
            finally
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(120);
                    this.SpaceKeyDebounceActive = false;
                });
            }

            e.Handled = true;
        }

        private void vScrollBar_volume_Scroll(object sender, ScrollEventArgs e)
        {
            this.label_volume.Text = $"Vol {this.Volume * 100.0f:0.0}";

            if (this.SelectedTrack != null)
            {
                this.SelectedTrack.SetVolume(this.Volume);
            }

            if (e.Type == ScrollEventType.EndScroll)
            {
                LogCollection.Log($"Set volume to: {this.Volume * 100.0f:0.0}%");
            }
        }

        private async void button_copy_Click(object sender, EventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                MessageBox.Show("No track selected to copy.", "Copy Track", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Save view state vor Operation
            this.SaveViewState();

            bool shiftFlag = ModifierKeys.HasFlag(Keys.Shift);

            bool isInMain = this.AudioC.Audios.Any(a => a.Id == track.Id);
            bool isInReserve = this.AudioC_res.Audios.Any(a => a.Id == track.Id);
            var preservedIds = new List<Guid>();
            if (isInMain)
            {
                preservedIds = this.listBox_audios.SelectedItems.Cast<AudioObj>().Select(a => a.Id).ToList();
            }
            else if (isInReserve)
            {
                preservedIds = this.listBox_reserve.SelectedItems.Cast<AudioObj>().Select(a => a.Id).ToList();
            }

            var copiedTrack = await track.CloneFromSelectionAsync();
            if (copiedTrack == null)
            {
                LogCollection.Log($"Failed to copy {track.Name}.");
                this.RestoreViewState();
                return;
            }

            if (isInMain)
            {
                if (shiftFlag)
                {
                    this.AudioC_res.Audios.Add(copiedTrack);
                    LogCollection.Log($"{track.Name} copied to Reserve list (Shift+Copy).");
                }
                else
                {
                    this.AudioC.Audios.Add(copiedTrack);
                    LogCollection.Log($"{track.Name} copied within Main list.");
                }
                this.listBox_reserve.SelectedIndex = -1;

                this.listBox_audios.BeginUpdate();
                try
                {
                    this.listBox_audios.SelectedIndices.Clear();
                    foreach (var id in preservedIds)
                    {
                        int idx = this.AudioC.Audios.FirstOrDefault(a => a.Id == id) is AudioObj obj ? this.AudioC.Audios.IndexOf(obj) : -1;
                        if (idx >= 0)
                        {
                            this.listBox_audios.SelectedIndices.Add(idx);
                        }
                    }
                }
                finally
                {
                    this.listBox_audios.EndUpdate();
                }
            }
            else if (isInReserve)
            {
                if (shiftFlag)
                {
                    this.AudioC.Audios.Add(copiedTrack);
                    LogCollection.Log($"{track.Name} copied to Main list (Shift+Copy).");
                }
                else
                {
                    this.AudioC_res.Audios.Add(copiedTrack);
                    LogCollection.Log($"{track.Name} copied within Reserve list.");
                }
                this.listBox_audios.SelectedIndex = -1;

                this.listBox_reserve.BeginUpdate();
                try
                {
                    this.listBox_reserve.SelectedIndices.Clear();
                    foreach (var id in preservedIds)
                    {
                        int idx = this.AudioC_res.Audios.FirstOrDefault(a => a.Id == id) is AudioObj obj ? this.AudioC_res.Audios.IndexOf(obj) : -1;
                        if (idx >= 0)
                        {
                            this.listBox_reserve.SelectedIndices.Add(idx);
                        }
                    }
                }
                finally
                {
                    this.listBox_reserve.EndUpdate();
                }
            }
            else
            {
                LogCollection.Log($"Failed to determine source list for copying track: {track.Name}");
            }

            // Restore view state nach Operation
            this.RestoreViewState();
        }

        private (long loopStartFrame, long loopEndFrame) GetLoopRange()
        {
            if (!this.loopState.LoopEnabled)
            {
                return (0, 0);
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return (0, 0);
            }

            int channels = Math.Max(1, track.Channels);
            long totalFrames = Math.Max(1, track.Length / channels);

            long loopStartFrames;
            long loopEndFrames;

            if (track.SelectionStart >= 0 && track.SelectionEnd >= 0 && track.SelectionStart != track.SelectionEnd)
            {
                long selStart = track.SelectionStart;
                long selEnd = track.SelectionEnd;
                if (selEnd < selStart)
                {
                    (selStart, selEnd) = (selEnd, selStart);
                }
                loopStartFrames = selStart / channels;
                loopEndFrames = Math.Min(totalFrames, selEnd / channels);
            }
            else
            {
                loopStartFrames = 0;
                loopEndFrames = totalFrames;
            }

            long duration = Math.Max(1, loopEndFrames - loopStartFrames);
            int denominator = Math.Max(1, this.loopState.CurrentLoopFractionDenominator);
            long segment = duration / denominator;
            if (segment <= 0)
            {
                segment = 1; // mindestens 1 Frame loopen
            }
            long calculatedEnd = loopStartFrames + segment;
            if (calculatedEnd > loopEndFrames)
            {
                calculatedEnd = loopEndFrames; // nicht über ursprüngliche Auswahl hinaus
            }
            if (calculatedEnd <= loopStartFrames)
            {
                calculatedEnd = loopStartFrames + 1; // Sicherheitsfallback
            }
            return (loopStartFrames, Math.Min(calculatedEnd, totalFrames));
        }

        private void UpdateLoopBounds(bool adjustStartIfOutside = false)
        {
            var track = this.SelectedTrack;
            if (track == null || !this.loopState.LoopEnabled)
            {
                return;
            }

            var (loopStartFrame, loopEndFrame) = this.GetLoopRange();
            track.LoopStartFrames = loopStartFrame;
            track.LoopEndFrames = loopEndFrame;

            if (adjustStartIfOutside)
            {
                long pos = track.Position;
                if (pos < loopStartFrame || pos >= loopEndFrame)
                {
                    track.SetPosition(loopStartFrame);
                    track.StartingOffset = loopStartFrame * track.Channels;
                }
            }

            LogCollection.Log($"Loop bounds updated: {loopStartFrame}-{loopEndFrame} (frames)");
        }

        private void UpdateLoopBoundsDynamic()
        {
            var track = this.SelectedTrack;
            if (track == null || !this.loopState.LoopEnabled || !track.PlayerPlaying)
            {
                return;
            }

            long currentFrames = track.Position;
            this.loopState.CycleLoopFraction();
            var (newLoopStart, newLoopEnd) = this.GetLoopRange();

            // Apply bounds
            track.LoopStartFrames = newLoopStart;
            track.LoopEndFrames = newLoopEnd;

            long loopStartSample = newLoopStart * track.Channels;
            long loopEndSample = newLoopEnd * track.Channels;
            track.UpdateLoopBoundsDuringPlayback(loopStartSample, loopEndSample);

            // Nur springen wenn außerhalb des neuen Bereichs
            if (currentFrames < newLoopStart || currentFrames >= newLoopEnd)
            {
                track.SetPosition(newLoopStart);
                track.StartingOffset = newLoopStart * track.Channels;
            }

            this.button_loop.Text = this.loopState.CurrentLoopFractionDenominator.ToString();
            this.button_loop.Font = new Font(this.button_loop.Font.FontFamily, 7f, FontStyle.Regular);
            this.button_loop.ForeColor = Color.Green;

            LogCollection.Log($"Loop bounds dynamic: {newLoopStart}-{newLoopEnd} (frames), position {track.Position}");
        }

        private void button_loop_Click(object sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // Ctrl+Click: Disable looping, reset to default
                this.loopState.ResetLoop();

                // UI: black, 9pt, bold ring-arrow
                this.button_loop.Text = "↻";
                this.button_loop.Font = new Font(this.button_loop.Font.FontFamily, 9f, FontStyle.Bold);
                this.button_loop.ForeColor = Color.Black;

                // If playing: immediately switch to linear provider and realign caret baseline
                var track = this.SelectedTrack;
                if (track != null && track.PlayerPlaying)
                {
                    track.DisableLoopNow();
                }

                LogCollection.Log("Loop disabled");
            }
            else
            {
                // Normal click
                if (!this.loopState.LoopEnabled)
                {
                    // Enable loop with first fraction (1/1)
                    this.loopState.LoopEnabled = true;
                    this.loopState.LoopFractionIndex = 0;

                    this.button_loop.Text = this.loopState.CurrentLoopFractionDenominator.ToString();
                    this.button_loop.Font = new Font(this.button_loop.Font.FontFamily, 7f, FontStyle.Regular);
                    this.button_loop.ForeColor = Color.Green;
                    LogCollection.Log($"Loop enabled: {this.loopState.GetLoopFractionString()}");

                    // Setze Grenzen (nur verschieben wenn aktuelle Position draußen)
                    this.UpdateLoopBounds(adjustStartIfOutside: true);

                    var track = this.SelectedTrack;
                    if (track != null && track.PlayerPlaying)
                    {
                        long loopStartSample = track.LoopStartFrames * track.Channels;
                        long loopEndSample = track.LoopEndFrames * track.Channels;
                        track.EnableLoopNow(loopStartSample, loopEndSample);
                    }
                }
                else
                {
                    // Cycle loop fraction
                    var track = this.SelectedTrack;
                    if (track != null && track.PlayerPlaying)
                    {
                        this.UpdateLoopBoundsDynamic();
                    }
                    else
                    {
                        this.loopState.CycleLoopFraction();
                        this.UpdateLoopBounds();
                    }

                    // UI update: denominator only, green, 7pt
                    this.button_loop.Text = this.loopState.CurrentLoopFractionDenominator.ToString();
                    this.button_loop.Font = new Font(this.button_loop.Font.FontFamily, 7f, FontStyle.Regular);
                    this.button_loop.ForeColor = Color.Green;

                    LogCollection.Log($"Loop changed to: {this.loopState.GetLoopFractionString()}");
                }
            }
        }

        private async void checkBox_solo_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_solo.Checked)
            {
                Guid selectedId = this.SelectedTrack?.Id ?? Guid.Empty;
                var playingIds = this.AudioC.Playing.Where(i => i != selectedId).ToList();
                await this.AudioC.StopManyAsync(playingIds);
            }
        }
    }
}
