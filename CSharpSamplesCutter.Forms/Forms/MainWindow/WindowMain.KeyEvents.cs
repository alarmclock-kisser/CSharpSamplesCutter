using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        /// <summary>
        /// Master keyboard event handler that prevents events when user is editing.
        /// </summary>
        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            // Skip handling if user is editing in a control (except for allowed keys)
            if (this.IsEditingContext())
            {
                // Allow some critical hotkeys to work even while editing
                if (!this.IsAllowedWhileEditing(e))
                {
                    return;
                }
            }

            // Route to appropriate handler based on key combination
            if (e.Control && e.KeyCode == Keys.Z && !e.Handled)
            {
                this.Form_CtrlZ_Pressed(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.Y && !e.Handled)
            {
                this.Form_CtrlY_Pressed(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.C && !e.Handled)
            {
                this.Form_CtrlC_Pressed(sender, e);
            }
            else if (e.KeyCode == Keys.Delete && !e.Handled)
            {
                this.Form_Del_Pressed(sender, e);
            }
            else if (e.KeyCode == Keys.Back && !e.Handled)
            {
                this.Form_Back_Pressed(sender, e);
            }
            else if (e.KeyCode == Keys.Space && !e.Handled)
            {
                this.Form_Space_Pressed(sender, e);
            }
        }

        /// <summary>
        /// Determines if a key combination should be allowed while editing.
        /// </summary>
        private bool IsAllowedWhileEditing(KeyEventArgs e)
        {
            // Allow Undo/Redo even while editing in most controls
            if ((e.Control && e.KeyCode == Keys.Z) || (e.Control && e.KeyCode == Keys.Y))
            {
                return true;
            }

            // Allow Escape to potentially cancel editing
            if (e.KeyCode == Keys.Escape)
            {
                return true;
            }

            return false;
        }

        private async void Form_CtrlZ_Pressed(object? sender, KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode != Keys.Z || e.Handled)
            {
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            // ✅ Bestimme, zu welcher Collection das Sample gehört
            AudioCollection targetCollection;
            if (this.AudioC.Audios.Any(a => a.Id == track.Id))
            {
                targetCollection = this.AudioC;
            }
            else if (this.AudioC_res.Audios.Any(a => a.Id == track.Id))
            {
                targetCollection = this.AudioC_res;
            }
            else
            {
                LogCollection.Log($"Track {track.Name} not found in any collection");
                return;
            }

            bool ok = await targetCollection.UndoAsync(track.Id);
            if (!ok)
            {
                LogCollection.Log($"No undo steps available for track: {track.Name}");
                return;
            }

            this.StepsBack = 0;
            this.UpdateSelectedCollectionListBox();
            this.UpdateViewingElements();
            this.UpdateUndoLabel();
            LogCollection.Log($"Undo applied on track: {track.Name}");

            e.Handled = true;
        }

        private async void Form_CtrlY_Pressed(object? sender, KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode != Keys.Y || e.Handled)
            {
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            // ✅ Bestimme, zu welcher Collection das Sample gehört
            AudioCollection targetCollection;
            if (this.AudioC.Audios.Any(a => a.Id == track.Id))
            {
                targetCollection = this.AudioC;
            }
            else if (this.AudioC_res.Audios.Any(a => a.Id == track.Id))
            {
                targetCollection = this.AudioC_res;
            }
            else
            {
                LogCollection.Log($"Track {track.Name} not found in any collection");
                return;
            }

            bool ok = await targetCollection.RedoAsync(track.Id);
            if (!ok)
            {
                LogCollection.Log($"No redo step available for track: {track.Name}");
                return;
            }

            this.StepsBack = 0;
            this.UpdateSelectedCollectionListBox();
            this.UpdateViewingElements();
            this.UpdateUndoLabel();
            LogCollection.Log($"Redo applied on track: {track.Name}");

            e.Handled = true;
        }

        private async void Form_CtrlC_Pressed(object? sender, KeyEventArgs e)
        {
            if (!e.Control || e.KeyCode != Keys.C || e.Handled)
            {
                return;
            }

            if (!this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            this.button_copy.PerformClick();
            await Task.CompletedTask;

            e.Handled = true;
        }

        private async void Form_Del_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete || e.Handled)
            {
                return;
            }

            if (!this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            long startSample = track.SelectionStart >= 0
                ? track.SelectionStart
                : track.Playing
                    ? track.Position * Math.Max(1, track.Channels)
                    : this.GetFrameUnderCursor() * Math.Max(1, track.Channels);
            long endSample = track.SelectionEnd >= 0 ? track.SelectionEnd : track.Length;

            // ✅ Bestimme, zu welcher Collection das Sample gehört
            AudioCollection targetCollection;
            if (this.AudioC.Audios.Any(a => a.Id == track.Id))
            {
                targetCollection = this.AudioC;
            }
            else if (this.AudioC_res.Audios.Any(a => a.Id == track.Id))
            {
                targetCollection = this.AudioC_res;
            }
            else
            {
                LogCollection.Log($"Track {track.Name} not found in any collection");
                return;
            }

            // ✅ Snapshot in der richtigen Collection erstellen
            await targetCollection.PushSnapshotAsync(track.Id);
            this.UpdateUndoLabel();
            
            await track.EraseSelectionAsync(startSample, endSample);
            LogCollection.Log($"Deleted selection on track: {track.Name}");

            this.UpdateSelectedCollectionListBox();
            this.UpdateViewingElements();

            e.Handled = true;
        }

        private async void Form_Back_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Back || e.Handled)
            {
                return;
            }

            if (this.IsEditingContext())
            {
                return;
            }

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                await this.AudioC.StopAllAsync();
                await this.AudioC_res.StopAllAsync();
                this.button_playback.Invoke(() => { this.button_playback.Text = "▶"; });
                this.PlaybackCancellationTokens.Clear();
                e.Handled = true;
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            if (this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
            {
                bool isPlaying = track.Playing;
                bool isPaused = track.Paused;
                int ch = Math.Max(1, track.Channels);

                // ✅ Bei Loop: Nutze GetLoopRange() um zum Loop-Start zu springen
                long targetFrame = 0;
                if (this.loopState.LoopEnabled)
                {
                    var (loopStart, loopEnd) = this.GetLoopRange();
                    targetFrame = loopStart;
                }
                else
                {
                    targetFrame = track.StartingOffset > 0 
                        ? track.StartingOffset / ch 
                        : 0;
                }

                if (isPlaying || isPaused)
                {
                    try
                    {
                        await track.PauseAsync();
                    }
                    catch
                    {
                    }

                    track.SetPosition(targetFrame);
                    track.StartingOffset = targetFrame * ch;

                    int width = Math.Max(1, this.pictureBox_wave.Width);
                    int spp = this.SamplesPerPixel;
                    long viewFrames = (long) width * Math.Max(1, spp);
                    long totalFrames = Math.Max(1, track.Length / Math.Max(1, ch));
                    long maxOffset = Math.Max(0, totalFrames - viewFrames);
                    float caretPosClamped = Math.Clamp(this.CaretPosition, 0f, 1f);
                    long caretInViewFrames = (long) Math.Round(viewFrames * caretPosClamped);
                    long newOffset = targetFrame - caretInViewFrames;
                    this.ViewOffsetFrames = Math.Clamp(newOffset, 0, maxOffset);
                    this.ClampViewOffset();
                    this.RecalculateScrollBar();
                    track.ScrollOffset = this.ViewOffsetFrames;

                    if (!track.Playing || track.Paused)
                    {
                        try
                        {
                            await track.PauseAsync();
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    track.SetPosition(targetFrame);
                    track.StartingOffset = targetFrame * ch;

                    int width = Math.Max(1, this.pictureBox_wave.Width);
                    int spp = this.SamplesPerPixel;
                    long viewFrames = (long) width * Math.Max(1, spp);
                    long totalFrames = Math.Max(1, track.Length / Math.Max(1, ch));
                    long maxOffset = Math.Max(0, totalFrames - viewFrames);
                    float caretPosClamped = Math.Clamp(this.CaretPosition, 0f, 1f);
                    long caretInViewFrames = (long) Math.Round(viewFrames * caretPosClamped);
                    long newOffset = targetFrame - caretInViewFrames;
                    this.ViewOffsetFrames = Math.Clamp(newOffset, 0, maxOffset);
                    this.ClampViewOffset();
                    this.RecalculateScrollBar();
                    track.ScrollOffset = this.ViewOffsetFrames;

                    try
                    {
                        // ✅ Aktualisiere Loop-Grenzen BEVOR Playback startet
                        if (this.loopState.LoopEnabled)
                        {
                            this.UpdateLoopBounds();
                        }

                        // ✅ Loop-Parameter übergeben
                        long loopStartSample = track.LoopStartFrames * track.Channels;
                        long loopEndSample = track.LoopEndFrames * track.Channels;
                        
                        await track.PlayAsync(CancellationToken.None, null, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample, desiredLatency: 90);
                    }
                    catch
                    {
                    }
                }
            }
            else if (this.SelectionMode.Equals("Erase", StringComparison.OrdinalIgnoreCase))
            {
                long startSample = track.SelectionStart >= 0
                    ? track.SelectionStart
                    : track.Playing
                        ? track.Position * Math.Max(1, track.Channels)
                        : this.GetFrameUnderCursor() * Math.Max(1, track.Channels);

                long endSample = track.Length;

                // ✅ Bestimme, zu welcher Collection das Sample gehört
                AudioCollection targetCollection;
                if (this.AudioC.Audios.Any(a => a.Id == track.Id))
                {
                    targetCollection = this.AudioC;
                }
                else if (this.AudioC_res.Audios.Any(a => a.Id == track.Id))
                {
                    targetCollection = this.AudioC_res;
                }
                else
                {
                    LogCollection.Log($"Track {track.Name} not found in any collection");
                    return;
                }

                // ✅ Snapshot in der richtigen Collection erstellen
                await targetCollection.PushSnapshotAsync(track.Id);
                this.UpdateUndoLabel();

                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    await track.CutOffBeforeAsync(startSample);
                }
                else
                {
                    await track.CutOffAfterAsync(startSample);
                }
            }

            e.Handled = true;
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
                if (track.Playing || track.Paused)
                {
                    await track.PauseAsync();
                    this.button_pause.ForeColor = track.Paused ? Color.DarkGray : Color.Black;
                }
                else
                {
                    // ✅ Aktualisiere Loop-Grenzen BEVOR Playback startet
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
                    
                    // ✅ Loop-Parameter übergeben
                    long loopStartSample = track.LoopStartFrames * track.Channels;
                    long loopEndSample = track.LoopEndFrames * track.Channels;
                    
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

        /// <summary>
        /// Gibt zurück, ob sich der Fokus in einem editierbaren Steuerelement befindet.
        /// </summary>
        private bool IsEditingContext()
        {
            var focused = this.ActiveControl;
            return focused is TextBoxBase || focused is ComboBox || focused is NumericUpDown;
        }

        /// <summary>
        /// Aktualisiert die Anzeige der aktuell ausgewählten Collection ListBox.
        /// </summary>
        private void UpdateSelectedCollectionListBox()
        {
            this.SelectedCollectionListBox?.Invalidate();
        }

        /// <summary>
        /// Aktualisiert die Anzeige des Undo-Labels.
        /// </summary>
        private void UpdateUndoLabel()
        {
            // Beispielimplementierung: Setzen Sie hier die Logik, wie das Undo-Label aktualisiert werden soll.
            // Zum Beispiel:
            if (this.label_undoSteps != null)
            {
                this.label_undoSteps.Text = $"Undo Steps: {this.StepsBack}";
            }
        }
    }
}
