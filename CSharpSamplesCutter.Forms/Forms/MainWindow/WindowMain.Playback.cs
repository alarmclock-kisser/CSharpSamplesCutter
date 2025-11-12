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

                // ✅ Loop-Grenzen VOR PlayAsync setzen (nutzt GetLoopRange)
                if (this.loopState.LoopEnabled)
                {
                    this.UpdateLoopBounds();
                }

                this.PlaybackCancellationTokens.AddOrUpdate(track.Id, new CancellationToken(), (key, oldValue) => new CancellationToken());
                this.button_playback.Text = "■";
                
                // ✅ Loop-Grenzen als SAMPLES an PlayAsync übergeben!
                long loopStartSample = track.LoopStartFrames * track.Channels;
                long loopEndSample = track.LoopEndFrames * track.Channels;
                
                await track.PlayAsync(this.PlaybackCancellationTokens.GetValueOrDefault(track.Id), onPlaybackStopped, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
            }
            else
            {
                await track.StopAsync();
                this.PlaybackCancellationTokens.TryRemove(track.Id, out _);
                this.button_playback.Text = "▶";
            }
        }

        /// <summary>
        /// ✅ Berechnet Loop-Grenzen basierend auf Selection oder ganzer Track
        /// WICHTIG: LoopStartFrames/LoopEndFrames sind in FRAMES, aber PlayAsync braucht SAMPLES!
        /// </summary>
        private void SetupLoopBounds(AudioObj track)
        {
            if (track == null || !this.loopState.LoopEnabled)
            {
                return;
            }

            int denominator = this.loopState.CurrentLoopFractionDenominator;
            int ch = Math.Max(1, track.Channels);

            // ✅ Wenn SELECTION vorhanden: Loop-Bereich auf Selection setzen
            if (track.SelectionStart >= 0 && track.SelectionEnd >= 0 && track.SelectionEnd > track.SelectionStart)
            {
                long selStartSample = track.SelectionStart; // bereits in Samples!
                long selEndSample = track.SelectionEnd;     // bereits in Samples!
                long selLength = selEndSample - selStartSample;

                // Loop-Fraction des Selections-Bereichs
                long loopLength = selLength / denominator;
                loopLength = Math.Max(ch, loopLength); // Mindestens 1 Frame = ch samples

                track.LoopStartFrames = selStartSample / ch;
                track.LoopEndFrames = track.LoopStartFrames + (loopLength / ch);

                // ✅ WICHTIG: StartingOffset auf LoopStart setzen!
                track.StartingOffset = track.LoopStartFrames * ch;

                LogCollection.Log($"Loop set to selection: 1/{denominator} = {loopLength} samples (frames {track.LoopStartFrames}..{track.LoopEndFrames})");
            }
            else
            {
                // ✅ Wenn KEINE SELECTION: Loop-Bereich auf ganze Track setzen
                long totalSamples = track.Length;  // in Samples (interleaved)!
                long loopSamples = totalSamples / denominator;
                loopSamples = Math.Max(ch, loopSamples); // Mindestens 1 Frame

                track.LoopStartFrames = 0;
                track.LoopEndFrames = loopSamples / ch;

                // ✅ WICHTIG: StartingOffset auf LoopStart setzen!
                track.StartingOffset = 0;

                LogCollection.Log($"Loop set to whole track: 1/{denominator} = {loopSamples} samples (frames {track.LoopStartFrames}..{track.LoopEndFrames})");
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

        /// <summary>
        /// ✅ Berechnet den Loop-Range basierend auf aktuellem Loop-Status und Selection
        /// </summary>
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
            long totalFrames = track.Length / channels;

            // Bestimme Loop-Range: Selection oder ganzer Track
            long loopStartFrames, loopEndFrames;

            if (track.SelectionStart >= 0 && track.SelectionEnd >= 0 && track.SelectionStart != track.SelectionEnd)
            {
                // Mit Selection
                long selStart = track.SelectionStart;
                long selEnd = track.SelectionEnd;
                if (selEnd < selStart)
                {
                    (selStart, selEnd) = (selEnd, selStart);
                }

                loopStartFrames = selStart / channels;
                loopEndFrames = selEnd / channels;
            }
            else
            {
                // Ganzer Track
                loopStartFrames = 0;
                loopEndFrames = totalFrames;
            }

            // Berechne Loop-Endpunkt basierend auf aktuellem Bruch
            int denominator = this.loopState.CurrentLoopFractionDenominator;
            long loopDurationFrames = loopEndFrames - loopStartFrames;
            long loopSegmentFrames = loopDurationFrames / denominator;

            long calculatedLoopEndFrames = loopStartFrames + loopSegmentFrames;

            return (loopStartFrames, calculatedLoopEndFrames);
        }

        /// <summary>
        /// ✅ Aktualisiert Loop-Grenzen basierend auf aktuellem Loop-Status
        /// </summary>
        private void UpdateLoopBounds()
        {
            var track = this.SelectedTrack;
            if (track == null || !this.loopState.LoopEnabled)
            {
                return;
            }

            var (loopStartFrame, loopEndFrame) = this.GetLoopRange();
            
            track.LoopStartFrames = loopStartFrame;
            track.LoopEndFrames = loopEndFrame;
            
            // ✅ WICHTIG: StartingOffset auf LoopStart setzen
            track.StartingOffset = loopStartFrame * track.Channels;
            
            LogCollection.Log($"Loop bounds updated: {loopStartFrame}..{loopEndFrame} frames");
        }

        /// <summary>
        /// ✅ DYNAMISCHE Loop-Bruch-Änderung während Playback
        /// Prüft: Ist aktuelle Position noch im neuen Bruch? → Weiterspielen. Nein? → Zurück zu Start.
        /// </summary>
        private void UpdateLoopBoundsDynamic()
        {
            var track = this.SelectedTrack;
            if (track == null || !this.loopState.LoopEnabled || !track.PlayerPlaying)
            {
                return;
            }

            // Aktuelle Position in Frames
            long currentFrames = track.Position;
            
            // Alle Brüche durchprobieren
            var (oldLoopStart, oldLoopEnd) = this.GetLoopRange();
            
            // Cycle zum nächsten Bruch
            this.loopState.CycleLoopFraction();
            var (newLoopStart, newLoopEnd) = this.GetLoopRange();
            
            // Update Track Loop-Grenzen
            track.LoopStartFrames = newLoopStart;
            track.LoopEndFrames = newLoopEnd;
            
            this.button_loop.Text = this.loopState.GetLoopFractionString();
            this.button_loop.ForeColor = System.Drawing.Color.Green;

            // ✅ INTELLIGENTE Logik:
            // Ist die aktuelle Position noch im neuen Loop-Bereich?
            if (currentFrames >= newLoopStart && currentFrames < newLoopEnd)
            {
                // ✅ JA: Weiterspielen mit neuer Grenze
                LogCollection.Log($"Loop bounds updated (playing): {newLoopStart}..{newLoopEnd} frames (continuing at {currentFrames})");
            }
            else
            {
                // ✅ NEIN: Zurück zum Loop-Start
                track.StartingOffset = newLoopStart * track.Channels;
                track.SetPosition(newLoopStart);
                LogCollection.Log($"Loop bounds updated (playing): {newLoopStart}..{newLoopEnd} frames (jumped to start)");
            }
        }

        private void button_loop_Click(object sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                // Ctrl+Click: Disable looping, reset to default
                this.loopState.ResetLoop();
                this.button_loop.Text = "↻";
                this.button_loop.ForeColor = System.Drawing.Color.Black;
                LogCollection.Log("Loop disabled");
            }
            else
            {
                // Normal click
                if (!this.loopState.LoopEnabled)
                {
                    // Aktiviere Loop mit erstem Bruch (1/1)
                    this.loopState.LoopEnabled = true;
                    this.loopState.LoopFractionIndex = 0;
                    this.button_loop.Text = this.loopState.GetLoopFractionString();
                    this.button_loop.ForeColor = System.Drawing.Color.Green;
                    LogCollection.Log($"Loop enabled: {this.loopState.GetLoopFractionString()}");
                    
                    // ✅ Setze Loop-Grenzen neu
                    this.UpdateLoopBounds();
                }
                else
                {
                    // ✅ Cycle durch die Brüche - DYNAMISCH während Playback!
                    var track = this.SelectedTrack;
                    if (track != null && track.PlayerPlaying)
                    {
                        // Loop läuft: Intelligente Bruch-Änderung
                        this.UpdateLoopBoundsDynamic();
                    }
                    else
                    {
                        // Kein Playback: Normal cyclen
                        this.loopState.CycleLoopFraction();
                        this.button_loop.Text = this.loopState.GetLoopFractionString();
                        this.button_loop.ForeColor = System.Drawing.Color.Green;
                        this.UpdateLoopBounds();
                    }
                    
                    LogCollection.Log($"Loop changed to: {this.loopState.GetLoopFractionString()}");
                }
            }
        }

        private async void checkBox_solo_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_solo.Checked)
            {
                Guid selectedId = this.SelectedTrack?.Id ?? Guid.Empty;
                var playingIds = this.AudioC.Playing.Where(i => i != selectedId);
                await this.AudioC.StopManyAsync(playingIds);
            }
        }
    }
}
