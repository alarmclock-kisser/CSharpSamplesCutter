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
        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            // Skip handling if user is editing in a control (except for allowed keys)
            if (this.IsEditingContext())
            {
                if (!this.IsAllowedWhileEditing(e))
                {
                    return;
                }
            }

            // Track-Auswahl per Pfeiltasten (hoch/runter) in aktiver ListBox
            if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) && !e.Handled)
            {
                ListBox? listBox = this.SelectedCollectionListBox;
                if (listBox != null && listBox.Items.Count > 0)
                {
                    int idx = listBox.SelectedIndex;
                    int newIdx = idx;
                    if (e.KeyCode == Keys.Up && idx > 0)
                        newIdx = idx - 1;
                    else if (e.KeyCode == Keys.Down && idx < listBox.Items.Count - 1)
                        newIdx = idx + 1;
                    if (newIdx != idx)
                    {
                        listBox.ClearSelected(); // Nur einen Track auswählen!
                        listBox.SelectedIndex = newIdx;
                        listBox.Tag = newIdx; // Merke aktuellen Index
                        this.UpdateViewingElements();
                        if (this.ActiveControl is Button)
                            this.ActiveControl = listBox;
                    }
                    e.Handled = true;
                    return;
                }
            }

            // Links: Reserve-Collection/ListBox auswählen
            if (e.KeyCode == Keys.Left && !e.Handled)
            {
                if (this.listBox_reserve.Items.Count > 0)
                {
                    // Merke aktuellen Index der audios-LB
                    this.listBox_audios.Tag = this.listBox_audios.SelectedIndex;
                    this.listBox_reserve.Focus();
                    int idx = (this.listBox_reserve.Tag is int tagIdx && tagIdx >= 0 && tagIdx < this.listBox_reserve.Items.Count) ? tagIdx : 0;
                    this.listBox_reserve.SelectedIndex = idx;
                    this.UpdateViewingElements();
                    e.Handled = true;
                }
                return;
            }
            // Rechts: Haupt-Collection/ListBox auswählen
            if (e.KeyCode == Keys.Right && !e.Handled)
            {
                if (this.listBox_audios.Items.Count > 0)
                {
                    // Merke aktuellen Index der reserve-LB
                    this.listBox_reserve.Tag = this.listBox_reserve.SelectedIndex;
                    this.listBox_audios.Focus();
                    int idx = (this.listBox_audios.Tag is int tagIdx && tagIdx >= 0 && tagIdx < this.listBox_audios.Items.Count) ? tagIdx : 0;
                    this.listBox_audios.SelectedIndex = idx;
                    this.UpdateViewingElements();
                    e.Handled = true;
                }
                return;
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

        // Clear one-shot guard when Space is released
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e?.KeyCode == Keys.Space)
            {
                this.playbackState.SpaceDownHandled = false;
            }
            base.OnKeyUp(e!);
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

            e.Handled = true;
            e.SuppressKeyPress = true;

            if (this.IsEditingContext())
            {
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            // Ctrl+Backspace: Stop all, set position & starting to 0 (global reset)
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                await this.AudioC.StopAllAsync();
                await this.AudioC_res.StopAllAsync();
                track.SetPosition(0);
                track.StartingOffset = 0; // in Samples (interleaved)
                this.button_playback.Invoke(() => { this.button_playback.Text = "▶"; });
                this.PlaybackCancellationTokens.Clear();
                this.UpdateViewingElements();
                this.ActiveControl = this.pictureBox_wave;
                return;
            }

            try
            {
                if (!this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
                {
                    if (this.SelectionMode.Equals("Erase", StringComparison.OrdinalIgnoreCase))
                    {
                        long startSample = track.SelectionStart >= 0
                            ? track.SelectionStart
                            : track.PlayerPlaying
                                ? track.Position * Math.Max(1, track.Channels)
                                : this.GetFrameUnderCursor() * Math.Max(1, track.Channels);

                        AudioCollection targetCollection;
                        if (this.AudioC.Audios.Any(a => a.Id == track.Id)) { targetCollection = this.AudioC; }
                        else if (this.AudioC_res.Audios.Any(a => a.Id == track.Id)) { targetCollection = this.AudioC_res; }
                        else { LogCollection.Log($"Track {track.Name} not found in any collection"); return; }

                        await targetCollection.PushSnapshotAsync(track.Id);
                        this.UpdateUndoLabel();

                        if (ModifierKeys.HasFlag(Keys.Shift)) { await track.CutOffBeforeAsync(startSample); }
                        else { await track.CutOffAfterAsync(startSample); }
                    }
                    return;
                }

                long startFrame = 0;
                if (this.loopState.LoopEnabled)
                {
                    // Loop-Grenzen stets auffrischen (z. B. nach Pause), vor Start
                    this.UpdateLoopBounds();
                    startFrame = track.LoopStartFrames;
                }
                else if (track.StartingOffset > 0)
                {
                    startFrame = track.StartingOffset / Math.Max(1, track.Channels);
                }

                bool needSoloStop = this.SoloPlayback && (!track.PlayerPlaying || track.Paused);
                if (needSoloStop)
                {
                    await this.AudioC.StopAllAsync(false);
                    await this.AudioC_res.StopAllAsync(false);
                }

                if (track.PlayerPlaying)
                {
                    track.FastSeekWhilePlaying(startFrame);

                    if (this.loopState.LoopEnabled)
                    {
                        long loopStartSample = track.LoopStartFrames * track.Channels;
                        long loopEndSample = track.LoopEndFrames * track.Channels;
                        track.EnableLoopNow(loopStartSample, loopEndSample);
                    }

                    track.StartingOffset = startFrame * Math.Max(1, track.Channels);
                }
                else if (track.Paused)
                {
                    // Pausiert + Loop aktiv: Start exakte Loop-Session und setze ForceFollow, damit die UI sofort folgt
                    track.SetPosition(startFrame);
                    track.StartingOffset = startFrame * Math.Max(1, track.Channels);

                    this.AddPlaybackForceFollow(track.Id);

                    this.button_playback.Invoke(() => { this.button_playback.Text = "■"; });

                    long loopStartSample = 0;
                    long loopEndSample = 0;
                    if (this.loopState.LoopEnabled)
                    {
                        loopStartSample = track.LoopStartFrames * track.Channels;
                        loopEndSample = track.LoopEndFrames * track.Channels;
                    }

                    await track.PlayAsync(CancellationToken.None, () =>
                    {
                        this.button_playback.Invoke(() => this.button_playback.Text = "▶");
                        this.RemovePlaybackForceFollow(track.Id);
                    }, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
                }
                else
                {
                    track.SetPosition(startFrame);
                    track.StartingOffset = startFrame * Math.Max(1, track.Channels);
                    this.AddPlaybackForceFollow(track.Id);
                    this.button_playback.Invoke(() => { this.button_playback.Text = "■"; });

                    long loopStartSample = 0;
                    long loopEndSample = 0;
                    if (this.loopState.LoopEnabled)
                    {
                        loopStartSample = track.LoopStartFrames * track.Channels;
                        loopEndSample = track.LoopEndFrames * track.Channels;
                    }

                    await track.PlayAsync(CancellationToken.None, () =>
                    {
                        this.button_playback.Invoke(() => this.button_playback.Text = "▶");
                        this.RemovePlaybackForceFollow(track.Id);
                    }, this.Volume, loopStartSample: loopStartSample, loopEndSample: loopEndSample);
                }

                // Während Playback: Loop-Bounds live updaten
                if (this.loopState.LoopEnabled && track.PlayerPlaying)
                {
                    this.UpdateLoopBounds();
                    long loopStartSample2 = track.LoopStartFrames * track.Channels;
                    long loopEndSample2 = track.LoopEndFrames * track.Channels;
                    track.UpdateLoopBoundsDuringPlayback(loopStartSample2, loopEndSample2);
                }

                _ = this.RedrawWaveformImmediateAsync();
                this.UpdateViewingElements();
                this.ActiveControl = this.pictureBox_wave;
            }
            catch { }
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
