using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private async void ListBox_Audios_SelectedValueChanged(object? sender, EventArgs e, ListBox? contraryListBox = null)
        {
            if (sender is not ListBox listBox)
            {
                return;
            }

            // Persist view state (zoom + scroll) of previously selected track before switching
            var prevTrack = this.LastSelectedTrack;
            if (prevTrack != null)
            {
                try
                {
                    prevTrack.LastSamplesPerPixel = this.SamplesPerPixel;
                    prevTrack.ScrollOffset = this.ViewOffsetFrames;
                }
                catch { }
            }

            if (listBox.SelectedIndex >= 0)
            {
                if (contraryListBox != null)
                {
                    contraryListBox.SelectedIndex = -1;

                    bool arrowPointingLeft = listBox.Location.X > contraryListBox.Location.X;
                    this.button_move.Text = arrowPointingLeft ? "← Move" : "Move →";
                }
            }

            if (this.SoloPlayback)
            {
                await this.AudioC.StopAllAsync();
                await this.AudioC_res.StopAllAsync();
            }

            var track = this.SelectedTrack;

            if (this.LastSelectedTrack != null)
            {
                try { this.LastSelectedTrack.PreviousSteps.ListChanged -= this.PreviousSteps_ListChanged; } catch { }
            }

            if (track == null)
            {
                this.button_playback.Enabled = false;
                this.button_autoCut.Enabled = false;
                this.button_playback.Enabled = false;
                this.button_export.Enabled = false;
                this.button_reload.Enabled = false;
                this.button_remove.Enabled = false;
                this.textBox_audioInfo.Text = string.Empty;
                this.textBox_trackMetrics.Text = string.Empty;
                this.label_audioName.Text = "No audio selected.";
                this.numericUpDown_samplesPerPixel.Value = 0;
                this.hScrollBar_scroll.Value = 0;
                this.hScrollBar_scroll.Maximum = 0;
                this.hScrollBar_scroll.Enabled = false;
                this.label_undoSteps.Text = "Undo's: 0";

                this.pictureBox_wave.Image = null;
                this.viewOffsetFrames = 0;
                this.LastSelectedTrack = null;
                return;
            }

            try { track.PreviousSteps.ListChanged += this.PreviousSteps_ListChanged; } catch { }
            this.LastSelectedTrack = track;

            // --- Konsistente Auswahl: Immer alles updaten, auch bei erneutem Klick ---
            this.button_playback.Enabled = true;
            this.button_autoCut.Enabled = true;
            this.button_playback.Enabled = true;
            this.button_export.Enabled = true;
            this.button_reload.Enabled = true;
            this.button_remove.Enabled = true;
            this.label_undoSteps.Text = $"Undo's: {track.PreviousSteps.Count}";

            // Restore per-track scroll + zoom
            this.viewOffsetFrames = track.ScrollOffset;
            this.textBox_audioInfo.Text = track.GetInfoString();
            this.textBox_trackMetrics.Text = track.GetMetricsString(); // Jetzt auch für Reserve-Liste korrekt
            this.label_audioName.Text = track.Name;

            // Calculate fit-to-length SPP if track has never been zoomed
            if (track.LastSamplesPerPixel <= 0)
            {
                int pictureBoxWidth = Math.Max(1, this.pictureBox_wave.Width);
                if (track.Data != null && track.Data.Length > 0 && track.Channels > 0)
                {
                    int totalSamples = track.Data.Length / track.Channels;
                    int fitSpp = (int) Math.Ceiling((double) totalSamples / pictureBoxWidth);
                    this.numericUpDown_samplesPerPixel.Value = Math.Max(1, fitSpp);
                }
                else
                {
                    this.numericUpDown_samplesPerPixel.Value = this.samplesPerPixelToFit;
                }
            }
            else
            {
                this.numericUpDown_samplesPerPixel.Value = track.LastSamplesPerPixel;
            }

            // Wenn Loop aktiv ist, für den neuen Track sofort die Loop-Grenzen anhand der aktuellen Auswahl setzen
            if (this.loopState.LoopEnabled)
            {
                this.UpdateLoopBounds();
            }

            this.hScrollBar_scroll.Enabled = true;

            this.ClampViewOffset();
            this.RecalculateScrollBar();
            this.UpdateViewingElements();
        }

        private void ListBox_HandleSelection(ListBox listBox, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            int index = listBox.IndexFromPoint(e.Location);
            if (index < 0)
            {
                return;
            }

            bool ctrl = ModifierKeys.HasFlag(Keys.Control);
            bool shift = ModifierKeys.HasFlag(Keys.Shift);

            // CS8156-Fix: Kein ref-Ternary möglich, daher explizit zuweisen
            int? anchor;
            if (listBox == this.listBox_audios)
            {
                anchor = this.AnchorIndexMain;
            }
            else
            {
                anchor = this.AnchorIndexReserve;
            }

            if (ctrl)
            {
                bool currentlySelected = listBox.GetSelected(index);
                listBox.SetSelected(index, !currentlySelected);
                if (!currentlySelected)
                {
                    if (listBox == this.listBox_audios)
                    {
                        this.AnchorIndexMain = index;
                    }
                    else
                    {
                        this.AnchorIndexReserve = index;
                    }
                }
                return;
            }

            if (shift && anchor.HasValue && anchor.Value >= 0 && anchor.Value < listBox.Items.Count)
            {
                int start = Math.Min(anchor.Value, index);
                int end = Math.Max(anchor.Value, index);

                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    listBox.SetSelected(i, false);
                }
                for (int i = start; i <= end; i++)
                {
                    listBox.SetSelected(i, true);
                }
                return;
            }

            for (int i = 0; i < listBox.Items.Count; i++)
            {
                if (i != index && listBox.GetSelected(i))
                {
                    listBox.SetSelected(i, false);
                }
            }
            listBox.SetSelected(index, true);
            if (listBox == this.listBox_audios)
            {
                this.AnchorIndexMain = index;
            }
            else
            {
                this.AnchorIndexReserve = index;
            }
        }

        private async void ListBox_Audios_RightClickMenu(object? sender, MouseEventArgs e)
        {
            if (sender is not ListBox listBox)
            {
                return;
            }

            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            int index = listBox.IndexFromPoint(e.Location);
            if (index < 0 || index >= listBox.Items.Count)
            {
                return;
            }

            bool ctrlPressed = ModifierKeys.HasFlag(Keys.Control);
            if (!ctrlPressed)
            {
                if (!listBox.GetSelected(index))
                {
                    listBox.ClearSelected();
                    listBox.SetSelected(index, true);
                }
            }
            else
            {
                bool wasSelected = listBox.GetSelected(index);
                listBox.SetSelected(index, !wasSelected);
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            var contextMenu = new ContextMenuStrip();
            try
            {
                var menuItemRename = new ToolStripMenuItem("Rename");
                menuItemRename.Click += (s, ev) =>
                {
                    string? input = Microsoft.VisualBasic.Interaction.InputBox("Enter new name for the audio sample:", "Rename Audio Sample", track.Name);
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        string oldName = track.Name;
                        track.Name = input.Trim();

                        listBox.Refresh();

                        LogCollection.Log($"Renamed audio sample: \"{oldName}\" to \"{track.Name}\"");
                    }
                };

                contextMenu.Items.Add(menuItemRename);

                contextMenu.Show(listBox, e.Location);

                contextMenu.Closed += (s, ev) =>
                {
                    this.BeginInvoke(new Action(() => contextMenu.Dispose()));
                };
            }
            catch
            {
                contextMenu.Dispose();
                throw;
            }
            finally
            {
                await Task.CompletedTask;
            }
        }

        private void ReselectAfterRemoval(ListBox listBox, IEnumerable<int> removedIndices)
        {
            if (listBox.Items.Count == 0)
            {
                listBox.SelectedIndex = -1;
                return;
            }

            int minRemoved = removedIndices.Min();
            int newIndex = Math.Min(minRemoved, listBox.Items.Count - 1);
            if (newIndex >= 0 && newIndex < listBox.Items.Count)
            {
                listBox.SelectedIndex = newIndex;
            }
            else
            {
                listBox.SelectedIndex = listBox.Items.Count - 1;
            }

            if (listBox == this.listBox_audios)
            {
                this.AnchorIndexMain = listBox.SelectedIndex >= 0 ? listBox.SelectedIndex : null;
            }
            else if (listBox == this.listBox_reserve)
            {
                this.AnchorIndexReserve = listBox.SelectedIndex >= 0 ? listBox.SelectedIndex : null;
            }
        }

        private void SelectShiftedSuccessor(ListBox listBox, List<int> removedIndices)
        {
            if (removedIndices.Count == 0)
            {
                return;
            }

            var beforeItems = listBox.Items.Cast<AudioObj>().ToList();
            int firstRemoved = removedIndices.Min();

            AudioObj? candidate = null;
            for (int i = firstRemoved; i < beforeItems.Count; i++)
            {
                if (!removedIndices.Contains(i))
                {
                    candidate = beforeItems[i];
                    break;
                }
            }

            if (candidate == null && firstRemoved > 0)
            {
                for (int i = firstRemoved - 1; i >= 0; i--)
                {
                    if (!removedIndices.Contains(i))
                    {
                        candidate = beforeItems[i];
                        break;
                    }
                }
            }

            var afterItems = listBox.Items.Cast<AudioObj>().ToList();
            if (afterItems.Count == 0)
            {
                listBox.SelectedIndex = -1;
                return;
            }

            int newIndex;
            if (candidate != null)
            {
                newIndex = afterItems.IndexOf(candidate);
                if (newIndex < 0)
                {
                    newIndex = Math.Min(firstRemoved, afterItems.Count - 1);
                }
            }
            else
            {
                newIndex = Math.Min(firstRemoved, afterItems.Count - 1);
            }

            listBox.SelectedIndex = newIndex;

            if (listBox == this.listBox_audios)
            {
                this.AnchorIndexMain = newIndex >= 0 ? newIndex : null;
            }
            else if (listBox == this.listBox_reserve)
            {
                this.AnchorIndexReserve = newIndex >= 0 ? newIndex : null;
            }
        }

        private static bool TryInvalidateUpdate(ListBox lb)
        {
            try
            {
                lb.Invalidate();
                lb.Update();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryReselectInvalidateUpdate(ListBox lb)
        {
            try
            {
                if (lb.IsDisposed)
                {
                    return false;
                }

                switch (lb.SelectionMode)
                {
                    case System.Windows.Forms.SelectionMode.None:
                        break;

                    case System.Windows.Forms.SelectionMode.One:
                        {
                            int idx = lb.SelectedIndex;
                            if (idx >= 0)
                            {
                                lb.ClearSelected();
                                if (idx >= lb.Items.Count)
                                {
                                    idx = lb.Items.Count - 1;
                                }

                                if (idx >= 0)
                                {
                                    lb.SelectedIndex = idx;
                                }
                            }
                            break;
                        }

                    default:
                        {
                            var selected = lb.SelectedIndices.Cast<int>().ToArray();
                            lb.ClearSelected();
                            foreach (var i in selected)
                            {
                                if (i >= 0 && i < lb.Items.Count)
                                {
                                    lb.SetSelected(i, true);
                                }
                            }
                            break;
                        }
                }

                lb.Invalidate();
                lb.Update();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
