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

                // Loop-Grenzen nur setzen wenn Loop aktiv
                if (this.loopState.LoopEnabled)
                {
                    this.UpdateLoopBounds();
                }

                this.PlaybackCancellationTokens.AddOrUpdate(track.Id, new CancellationToken(), (key, oldValue) => new CancellationToken());
                this.button_playback.Text = "■";

                // Nur bei aktivem Loop Werte übergeben, sonst (0,0) damit linearer Provider erstellt wird
                long loopStartSample = 0;
                long loopEndSample = 0;
                if (this.loopState.LoopEnabled)
                {
                    loopStartSample = track.LoopStartFrames * track.Channels;
                    loopEndSample = track.LoopEndFrames * track.Channels;

                    // Wenn aktuelle Position außerhalb der Loop liegt, an den Loop-Anfang springen
                    if (track.Position >= 0 && (track.Position < track.LoopStartFrames || track.Position >= track.LoopEndFrames))
                    {
                        try { track.SetPosition(track.LoopStartFrames); } catch { }
                    }
                }

                await track.PlayAsync(this.PlaybackCancellationTokens.GetValueOrDefault(track.Id), onPlaybackStopped, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
            }
            else if (track.Paused)
            {
                long loopStartSample = 0;
                long loopEndSample = 0;
                if (this.loopState.LoopEnabled)
                {
                    loopStartSample = track.LoopStartFrames * track.Channels;
                    loopEndSample = track.LoopEndFrames * track.Channels;
                }

                await track.PlayAsync(this.PlaybackCancellationTokens.GetValueOrDefault(track.Id), null, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
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

            // One-shot on KeyDown only; ignore repeats until KeyUp resets
            if (this.playbackState.SpaceDownHandled)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            this.playbackState.SpaceDownHandled = true;

            try
            {
                if (track.Paused || track.Playing)
                {
                    this.button_pause.Invoke(this.button_pause.PerformClick);
                }
                else
                {
                    this.button_playback.Invoke(this.button_playback.PerformClick);
                }
            }
            finally
            {
                // keep handled until KeyUp clears it
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            // Fokus von Buttons entfernen
            this.ActiveControl = this.pictureBox_wave;
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
            this.loopState.CycleLoopFraction(ModifierKeys.HasFlag(Keys.Shift));
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
            // Sonst Position beibehalten

            this.button_loop.Text = this.loopState.CurrentLoopFractionDenominator.ToString();
            this.button_loop.Font = new Font(this.button_loop.Font.FontFamily, 7f, FontStyle.Regular);
            this.button_loop.ForeColor = Color.Green;

            this.UpdateViewingElements();
            _ = this.RedrawWaveformImmediateAsync();
            LogCollection.Log($"Loop bounds dynamic: {newLoopStart}-{newLoopEnd} (frames), position {track.Position}");
        }

        private async void button_loop_Click(object sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // Ctrl+Click: Disable looping, reset to default
                this.loopState.ResetLoop();

                // UI: black, 9pt, bold ring-arrow
                this.button_loop.Text = "↻";
                this.button_loop.Font = new Font(this.button_loop.Font.FontFamily, 9f, FontStyle.Bold);
                this.button_loop.ForeColor = Color.Black;

                // Loop im Track und Player immer deaktivieren, egal ob playing oder nicht
                var track = this.SelectedTrack;
                if (track != null)
                {
                    bool wasPlaying = track.PlayerPlaying;
                    track.DisableLoopNow();
                    track.LoopStartFrames = 0;
                    track.LoopEndFrames = track.Length / Math.Max(1, track.Channels);

                    // Nahtloses UI-Follow direkt nach Deaktivierung
                    try
                    {
                        int spp = Math.Max(1, this.SamplesPerPixel);
                        long viewFrames = (long)this.pictureBox_wave.Width * spp;

                        int ch = Math.Max(1, track.Channels);
                        long totalFrames = Math.Max(1, track.Length / ch);
                        long maxOffset = Math.Max(0, totalFrames - viewFrames);

                        long caretFrame = track.Position; // jetzt linear, aus Provider korrekt
                        float caretPosClamped = Math.Clamp(this.CaretPosition, 0f, 1f);
                        long caretInViewFrames = (long)Math.Round(viewFrames * caretPosClamped);
                        long wanted = Math.Clamp(caretFrame - caretInViewFrames, 0, maxOffset);

                        this.ViewOffsetFrames = wanted;
                        this.ClampViewOffset();
                        this.SuppressScrollEvent = true;
                        try { this.RecalculateScrollBar(); }
                        finally { this.SuppressScrollEvent = false; }
                        track.ScrollOffset = this.ViewOffsetFrames;

                        // Einen Tick lang Follow erzwingen, falls Sync aus ist
                        if (wasPlaying)
                        {
                            this.AddPlaybackForceFollow(track.Id);
                            // optional später entfernen (kleines Zeitfenster reicht für 1-2 Frames)
                            _ = Task.Run(async () => { await Task.Delay(200); this.RemovePlaybackForceFollow(track.Id); });
                        }
                    }
                    catch { }
                }

                this.UpdateViewingElements();
                _ = this.RedrawWaveformImmediateAsync();
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

                    // Bestimme Loop-Grenzen anhand Auswahl, aber ändre Position nicht automatisch
                    this.UpdateLoopBounds(adjustStartIfOutside: false);

                    var track = this.SelectedTrack;
                    if (track != null)
                    {
                        long loopStartSample = track.LoopStartFrames * track.Channels;
                        long loopEndSample = track.LoopEndFrames * track.Channels;

                        if (track.PlayerPlaying)
                        {
                            long pos = track.Position;
                            bool inside = pos >= track.LoopStartFrames && pos < track.LoopEndFrames;
                            if (inside)
                            {
                                // Wechsel live in Loop-Provider ohne Positionsänderung
                                track.EnableLoopNow(loopStartSample, loopEndSample);
                            }
                            else
                            {
                                // Außerhalb: erst stoppen, dann von Beginn der Auswahl starten (kein Overlap!)
                                await track.StopAsync();
                                track.SetPosition(track.LoopStartFrames);
                                track.StartingOffset = track.LoopStartFrames * Math.Max(1, track.Channels);

                                this.AddPlaybackForceFollow(track.Id);
                                this.button_playback.Text = "■";
                                await track.PlayAsync(CancellationToken.None, () =>
                                {
                                    this.button_playback.Invoke(() => this.button_playback.Text = "▶");
                                    this.RemovePlaybackForceFollow(track.Id);
                                }, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
                            }
                        }
                        else if (track.Paused)
                        {
                            // Pausiert: Position nicht ändern, nur Loop-Provider vorbereiten
                            // Beim Resume wird Loop berücksichtigt
                            // Nichts weiter zu tun
                        }
                        else
                        {
                            // Nicht spielend: nichts starten, nur Grenzen gesetzt lassen
                        }
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
                        this.loopState.CycleLoopFraction(ModifierKeys.HasFlag(Keys.Shift));
                        this.UpdateLoopBounds();
                        this.UpdateViewingElements();
                        _ = this.RedrawWaveformImmediateAsync();
                    }

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

        private bool IsEditingContext()
        {
            var focused = this.ActiveControl;
            // Timestamp-Textbox soll Hotkeys nicht blockieren
            if (focused == this.textBox_timestamp)
            {
                return false;
            }
            return focused is TextBoxBase || focused is ComboBox || focused is NumericUpDown;
        }
    }
}
