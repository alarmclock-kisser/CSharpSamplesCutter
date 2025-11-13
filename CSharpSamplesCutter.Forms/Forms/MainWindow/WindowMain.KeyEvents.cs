using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        // NEU: Debounce / Reentrancy für Backspace
        private bool BackspaceKeyDebounceActive = false;
        private DateTime LastBackspaceToggleUtc = DateTime.MinValue;

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

            // ✅ NACH Erase: Loop-Grenzen neu berechnen!
            if (this.loopState.LoopEnabled && track.PlayerPlaying)
            {
                this.UpdateLoopBounds();
                long loopStartSample = track.LoopStartFrames * track.Channels;
                long loopEndSample = track.LoopEndFrames * track.Channels;
                track.UpdateLoopBoundsDuringPlayback(loopStartSample, loopEndSample);
            }

            e.Handled = true;
        }

        private async void Form_Back_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Back || e.Handled)
            {
                return;
            }

            // Sofort markieren (verhindert Auto-Repeat Doppel-Handling)
            e.Handled = true;

            if (this.IsEditingContext())
            {
                return;
            }

            // Ctrl+Backspace: Alles stoppen
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                await this.AudioC.StopAllAsync();
                await this.AudioC_res.StopAllAsync();
                this.button_playback.Invoke(() => { this.button_playback.Text = "▶"; });
                this.PlaybackCancellationTokens.Clear();
                return;
            }

            // Debounce / Reentrancy
            var now = DateTime.UtcNow;
            if (this.BackspaceKeyDebounceActive || (now - this.LastBackspaceToggleUtc).TotalMilliseconds < 140)
            {
                return;
            }
            this.BackspaceKeyDebounceActive = true;
            this.LastBackspaceToggleUtc = now;

            var track = this.SelectedTrack;
            if (track == null)
            {
                this.BackspaceKeyDebounceActive = false;
                return;
            }

            try
            {
                if (this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
                {
                    bool wasPlaying = track.PlayerPlaying;
                    bool wasPaused = track.Paused;
                    int ch = Math.Max(1, track.Channels);

                    // Loop oder Track-Beginn als hartes Reset
                    long targetFrame;
                    if (this.loopState.LoopEnabled)
                    {
                        var (loopStart, _) = this.GetLoopRange();
                        targetFrame = loopStart;
                    }
                    else
                    {
                        targetFrame = 0; // immer zum Anfang statt StartingOffset
                    }

                    // Nur pausieren falls nötig
                    if (wasPlaying || wasPaused)
                    {
                        try { await track.PauseAsync(); } catch { }
                    }

                    // Position setzen
                    track.SetPosition(targetFrame);
                    track.StartingOffset = targetFrame * ch;

                    // Scroll neu ausrichten (Caret bleibt an definierter relativer Position)
                    int width = Math.Max(1, this.pictureBox_wave.Width);
                    int spp = Math.Max(1, this.SamplesPerPixel);
                    long viewFrames = (long) width * spp;
                    long totalFrames = Math.Max(1, track.Length / ch);
                    long maxOffset = Math.Max(0, totalFrames - viewFrames);
                    float caretPosClamped = Math.Clamp(this.CaretPosition, 0f, 1f);
                    long caretInViewFrames = (long) Math.Round(viewFrames * caretPosClamped);
                    long newOffset = targetFrame - caretInViewFrames;
                    this.ViewOffsetFrames = Math.Clamp(newOffset, 0, maxOffset);
                    this.ClampViewOffset();

                    this.SuppressScrollEvent = true;
                    try { this.RecalculateScrollBar(); }
                    finally { this.SuppressScrollEvent = false; }

                    track.ScrollOffset = this.ViewOffsetFrames;
                    _ = this.RedrawWaveformImmediateAsync();

                    // Wiedergabe reaktivieren falls vorher spielte
                    if (wasPlaying || (!wasPlaying && !wasPaused))
                    {
                        try
                        {
                            if (this.loopState.LoopEnabled)
                            {
                                this.UpdateLoopBounds();
                            }
                            long loopStartSample = track.LoopStartFrames * track.Channels;
                            long loopEndSample = track.LoopEndFrames * track.Channels;
                            await track.PlayAsync(CancellationToken.None, null, this.Volume,
                                loopStartSample: loopStartSample,
                                loopEndSample: loopEndSample,
                                desiredLatency: 90);
                        }
                        catch { }
                    }
                }
                else if (this.SelectionMode.Equals("Erase", StringComparison.OrdinalIgnoreCase))
                {
                    long startSample = track.SelectionStart >= 0
                        ? track.SelectionStart
                        : track.PlayerPlaying
                            ? track.Position * Math.Max(1, track.Channels)
                            : this.GetFrameUnderCursor() * Math.Max(1, track.Channels);

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
            }
            finally
            {
                // Debounce Reset
                _ = Task.Run(async () =>
                {
                    await Task.Delay(130);
                    this.BackspaceKeyDebounceActive = false;
                });
            }
        }

        private bool IsEditingContext()
        {
            var focused = this.ActiveControl;
            return focused is TextBoxBase || focused is ComboBox || focused is NumericUpDown;
        }

        private void UpdateSelectedCollectionListBox()
        {
            this.SelectedCollectionListBox?.Invalidate();
        }

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
