using CSharpSamplesCutter.Core;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private void Register_PictureBox_Events(PictureBox pictureBox_waveform)
        {
            pictureBox_waveform.MouseMove += (s, e) =>
            {
                var track = this.SelectedTrack;
                if (track == null)
                {
                    return;
                }

                if (this.IsSelecting)
                {
                    long startFrame = this.CurrentScrollOffsetFrames;
                    int cursorX = Math.Max(0, Math.Min(this.pictureBox_wave.Width - 1, e.X));
                    long endFrame = startFrame + (long) cursorX * this.SamplesPerPixel;
                    track.SelectionEnd = endFrame * Math.Max(1, track.Channels);
                }

                pictureBox_waveform.Cursor = Cursors.IBeam;
            };

            pictureBox_waveform.MouseEnter += (s, e) => pictureBox_waveform.Focus();

            pictureBox_waveform.MouseWheel += (s, e) =>
            {
                var track = this.SelectedTrack;
                if (track == null)
                {
                    return;
                }

                int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;

                if (ModifierKeys.HasFlag(Keys.Control) && this.numericUpDown_samplesPerPixel.Enabled)
                {
                    int oldSPP = this.SamplesPerPixel;
                    int step = Math.Max(1, oldSPP / 10);
                    int newSPP = oldSPP + (notches < 0 ? step : -step);
                    newSPP = (int) Math.Clamp(newSPP, (double) this.numericUpDown_samplesPerPixel.Minimum, (double) this.numericUpDown_samplesPerPixel.Maximum);

                    int cursorX = Math.Max(0, Math.Min(this.pictureBox_wave.Width - 1, ((MouseEventArgs) e).X));
                    long anchorFrame = this.CursorOverPictureBox
                        ? this.ViewOffsetFrames + (long) cursorX * oldSPP
                        : this.ViewOffsetFrames + (long) (this.pictureBox_wave.Width / 2) * oldSPP;

                    this.numericUpDown_samplesPerPixel.Value = newSPP;

                    this.RecalculateScrollBar();

                    long anchorPixelX = this.CursorOverPictureBox ? cursorX : this.pictureBox_wave.Width / 2;
                    long newStartFrame = Math.Max(0, anchorFrame - anchorPixelX * newSPP);
                    this.ViewOffsetFrames = newStartFrame;

                    track.ScrollOffset = this.ViewOffsetFrames;

                    this.RecalculateScrollBar();
                    this.UpdateViewingElements();
                }
                else
                {
                    if (track.PlayerPlaying && this.checkBox_sync.Checked)
                    {
                        return;
                    }

                    int deltaCols = (int) (notches * this.hScrollBar_scroll.SmallChange);
                    long newColumn = this.hScrollBar_scroll.Value - deltaCols;
                    newColumn = Math.Clamp(newColumn, 0, (long) this.hScrollBar_scroll.Maximum);
                    this.hScrollBar_scroll.Value = (int) newColumn;

                    this.ViewOffsetFrames = this.hScrollBar_scroll.Value * (long) this.SamplesPerPixel;
                    track.ScrollOffset = this.ViewOffsetFrames;

                    _ = this.RedrawWaveformImmediateAsync();
                    this.UpdateViewingElements(skipScrollbarSync: true);
                }
            };

            pictureBox_waveform.MouseDown += (s, e) =>
            {
                if (this.SelectedTrack != null)
                {
                    this.UpdateViewingElements();
                }

                if (MouseButtons.HasFlag(MouseButtons.Right))
                {
                    this.IsSelecting = false;
                    if (this.SelectedTrack != null)
                    {
                        this.SelectedTrack.SelectionStart = -1;
                        this.SelectedTrack.SelectionEnd = -1;
                    }

                    return;
                }

                var track = this.SelectedTrack;
                if (track == null)
                {
                    return;
                }

                this.IsSelecting = true;

                if (this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase) || this.SelectionMode.Equals("Erase", StringComparison.OrdinalIgnoreCase))
                {
                    long startFrame = this.CurrentScrollOffsetFrames;
                    int cursorX = Math.Max(0, Math.Min(this.pictureBox_wave.Width - 1, e.X));
                    long selStartFrame = startFrame + (long) cursorX * this.SamplesPerPixel;
                    track.SelectionEnd = -1;
                    track.SelectionStart = selStartFrame * Math.Max(1, track.Channels);
                }
            };

            pictureBox_waveform.MouseLeave += (s, e) =>
            {
                pictureBox_waveform.Cursor = Cursors.Default;
            };

            pictureBox_waveform.MouseUp += async (s, e) =>
            {
                int selectedIndex = this.listBox_audios.SelectedIndex >= 0 ? this.listBox_audios.SelectedIndex : this.listBox_reserve.SelectedIndex;
                if (this.SelectedTrack == null)
                {
                    return;
                }

                if (this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
                {
                    long startFrame = this.CurrentScrollOffsetFrames;
                    int cursorX = Math.Max(0, Math.Min(this.pictureBox_wave.Width - 1, e.X));
                    long endFrame = startFrame + (long) cursorX * this.SamplesPerPixel;
                    int chSel = Math.Max(1, this.SelectedTrack.Channels);
                    long totalFramesSel = this.SelectedTrack.Length / chSel;
                    endFrame = Math.Clamp(endFrame, 0, Math.Max(0, totalFramesSel - 1));
                    this.SelectedTrack.SelectionEnd = endFrame * chSel;

                    if (Math.Abs(this.SelectedTrack.SelectionStart - this.SelectedTrack.SelectionEnd) < 5)
                    {
                        if (!this.SelectedTrack.PlayerPlaying)
                        {
                            long caretFrame = this.GetFrameUnderCursor();
                            this.SelectedTrack.SetPosition(caretFrame);
                            this.SelectedTrack.StartingOffset = caretFrame * chSel;
                        }
                        this.SelectedTrack.SelectionStart = -1;
                        this.SelectedTrack.SelectionEnd = -1;
                    }
                    else
                    {
                        // ✅ NEW: Bei Selection-Update Loop-Grenzen aktualisieren wenn Loop aktiv
                        if (this.loopState.LoopEnabled)
                        {
                            this.UpdateLoopBounds();

                            // Wenn Track läuft: LIVE Update
                            if (this.SelectedTrack.PlayerPlaying)
                            {
                                long loopStartSample = this.SelectedTrack.LoopStartFrames * this.SelectedTrack.Channels;
                                long loopEndSample = this.SelectedTrack.LoopEndFrames * this.SelectedTrack.Channels;
                                this.SelectedTrack.UpdateLoopBoundsDuringPlayback(loopStartSample, loopEndSample);
                            }
                        }
                    }
                }
                else if (this.SelectionMode.Equals("Erase", StringComparison.OrdinalIgnoreCase))
                {
                    await this.SelectedTrack.CreateSnapshotAsync();
                    this.UpdateUndoLabel();
                    await this.SelectedTrack.EraseSelectionAsync();
                    this.SelectedTrack.SelectionStart = -1;
                    this.SelectedTrack.SelectionEnd = -1;

                    this.StepsBack = 0;
                    this.ClampViewOffset();
                    this.RecalculateScrollBar();
                    this.SelectedTrack.ScrollOffset = this.ViewOffsetFrames;

                    // ✅ WICHTIG: Nach Erase Loop-Grenzen neu berechnen!
                    if (this.loopState.LoopEnabled)
                    {
                        this.UpdateLoopBounds();

                        // Wenn Track läuft: LIVE Update
                        if (this.SelectedTrack.PlayerPlaying)
                        {
                            long loopStartSample = this.SelectedTrack.LoopStartFrames * this.SelectedTrack.Channels;
                            long loopEndSample = this.SelectedTrack.LoopEndFrames * this.SelectedTrack.Channels;
                            this.SelectedTrack.UpdateLoopBoundsDuringPlayback(loopStartSample, loopEndSample);
                        }
                    }
                }

                this.IsSelecting = false;
            };
        }

		private async Task UpdateTimer_TickAsync()
		{
			try
			{
				var spp = this.SamplesPerPixel;
				var track = this.SelectedTrack;
				if (track == null)
				{
					this.pictureBox_wave.Image = null;
					return;
				}

				this.ClampViewOffset();

				if (this.pictureBox_wave.InvokeRequired || this.textBox_timestamp.InvokeRequired)
				{
					var tcs = new TaskCompletionSource<object?>();
					this.pictureBox_wave.BeginInvoke(new Action(async () =>
					{
						try { await this.UpdateTimer_TickAsync(); tcs.TrySetResult(null); }
						catch (Exception ex) { tcs.TrySetException(ex); }
					}));
					await tcs.Task;
					return;
				}

				bool follow = this.checkBox_sync.Checked && track.PlayerPlaying;

				if (follow)
				{
					int sppLocal = Math.Max(1, spp);
					long viewFrames = (long) this.pictureBox_wave.Width * sppLocal;

					int ch = Math.Max(1, track.Channels);
					long totalFrames = Math.Max(1, track.Length / ch);
					long viewEndLimit = (track.LoopEndFrames > track.LoopStartFrames)
						? track.LoopEndFrames
						: totalFrames;

					long maxOffset = Math.Max(0, viewEndLimit - viewFrames);

					float caretPosClamped = Math.Clamp(this.CaretPosition, 0f, 1f);
					long caretInViewFrames = (long) Math.Round(viewFrames * caretPosClamped);
					long caretFrame = track.Position;

					// Direkt ankern: gewünschter Offset so, dass Caret an definierter Position bleibt
					long wanted = Math.Clamp(caretFrame - caretInViewFrames, 0, maxOffset);

					if (wanted != this.ViewOffsetFrames)
					{
						this.ViewOffsetFrames = wanted;
						this.ClampViewOffset();

						this.SuppressScrollEvent = true;
						try { this.RecalculateScrollBar(); }
						finally { this.SuppressScrollEvent = false; }

						track.ScrollOffset = this.ViewOffsetFrames;
					}
				}
				else if (!this.IsUserScroll && track.ScrollOffset != this.ViewOffsetFrames)
				{
					this.ViewOffsetFrames = track.ScrollOffset;
					this.ClampViewOffset();

					this.SuppressScrollEvent = true;
					try { this.RecalculateScrollBar(); }
					finally { this.SuppressScrollEvent = false; }
				}

				if (this.HueEnabled)
				{
					this.GetNextHue(spp);
				}

				long? offsetFrames = this.viewOffsetFrames;

				Bitmap bmp = await track.DrawWaveformAsync(
					this.pictureBox_wave.Width,
					this.pictureBox_wave.Height,
					spp,
					this.DrawEachChannel,
					this.CaretWidth,
					offsetFrames,
					this.HueEnabled ? this.HueColor : this.WaveGraphColor,
					this.WaveBackColor,
					this.CaretColor,
					this.SelectionColor,
					this.SmoothenWaveform,
					this.TimingMarkerInterval,
					this.CaretPosition,
					2);

				this.textBox_timestamp.Text =
					((track.PlayerPlaying || track.Paused) ? track.CurrentTime : track.Duration).ToString("hh\\:mm\\:ss\\.fff");

				this.pictureBox_wave.Image = bmp;
			}
			catch (Exception ex)
			{
				LogCollection.Log($"Error in UpdateTimer_TickAsync: {ex.Message}");
			}
			finally
			{
				this.UpdateViewingElements(skipScrollbarSync: true);
			}
		}

		private void hScrollBar_caretPosition_Scroll(object sender, ScrollEventArgs e)
        {
            this.label_info_caretPosition.Text = $"Caret Position: {(this.CaretPosition * 100f):F1}%";
        }

        private void hScrollBar_scroll_Scroll(object sender, ScrollEventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            if (this.SuppressScrollEvent)
            {
                return;
            }

            if (track.PlayerPlaying && this.checkBox_sync.Checked)
            {
                this.SuppressScrollEvent = true;
                try
                {
                    int target = this.FramesToScrollValue(this.viewOffsetFrames);
                    int maxAllowed = Math.Max(this.hScrollBar_scroll.Minimum, this.hScrollBar_scroll.Maximum - this.hScrollBar_scroll.LargeChange + 1);
                    int clamped = Math.Clamp(target, this.hScrollBar_scroll.Minimum, maxAllowed);
                    e.NewValue = clamped;
                    if (this.hScrollBar_scroll.Value != clamped)
                    {
                        this.hScrollBar_scroll.Value = clamped;
                    }
                }
                finally { this.SuppressScrollEvent = false; }
                return;
            }

            try
            {
                this.IsUserScroll = true;
                this.viewOffsetFrames = (long) this.hScrollBar_scroll.Value * Math.Max(1, this.SamplesPerPixel);
                this.ClampViewOffset();
                track.ScrollOffset = this.viewOffsetFrames;
                _ = this.RedrawWaveformImmediateAsync();
            }
            finally
            {
                this.IsUserScroll = false;
            }

            this.UpdateViewingElements(skipScrollbarSync: true);
        }

        private int FramesToScrollValue(long frames) =>
            (int) Math.Clamp(frames / Math.Max(1, (long) this.SamplesPerPixel), this.hScrollBar_scroll.Minimum,
                            Math.Max(this.hScrollBar_scroll.Minimum, this.hScrollBar_scroll.Maximum - this.hScrollBar_scroll.LargeChange + 1));

        private void numericUpDown_samplesPerPixel_ValueChanged(object sender, EventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            track.LastSamplesPerPixel = this.SamplesPerPixel;

            this.ClampViewOffset();
            this.RecalculateScrollBar();
            track.ScrollOffset = this.viewOffsetFrames;

            _ = this.RedrawWaveformImmediateAsync();
            this.UpdateViewingElements();
        }

        private void UpdateViewingElements(bool skipScrollbarSync = false)
        {
            try
            {
                var track = this.SelectedTrack;
                if (track == null)
                {
                    this.button_playback.Text = "▶";
                    this.button_pause.Enabled = false;
                    this.button_move.Enabled = false;
                    this.label_sampleArea.Text = "No sample area available or selected.";
                    this.label_sampleAtCursor.Text = "Sample at Cursor: -";
                    return;
                }

                this.button_playback.Text = track.Playing ? "■" : "▶";
                this.button_pause.Enabled = track.Playing || track.Paused;
                this.button_pause.ForeColor = track.Paused ? Color.DarkGray : Color.Black;
                this.button_move.Enabled = true;

                this.hScrollBar_scroll.Enabled = true;
                this.hScrollBar_scroll.Cursor = track.PlayerPlaying ? Cursors.NoMoveHoriz : Cursors.Default;

                if (!skipScrollbarSync && !this.IsUserScroll)
                {
                    this.RecalculateScrollBar();
                }

                int ch = Math.Max(1, track.Channels);
                long frameIndex = this.GetFrameUnderCursor();
                double timeSeconds = frameIndex / (double) track.SampleRate;

                this.label_sampleArea.Text =
                    track.SelectionStart >= 0 && track.SelectionEnd >= 0
                        ? $"Sample Area: {Math.Min(track.SelectionStart, track.SelectionEnd)} - {Math.Max(track.SelectionStart, track.SelectionEnd)} (Duration: {TimeSpan.FromSeconds(Math.Abs(track.SelectionEnd - track.SelectionStart) / (double) (track.SampleRate * ch)).ToString("hh\\:mm\\:ss\\.fff")})"
                        : "No sample area available or selected.";

                var ts = TimeSpan.FromSeconds(timeSeconds);
                this.label_sampleAtCursor.Text =
                    $"Sample at {(this.CursorOverPictureBox ? "Cursor" : "Caret")}: {frameIndex} ({ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3} sec.)";
                this.label_sampleAtCursor.ForeColor = this.CursorOverPictureBox ? Color.Blue : Color.Black;
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private async Task RedrawWaveformImmediateAsync()
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            try
            {
                int spp = this.SamplesPerPixel;
                long totalFrames = Math.Max(1, track.Length / Math.Max(1, track.Channels));
                float liveCaretPosLocal = track.PlayerPlaying ? Math.Clamp((float)track.Position / (float)totalFrames, 0f, 1f) : this.CaretPosition;
                Bitmap bmp = await track.DrawWaveformAsync(
                    this.pictureBox_wave.Width,
                    this.pictureBox_wave.Height,
                    spp,
                    this.DrawEachChannel,
                    this.CaretWidth,
                    this.viewOffsetFrames,
                    this.HueEnabled ? this.HueColor : this.WaveGraphColor,
                    this.WaveBackColor,
                    this.CaretColor,
                    this.SelectionColor,
                    this.SmoothenWaveform,
                    this.TimingMarkerInterval,
                    liveCaretPosLocal,
                    2);

                if (!this.pictureBox_wave.IsDisposed)
                {
                    this.pictureBox_wave.Image = bmp;
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log($"Redraw error: {ex.Message}");
            }
        }

        private void ClampViewOffset()
        {
            var track = this.SelectedTrack;
            if (track == null || this.pictureBox_wave.Width <= 0)
            {
                return;
            }
            int ch = Math.Max(1, track.Channels);
            long totalFrames = track.Length / ch;
            long viewFrames = (long) this.pictureBox_wave.Width * this.SamplesPerPixel;
            long maxOffsetFrames = Math.Max(0, totalFrames - viewFrames);
            if (this.viewOffsetFrames > maxOffsetFrames)
            {
                this.viewOffsetFrames = maxOffsetFrames;
            }
            if (this.viewOffsetFrames < 0)
            {
                this.viewOffsetFrames = 0;
            }
        }

        private void RecalculateScrollBar()
        {
            var track = this.SelectedTrack;
            if (track == null || this.pictureBox_wave.Width <= 0)
            {
                return;
            }

            int spp = Math.Max(1, this.SamplesPerPixel);
            int ch = Math.Max(1, track.Channels);
            long totalFrames = Math.Max(0, track.Length / ch);
            long viewFrames = (long) this.pictureBox_wave.Width * spp;
            long maxOffsetFrames = Math.Max(0, totalFrames - viewFrames);

            int maxValue = (int) Math.Max(0, maxOffsetFrames / spp);
            int large = Math.Max(1, (int) (viewFrames / spp));
            int small = Math.Max(1, large / 10);

            int targetValue = (int) Math.Clamp(this.viewOffsetFrames / spp, 0, maxValue);
            int maxAllowed = Math.Max(0, (maxValue + large - 1) - large + 1);

            bool needApply = this.hScrollBar_scroll.LargeChange != large
                          || this.hScrollBar_scroll.SmallChange != small
                          || this.hScrollBar_scroll.Maximum != (maxValue + large - 1)
                          || this.hScrollBar_scroll.Value != Math.Min(targetValue, maxAllowed);

            if (!needApply)
            {
                return;
            }

            this.SuppressScrollEvent = true;
            try
            {
                this.hScrollBar_scroll.Minimum = 0;
                this.hScrollBar_scroll.LargeChange = large;
                this.hScrollBar_scroll.SmallChange = small;
                this.hScrollBar_scroll.Maximum = maxValue + large - 1;

                int newValue = Math.Min(targetValue, Math.Max(0, this.hScrollBar_scroll.Maximum - this.hScrollBar_scroll.LargeChange + 1));
                newValue = Math.Max(this.hScrollBar_scroll.Minimum, newValue);
                if (this.hScrollBar_scroll.Value != newValue)
                {
                    this.hScrollBar_scroll.Value = newValue;
                }
            }
            finally
            {
                this.SuppressScrollEvent = false;
            }
        }

        private bool GetCursorOverPictureBox()
        {
            bool over = false;

            try
            {
                Point cursorPosition = this.pictureBox_wave.PointToClient(Cursor.Position);
                over = cursorPosition.X >= 0 && cursorPosition.X < this.pictureBox_wave.Width && cursorPosition.Y >= 0 && cursorPosition.Y < this.pictureBox_wave.Height;
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
                over = false;
            }

            return over;
        }

        private long GetFrameUnderCursor()
        {
            if (this.SelectedTrack == null)
            {
                return 0L;
            }
            var track = this.SelectedTrack;
            int ch = Math.Max(1, track!.Channels);
            long totalFrames = track.Length / ch;
            this.ClampViewOffset();
            long frameIndex = 0L;
            try
            {
                if (this.CursorOverPictureBox)
                {
                    int cursorX = Math.Max(0, Math.Min(this.pictureBox_wave.Width - 1, this.pictureBox_wave.PointToClient(Cursor.Position).X));
                    long startFrame = this.CurrentScrollOffsetFrames;
                    frameIndex = startFrame + (long) cursorX * this.SamplesPerPixel;
                    frameIndex = Math.Clamp(frameIndex, 0, Math.Max(0, totalFrames - 1));
                }
                else
                {
                    frameIndex = track.Position;
                    frameIndex = Math.Clamp(frameIndex, 0, Math.Max(0, totalFrames - 1));
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
                return 0L;
            }
            return frameIndex;
        }

        private double GetTimeUnderCursor()
        {
            if (this.SelectedTrack == null)
            {
                return 0.0;
            }

            long frameIndex = this.GetFrameUnderCursor();
            return frameIndex > 0 ? frameIndex / (double) this.SelectedTrack.SampleRate : 0.0;
        }

        private void ListBox_audios_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            ListBox listBox = (ListBox) sender;

            if (e.Index < 0 || e.Index >= listBox.Items.Count)
            {
                e.DrawBackground();
                return;
            }

            e.DrawBackground();

            object item = listBox.Items[e.Index];
            string itemText = item.GetType().GetProperty(listBox.DisplayMember)?.GetValue(item)?.ToString() ?? "(Error)";

            Rectangle textRect = e.Bounds;

            textRect.X += 2;
            textRect.Width -= 4;

            TextFormatFlags flags = TextFormatFlags.VerticalCenter |
                            TextFormatFlags.Left |
                            TextFormatFlags.SingleLine |
                            TextFormatFlags.EndEllipsis;

            Color textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                        ? SystemColors.HighlightText
                        : SystemColors.WindowText;

            TextRenderer.DrawText(
                e.Graphics,
                itemText,
                e.Font,
                textRect,
                textColor,
                flags
            );

            e.DrawFocusRectangle();
        }

        private void Register_NumericUpDown_ToBePowOf2(NumericUpDown numeric)
        {
            numeric.Tag = numeric.Value;
            numeric.ValueChanged += (s, e) =>
            {
                int previousValue = numeric.Tag is decimal d ? (int) d : (int) numeric.Value;
                int newValue = (int) numeric.Value;

                if (newValue > previousValue)
                {
                    numeric.Value = Math.Clamp(previousValue * 2, numeric.Minimum, numeric.Maximum);
                }
                else if (previousValue > newValue)
                {
                    numeric.Value = Math.Clamp(previousValue / 2, numeric.Minimum, numeric.Maximum);
                }

                numeric.Tag = numeric.Value;
            };
        }
    }
}
