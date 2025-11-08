using CSharpSamplesCutter.Core;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace CSharpSamplesCutter.Forms
{
	public partial class WindowMain : Form
	{
		public readonly AudioCollection AudioC = new(null, null, 24);

		public AudioObj? SelectedTrack => this.AudioC[this.listBox_audios.SelectedValue is Guid id ? id : Guid.Empty];
		private AudioObj? LastSelectedTrack = null;
		private List<Guid> SelectedGuids => this.listBox_audios.SelectedItems.Cast<AudioObj>().Select(a => a.Id).ToList();

		private readonly Timer UpdateTimer;
		private readonly ConcurrentDictionary<Guid, CancellationToken> PlaybackCancellationTokens = [];
		private bool IsSelecting = false;
		private bool CursorOverPictureBox => this.GetCursorOverPictureBox();

		public readonly double FrameRate;
		public int SkipTracks => (int) this.numericUpDown_skipTracks.Value;
		public int StepsBack { get; private set; } = 0;
		public string SelectionMode { get; set; } = "Select";
		public bool UsingSamplesPerPixelToFit => this.numericUpDown_samplesPerPixel.Value == 0 || this.numericUpDown_samplesPerPixel.Enabled == false;
		private int samplesPerPixelToFit => (this.pictureBox_wave.Width > 0 && this.SelectedTrack != null && this.SelectedTrack.Length > 0) ? Math.Max(1, (int) Math.Ceiling((this.SelectedTrack.Length) / this.SelectedTrack.Channels / (this.pictureBox_wave.Width * 0.9))) : 256;
		public int SamplesPerPixel => this.UsingSamplesPerPixelToFit ? this.samplesPerPixelToFit : (int) this.numericUpDown_samplesPerPixel.Value;
		public int SamplesPerPixelSelected => (int) this.numericUpDown_samplesPerPixel.Value;
		public int CaretWidth { get; set; } = 2;
		public float Volume => (this.vScrollBar_volume.Maximum - this.vScrollBar_volume.Value) / (float) this.vScrollBar_volume.Maximum;
		public long CurrentScrollPosition => this.SelectedTrack != null ? this.SelectedTrack.Playing ? this.SelectedTrack.Position : (long) this.hScrollBar_scroll.Value * this.SamplesPerPixel * this.SelectedTrack.Channels : 0L;

		// Persistenter View-Offset (Frames, nicht interleavte Samples) unabhängig von Playback-Zustand.
		private long viewOffsetFrames = 0;
		public long CurrentScrollOffsetFrames => this.viewOffsetFrames;

		public WindowMain()
		{
			this.InitializeComponent();
			this.KeyPreview = true; // Form erhält Key-Events zuerst (für Ctrl+Z global)

			this.StartPosition = FormStartPosition.Manual;
			this.Location = WindowsScreenHelper.GetCenterStartingPoint(this);

			this.FrameRate = WindowsScreenHelper.GetScreenRefreshRate();

			this.Load += this.WindowMain_Load;

			this.UpdateTimer = new Timer()
			{
				Interval = (int) (1000f / this.FrameRate)
			};

			this.UpdateTimer.Tick += async (s, e) => await this.UpdateTimer_TickAsync();
			this.UpdateTimer.Start();
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

		// Load Event
		private void WindowMain_Load(object? sender, EventArgs e)
		{
			this.listBox_audios.Items.Clear();
			this.listBox_audios.ValueMember = "Id";
			this.listBox_audios.DisplayMember = "Name";
			this.RebindAudioListForSkip();

			this.button_autoCut.Enabled = false;
			this.button_playback.Enabled = false;
			this.button_export.Enabled = false;
			this.button_reload.Enabled = false;
			this.button_remove.Enabled = false;
			this.hScrollBar_scroll.Enabled = false;

			// Synchronize SkipTracks maximum & rebind on list changes
			this.AudioC.Audios.ListChanged += (s, ev) =>
			{
				this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;
				if (this.numericUpDown_skipTracks.Value > this.numericUpDown_skipTracks.Maximum)
				{
					this.numericUpDown_skipTracks.Value = this.numericUpDown_skipTracks.Maximum;
				}
				this.RebindAudioListForSkip();
			};
			this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;

			// Event(s) for listBox_audio to switch to multi selection mode when Ctrl is held, jump back to single selection when released (select last prev selected track only)
			this.listBox_audios.MouseDown += (s, ev) =>
			{
				if (ModifierKeys.HasFlag(Keys.Control))
				{
					var selected = this.listBox_audios.SelectedIndex;
					this.listBox_audios.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
					
					if (selected >= 0)
					{
						this.listBox_audios.SelectedIndices.Add(selected);
					}
				}
			};
			this.listBox_audios.MouseUp += (s, ev) =>
			{
				if (!ModifierKeys.HasFlag(Keys.Control))
				{
					// Preserve last selected
					var lastSelected = this.SelectedTrack;
					this.listBox_audios.SelectionMode = System.Windows.Forms.SelectionMode.One;
					if (lastSelected != null)
					{
						this.listBox_audios.SelectedValue = lastSelected.Id;
					}
				}
			};

			this.listBox_audios.SelectedIndexChanged += (s, ev) =>
			{
				var track = this.SelectedTrack;

				// Vorherige Subscription lösen
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

				// Neue Subscription setzen
				try { track.PreviousSteps.ListChanged += this.PreviousSteps_ListChanged; } catch { }
				this.LastSelectedTrack = track;

				this.button_playback.Enabled = true;
				this.button_autoCut.Enabled = true;
				this.button_playback.Enabled = true;
				this.button_export.Enabled = true;
				this.button_reload.Enabled = true;
				this.button_remove.Enabled = true;
				this.label_undoSteps.Text = $"Undo's: {track.PreviousSteps.Count}";

				// RESTORE: auf zuletzt genutzte Scrollposition des Tracks springen
				this.viewOffsetFrames = track.ScrollOffset;
				this.textBox_audioInfo.Text = track.GetInfoString();
				this.label_audioName.Text = track.Name;
				this.numericUpDown_samplesPerPixel.Value = track.LastSamplesPerPixel > 0 ? track.LastSamplesPerPixel : this.samplesPerPixelToFit;
				this.hScrollBar_scroll.Enabled = true;

				// Wichtig: clampen und Scrollbar setzen
				this.ClampViewOffset();
				this.RecalculateScrollBar();
				this.UpdateViewingElements();
			};

			this.listBox_audios.SelectedIndex = -1;

			this.listBox_log.Items.Clear();
			this.listBox_log.DataSource = LogCollection.Logs;
			LogCollection.Logs.ListChanged += (s, ev) =>
			{
				this.listBox_log.TopIndex = this.listBox_log.Items.Count - 1;
			};
			this.listBox_log.DoubleClick += (s, ev) =>
			{
				if (this.listBox_log.SelectedItem != null)
				{
					Clipboard.SetText(this.listBox_log.SelectedItem.ToString() ?? string.Empty);
					LogCollection.Log("Log entry copied to clipboard.");
				}
			};

			this.numericUpDown_samplesPerPixel.MouseUp += (s, ev) =>
			{
				if (ModifierKeys.HasFlag(Keys.Control))
				{
					this.numericUpDown_samplesPerPixel.Enabled = false;
				}
			};
			this.panel_enableSamplesPerPixel.MouseUp += (s, ev) =>
			{
				// If right clicked disable
				if (ModifierKeys.HasFlag(Keys.Control))
				{
					this.numericUpDown_samplesPerPixel.Enabled = true;
				}
			};

			// Rebind list whenever skip changes
			this.numericUpDown_skipTracks.ValueChanged += this.numericUpDown_skipTracks_ValueChanged;

			this.KeyDown += this.Form_CtrlZ_Pressed;
			this.KeyDown += this.Form_CtrlY_Pressed; // Redo
			this.Register_PictureBox_Events(this.pictureBox_wave);
		}

		private void PreviousSteps_ListChanged(object? sender, ListChangedEventArgs e)
		{
			if (this.InvokeRequired)
			{
				try { this.BeginInvoke(new Action(() => this.UpdateUndoLabel())); } catch { }
			}
			else
			{
				this.UpdateUndoLabel();
			}
		}

		private void UpdateUndoLabel()
		{
			var track = this.SelectedTrack;
			this.label_undoSteps.Text = track != null ? $"Undo's: {track.PreviousSteps.Count}" : "Undo's: 0";
		}

		private void RebindAudioListForSkip()
		{
			// Preserve current selection
			Guid selectedId = this.listBox_audios.SelectedValue is Guid id ? id : Guid.Empty;
			int skip = Math.Clamp(this.SkipTracks, 0, this.AudioC.Audios.Count);
			var slice = this.AudioC.Audios.Skip(skip).ToList();
			this.listBox_audios.DataSource = null;
			this.listBox_audios.DataSource = slice;
			this.listBox_audios.DisplayMember = "Name";
			this.listBox_audios.ValueMember = "Id";
			if (selectedId != Guid.Empty && slice.Any(a => a.Id == selectedId))
			{
				this.listBox_audios.SelectedValue = selectedId;
			}
			else
			{
				this.listBox_audios.SelectedIndex = slice.Count > 0 ? 0 : -1;
			}
		}

		private void RecalculateScrollBar()
		{
			var track = this.SelectedTrack;
			if (track == null)
			{
				return;
			}
			int ch = Math.Max(1, track.Channels);
			long totalFrames = track.Length / ch;
			long viewFrames = (long) this.pictureBox_wave.Width * this.SamplesPerPixel;
			long maxOffsetFrames = Math.Max(0, totalFrames - viewFrames);
			this.ClampViewOffset(); // neu: Offset vor Berechnung begrenzen
			int maxColumns = (int) Math.Max(0, (maxOffsetFrames + this.SamplesPerPixel - 1) / this.SamplesPerPixel);
			this.hScrollBar_scroll.Minimum = 0;

			// Proportionale Scroll-Schritte relativ zur sichtbaren Breite
			int viewColumns = Math.Max(1, this.pictureBox_wave.Width);
			this.hScrollBar_scroll.SmallChange = Math.Max(1, viewColumns / 20); // ~5% der Breite
			this.hScrollBar_scroll.LargeChange = Math.Max(1, viewColumns / 2);  // ~50% Seite

			this.hScrollBar_scroll.Maximum = Math.Max(0, maxColumns + this.hScrollBar_scroll.LargeChange - 1);

			// Aktuellen Offset in Frames in Spaltenwert überführen und clampen
			long desiredColumn = this.viewOffsetFrames / Math.Max(1, this.SamplesPerPixel);
			int clamped = (int) Math.Clamp(desiredColumn, 0, Math.Max(0, this.hScrollBar_scroll.Maximum - this.hScrollBar_scroll.LargeChange + 1));
			this.hScrollBar_scroll.Value = clamped;
		}

		private async void Form_CtrlZ_Pressed(object? sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.Z)
			{
				var track = this.SelectedTrack;
				if (track == null)
				{
					return;
				}

				bool ok = await this.AudioC.UndoAsync(track.Id);
				if (!ok)
				{
					LogCollection.Log($"No undo steps available for track: {track.Name}");
					return;
				}

				this.StepsBack = 0; // interne Zählung nicht mehr benötigt
				this.listBox_audios.Refresh();
				this.UpdateViewingElements();
				this.UpdateUndoLabel();
				LogCollection.Log($"Undo applied on track: {track.Name}");
			}
		}

		private async void Form_CtrlY_Pressed(object? sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.Y)
			{
				var track = this.SelectedTrack;
				if (track == null)
				{
					return;
				}

				bool ok = await this.AudioC.RedoAsync(track.Id);
				if (!ok)
				{
					LogCollection.Log($"No redo step available for track: {track.Name}");
					return;
				}

				this.StepsBack = 0;
				this.listBox_audios.Refresh();
				this.UpdateViewingElements();
				this.UpdateUndoLabel();
				LogCollection.Log($"Redo applied on track: {track.Name}");
			}
		}

		// Further Events
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
					// SelectionEnd in interleavten Samples basierend auf View-Offset + CursorX
					long startFrame = this.CurrentScrollOffsetFrames;
					int cursorX = Math.Max(0, Math.Min(this.pictureBox_wave.Width - 1, e.X));
					long endFrame = startFrame + (long) cursorX * this.SamplesPerPixel;
					track.SelectionEnd = endFrame * Math.Max(1, track.Channels);
				}

				pictureBox_waveform.Cursor = Cursors.IBeam;
			};

			// Fokus setzen, damit MouseWheel-Event sicher ankommt
			pictureBox_waveform.MouseEnter += (s, e) => pictureBox_waveform.Focus();

			// Scrollen/Zoomen mit Mausrad (jetzt auch während Playback erlaubt)
			pictureBox_waveform.MouseWheel += (s, e) =>
			{
				var track = this.SelectedTrack;
				if (track == null)
				{
					return;
				}

				int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;

				// Ctrl + Wheel => Zoomen
				if (ModifierKeys.HasFlag(Keys.Control) && this.numericUpDown_samplesPerPixel.Enabled)
				{
					int oldSPP = this.SamplesPerPixel;
					int step = Math.Max(1, oldSPP / 10);
					int newSPP = oldSPP + (notches < 0 ? step : -step);
					newSPP = (int) Math.Clamp(newSPP, (double) this.numericUpDown_samplesPerPixel.Minimum, (double) this.numericUpDown_samplesPerPixel.Maximum);

					int cursorX = Math.Max(0, Math.Min(this.pictureBox_wave.Width - 1, ((MouseEventArgs) e).X));
					long anchorFrame = this.CursorOverPictureBox
						? this.viewOffsetFrames + (long) cursorX * oldSPP
						: this.viewOffsetFrames + (long) (this.pictureBox_wave.Width / 2) * oldSPP;

					this.numericUpDown_samplesPerPixel.Value = newSPP;

					this.RecalculateScrollBar();

					long anchorPixelX = this.CursorOverPictureBox ? cursorX : this.pictureBox_wave.Width / 2;
					long newStartFrame = Math.Max(0, anchorFrame - anchorPixelX * newSPP);
					this.viewOffsetFrames = newStartFrame;

					// SAVE: geänderten Offset im Track speichern
					track.ScrollOffset = this.viewOffsetFrames;

					this.RecalculateScrollBar();
					this.UpdateViewingElements();
				}
				else
				{
					// Horizontal scroll – Schritt in Spalteneinheiten
					int deltaCols = (int) (notches * this.hScrollBar_scroll.SmallChange);
					long newColumn = this.hScrollBar_scroll.Value - deltaCols; // Notches>0 => nach links
					newColumn = Math.Clamp(newColumn, 0, (long) this.hScrollBar_scroll.Maximum);
					this.hScrollBar_scroll.Value = (int) newColumn;
					this.viewOffsetFrames = this.hScrollBar_scroll.Value * (long) this.SamplesPerPixel;
					this.UpdateViewingElements();
				}
			};

			// Also update when mouse button down (first click should reflect position)
			pictureBox_waveform.MouseDown += (s, e) =>
			{
				if (this.SelectedTrack != null)
				{
					this.UpdateViewingElements();
				}

				// Abort if right down
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
					// Start-Frame basiert auf aktuellem View-Offset + CursorX
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
				int selectedIndex = this.listBox_audios.SelectedIndex;
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
						// If track is not Playing, set position
						if (!this.SelectedTrack.PlayerPlaying)
						{
							long caretFrame = this.GetFrameUnderCursor();
							this.SelectedTrack.SetPosition(caretFrame);
							this.SelectedTrack.StartingOffset = caretFrame * chSel;
						}
						this.SelectedTrack.SelectionStart = -1;
						this.SelectedTrack.SelectionEnd = -1;
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

					// SAVE: Offset nach Längenänderung sichern
					this.SelectedTrack.ScrollOffset = this.viewOffsetFrames;
				}

				this.listBox_audios.Refresh();
				this.UpdateViewingElements();

				this.IsSelecting = false;
			};
		}

		private async Task UpdateTimer_TickAsync()
		{
			var spp = this.SamplesPerPixel;
			var track = this.SelectedTrack;
			if (track == null)
			{
				return;
			}
			this.ClampViewOffset();
			if (this.pictureBox_wave.InvokeRequired || this.textBox_timestamp.InvokeRequired)
			{
				await this.pictureBox_wave.Invoke(async () => await this.UpdateTimer_TickAsync());
				return;
			}

			// Auto-Sync
			if (this.checkBox_sync.Checked && track.PlayerPlaying)
			{
				long caretFrame = track.Position;
				long viewFrames = (long) this.pictureBox_wave.Width * Math.Max(1, spp);
				long leftMargin = 8L * Math.Max(1, spp);
				long rightMargin = 16L * Math.Max(1, spp);
				long start = this.viewOffsetFrames;

				if (caretFrame < start + leftMargin)
				{
					start = Math.Max(0, caretFrame - leftMargin);
				}
				else if (caretFrame > start + viewFrames - rightMargin)
				{
					start = Math.Max(0, caretFrame - (viewFrames - rightMargin));
				}

				if (start != this.viewOffsetFrames)
				{
					this.viewOffsetFrames = start;
					this.ClampViewOffset();
					this.RecalculateScrollBar();

					// SAVE: Auto-Scroll in Track persistieren
					track.ScrollOffset = this.viewOffsetFrames;
				}
			}

			// Immer den aktuellen viewOffsetFrames verwenden
			long? offsetFrames = this.CurrentScrollOffsetFrames;

			Bitmap bmp = await track.DrawWaveformAsync(
				this.pictureBox_wave.Width,
				this.pictureBox_wave.Height,
				spp,
				false,
				this.CaretWidth,
				offsetFrames,
				Color.Black,
				this.pictureBox_wave.BackColor,
				Color.Red,
				false,
				0,
				0,
				2);

			this.UpdateViewingElements();
			this.textBox_timestamp.Text = (track.PlayerPlaying ? track.CurrentTime : track.Duration).ToString("hh\\:mm\\:ss\\.fff");
			this.pictureBox_wave.Image = bmp;
			GC.Collect();
		}

		// IO
		private async void button_load_Click(object sender, EventArgs e)
		{
			int selectedIndex = this.listBox_audios.SelectedIndex;

			// If Ctrl down, LoadDialog
			if (ModifierKeys.HasFlag(Keys.Control))
			{
				// Open LoadDialog
				using (var loadDialog = new Dialogs.LoadDialog())
				{
					var result = loadDialog.ShowDialog(this);
					if (result == DialogResult.OK)
					{
						// Get Results
						var results = loadDialog.Results;
						foreach (var item in results)
						{
							this.AudioC.Audios.Add(item);
						}

						// Refresh ListBox
						this.listBox_audios.Refresh();
						this.listBox_audios.SelectedIndex = -1;
						this.listBox_audios.SelectedIndex = Math.Clamp(selectedIndex, -1, this.listBox_audios.Items.Count - 1);
					}
				}
			}
			else
			{
				using var ofd = new OpenFileDialog()
				{
					Filter = "Audio Files|*.wav;*.mp3;*.flac",
					Title = "Select Audio File(s)",
					Multiselect = true,
				};
				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					int loadedCount = 0;
					foreach (var filePath in ofd.FileNames)
					{
						var verifiedPath = AudioCollection.VerifyAudioFile(filePath);
						if (verifiedPath != null)
						{
							var audioObj = await AudioObj.FromFileAsync(verifiedPath);
							if (audioObj == null)
							{
								LogCollection.Log($"Failed to load audio file: {filePath}");
								continue;
							}

							this.AudioC.Audios.Add(audioObj);
							loadedCount++;
						}
						else
						{
							LogCollection.Log($"Unsupported audio format: {filePath}");
						}
					}

					this.listBox_audios.SelectedIndex = -1;
					this.listBox_audios.SelectedIndex = this.listBox_audios.Items.Count - 1;
					LogCollection.Log($"{loadedCount} audio samples loaded.");
				}
			}
		}

		private async void button_export_Click(object sender, EventArgs e)
		{
			if (this.SelectedGuids.Count > 1)
			{
				await this.AudioC.ExportManyAsync(this.SelectedGuids);
				return;
			}

			if (ModifierKeys.HasFlag(Keys.Shift))
			{
				// Export All
				int successCount = 0;
				foreach (var track in this.AudioC.Audios)
				{
					string exportDirectory = Path.GetFullPath(this.AudioC.ExportPath);
					string exportFilePath = Path.Combine(exportDirectory, $"{track.Name}_export.wav");
					string? resultPath = await this.AudioC.Exporter.ExportWavAsync(track, 24);
					if (string.IsNullOrEmpty(resultPath))
					{
						successCount++;
						LogCollection.Log($"Failed to export audio sample: {track.Name}");
					}
				}
				LogCollection.Log($"Exported {successCount} / {this.AudioC.Audios.Count} audio samples.");
				return;
			}
			else
			{
				var track = this.SelectedTrack;
				if (track == null)
				{
					return;
				}

				string exportDirectory = Path.GetFullPath(this.AudioC.ExportPath);
				string? exportFilePath = Path.Combine(exportDirectory, $"{track.Name}_export.wav");
				if (ModifierKeys.HasFlag(Keys.Control))
				{
					using var sfd = new SaveFileDialog()
					{
						InitialDirectory = exportDirectory,
						Filter = "WAV File|*.wav",
						Title = "Select Export File Path",
						FileName = $"{track.Name}_export.wav"
					};
					if (sfd.ShowDialog(this) == DialogResult.OK)
					{
						exportFilePath = sfd.FileName;
						exportDirectory = Path.GetDirectoryName(exportFilePath) ?? exportDirectory;
					}
					else
					{
						return;
					}
				}

				exportFilePath = await this.AudioC.Exporter.ExportWavAsync(track, 24, exportDirectory);
				if (string.IsNullOrEmpty(exportFilePath))
				{
					LogCollection.Log($"Failed to export audio sample: {track.Name}");
				}
			}


		}

		private async void button_reload_Click(object sender, EventArgs e)
		{
			int selectedIndex = this.listBox_audios.SelectedIndex;
			var track = this.SelectedTrack;
			if (track == null)
			{
				return;
			}

			await Task.Run(track.LoadAudioFile);
			LogCollection.Log($"Reloaded audio sample: {track.Name}");

			this.listBox_audios.SelectedIndex = -1;
			this.listBox_audios.SelectedIndex = Math.Clamp(selectedIndex, -1, this.listBox_audios.Items.Count - 1);
		}

		private async void button_remove_Click(object sender, EventArgs e)
		{
			if (ModifierKeys.HasFlag(Keys.Control))
			{
				await this.AudioC.ClearAsync();
				this.AudioC.Audios.Clear();
				this.listBox_audios.SelectedIndex = -1;
				LogCollection.Log("Removed all audio samples.");
				return;
			}

			int selectedIndex = this.listBox_audios.SelectedIndex;
			var track = this.SelectedTrack;
			if (track == null)
			{
				return;
			}

			Guid id = track.Id;

			var removed = await this.AudioC.RemoveAsync(track.Id);
			if (this.AudioC.Audios.Any(a => a.Id == id))
			{
				LogCollection.Log($"Failed to remove audio sample: {track.Name}");
				return;
			}
			else
			{
				LogCollection.Log($"Removed audio sample: {track.Name}");
			}

			this.listBox_audios.Refresh();
			this.UpdateViewingElements();
			this.listBox_audios.SelectedIndex = -1;
			this.listBox_audios.SelectedIndex = Math.Clamp(selectedIndex, -1, this.listBox_audios.Items.Count - 1);
		}

		private void numericUpDown_skipTracks_ValueChanged(object? sender, EventArgs e)
		{
			// Clamp to current count and rebind view
			this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;
			if (this.numericUpDown_skipTracks.Value > this.numericUpDown_skipTracks.Maximum)
			{
				this.numericUpDown_skipTracks.Value = this.numericUpDown_skipTracks.Maximum;
			}
			this.RebindAudioListForSkip();
		}


		// Playback
		private async void button_playback_Click(object sender, EventArgs e)
		{
			if (this.SelectedGuids.Count > 1)
			{
				await this.AudioC.PlayManyAsync(this.SelectedGuids);
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

			// If CTRL down, stop all
			if (Control.ModifierKeys.HasFlag(Keys.Control))
			{
				await this.AudioC.StopAllAsync();
				this.PlaybackCancellationTokens.Clear();
				LogCollection.Log("All tracks stopped.");
				return;
			}

			if (!track.Playing && !track.Paused)
			{
				this.PlaybackCancellationTokens.AddOrUpdate(track.Id, new CancellationToken(), (key, oldValue) => new CancellationToken());
				this.button_playback.Text = "■";
				await track.PlayAsync(this.PlaybackCancellationTokens.GetValueOrDefault(track.Id), onPlaybackStopped, this.Volume);
			}
			else
			{
				await track.StopAsync();
				this.PlaybackCancellationTokens.TryRemove(track.Id, out _);

				this.button_playback.Text = "▶";
			}
		}

		private void vScrollBar_volume_Scroll(object sender, ScrollEventArgs e)
		{
			if (this.SelectedTrack != null)
			{
				this.SelectedTrack.SetVolume(this.Volume);
			}

			// If mouse just went up, log volume
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

			var copiedTrack = await track.CloneFromSelectionAsync();
			if (copiedTrack != null)
			{
				this.AudioC.Audios.Add(copiedTrack);
				LogCollection.Log($"{track.Name} copied to {copiedTrack.Name}.");
			}
			else
			{
				LogCollection.Log($"Failed to copy {track.Name}.");
			}
		}

		private void button_selectionMode_Click(object sender, EventArgs e)
		{
			Dictionary<string, string> modes = new()
			{
				{ "Select", "⛶" },
				{ "Erase", "⛏️" }
			};

			// Determine next mode based on current SelectionMode
			var keys = modes.Keys.ToList();
			int currentIdx = Math.Max(0, keys.FindIndex(k => k.Equals(this.SelectionMode, StringComparison.OrdinalIgnoreCase)));
			int nextIdx = (currentIdx + 1) % keys.Count;
			this.SelectionMode = keys[nextIdx];

			// Update UI to reflect new mode
			this.label_selectionMode.Text = $"{this.SelectionMode}";
			switch (this.SelectionMode)
			{
				case "Select":
					this.label_selectionMode.ForeColor = Color.Green;
					this.button_copy.Enabled = true;
					break;
				case "Erase":
					this.label_selectionMode.ForeColor = Color.Red;
					this.button_copy.Enabled = false;
					break;
				default:
					this.label_selectionMode.ForeColor = Color.Black;
					this.button_copy.Enabled = true;
					break;
			}

			// Set button icon for the new mode
			this.button_selectionMode.Text = modes[this.SelectionMode];
		}

		private void hScrollBar_scroll_Scroll(object sender, ScrollEventArgs e)
		{
			var track = this.SelectedTrack;
			if (track == null)
			{
				return;
			}
			this.viewOffsetFrames = (long) this.hScrollBar_scroll.Value * this.SamplesPerPixel;
			track.ScrollOffset = this.viewOffsetFrames; // SAVE: Scrollbar → Track
			this.ClampViewOffset();
			this.UpdateViewingElements();
		}

		private void numericUpDown_samplesPerPixel_ValueChanged(object sender, EventArgs e)
		{
			var track = this.SelectedTrack;
			if (track == null)
			{
				return;
			}

			track.LastSamplesPerPixel = this.SamplesPerPixel;

			// Beim Zoom per NumericUpDown: Offset gültig halten und speichern
			this.ClampViewOffset();
			this.RecalculateScrollBar();
			track.ScrollOffset = this.viewOffsetFrames; // SAVE
			this.UpdateViewingElements();
		}


		// Processing
		private void button_autoCut_Click(object sender, EventArgs e)
		{
			// Open LoadDialog
			using (var loadDialog = new Dialogs.LoadDialog(this.SelectedTrack))
			{
				var result = loadDialog.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					// Get Results
					var results = loadDialog.Results;
					int beforeCount = this.AudioC.Audios.Count;
					foreach (var item in results)
					{
						this.AudioC.Audios.Add(item);
					}

					// Set SkipTracks to first added index - 1 so new items appear first
					if (this.AudioC.Audios.Count > beforeCount)
					{
						int firstIndex = beforeCount;
						int desiredSkip = Math.Max(0, firstIndex - 1);
						this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;
						this.numericUpDown_skipTracks.Value = Math.Min(desiredSkip, (int) this.numericUpDown_skipTracks.Maximum);
					}

					// Refresh ListBox
					this.listBox_audios.SelectedIndex = -1;
					this.listBox_audios.SelectedIndex = this.listBox_audios.Items.Count - 1;
				}
			}
		}

		private void button_drumSet_Click(object sender, EventArgs e)
		{
			// Open Drumset Dialog
			using (var drumsetDialog = new Dialogs.DrumsetDialog(this.AudioC.Audios))
			{
				var result = drumsetDialog.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					// Get Result Sample
					var resultSample = drumsetDialog.ResultSample;
					if (resultSample != null)
					{
						int beforeCount = this.AudioC.Audios.Count;
						this.AudioC.Audios.Add(resultSample);

						// Adjust skip to index - 1 of first added
						int firstIndex = beforeCount;
						int desiredSkip = Math.Max(0, firstIndex - 1);
						this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;
						this.numericUpDown_skipTracks.Value = Math.Min(desiredSkip, (int) this.numericUpDown_skipTracks.Maximum);

						// Refresh ListBox
						this.listBox_audios.SelectedIndex = -1;
						this.listBox_audios.SelectedIndex = this.listBox_audios.Items.Count - 1;
					}
				}
			}
		}


		// Private helpers
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
			this.ClampViewOffset(); // sicherstellen, dass Offset zur Länge passt
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

		private void UpdateViewingElements()
		{
			var track = this.SelectedTrack;
			if (track == null)
			{
				this.button_playback.Text = "▶";
				this.label_sampleArea.Text = "No sample area available or selected.";
				this.label_sampleAtCursor.Text = "Sample at Cursor: -";
				return;
			}

			this.button_playback.Text = track.Playing ? "■" : "▶";
			// Scrollbar auf Basis des aktuellen Offsets/SPP neu anpassen
			this.RecalculateScrollBar();

			int ch = Math.Max(1, track.Channels);
			long frameIndex = this.GetFrameUnderCursor();
			double timeSeconds = frameIndex / (double) track.SampleRate;
			this.label_sampleArea.Text = track.SelectionStart >= 0 && track.SelectionEnd >= 0
				? $"Sample Area: {Math.Min(track.SelectionStart, track.SelectionEnd)} - {Math.Max(track.SelectionStart, track.SelectionEnd)} (Duration: {TimeSpan.FromSeconds(Math.Abs(track.SelectionEnd - track.SelectionStart) / (double) (track.SampleRate * ch)).ToString("hh\\:mm\\:ss\\.fff")})"
				: "No sample area available or selected.";
			var ts = TimeSpan.FromSeconds(timeSeconds);
			this.label_sampleAtCursor.Text = $"Sample at {(this.CursorOverPictureBox ? "Cursor" : "Caret")}: {frameIndex} ({ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3} sec.)"; this.label_sampleAtCursor.ForeColor = this.CursorOverPictureBox ? Color.Blue : Color.Black;
		}
	}
}
