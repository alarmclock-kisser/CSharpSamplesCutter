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
        public readonly AudioCollection AudioC_res = new(null, null, 24);

        internal readonly DynamicAudioProcessing_ViewModel AudioProcessingViewModel;

        public AudioObj? SelectedTrack => this.listBox_audios.SelectedIndex >= 0 ? this.AudioC[this.listBox_audios.SelectedValue is Guid id ? id : Guid.Empty] : this.listBox_reserve.SelectedIndex >= 0 ? this.AudioC_res[this.listBox_reserve.SelectedValue is Guid id_res ? id_res : Guid.Empty] : null;
        private AudioObj? LastSelectedTrack = null;


        private ListBox? SelectedCollectionListBox => this.listBox_audios.Items.OfType<AudioObj>().Any(a => a.Id == this.SelectedTrack?.Id) ? this.listBox_audios : this.listBox_reserve.Items.OfType<AudioObj>().Any(a => a.Id == this.SelectedTrack?.Id) ? this.listBox_reserve : null;
        internal void UpdateSelectedCollectionListBox() => _ = (this.SelectedCollectionListBox is { IsDisposed: false } lb) ? (lb.IsHandleCreated && lb.InvokeRequired ? lb.BeginInvoke(new Action(() => TryReselectInvalidateUpdate(lb))) : (object) TryReselectInvalidateUpdate(lb)) : null!;


        internal List<Guid> SelectedGuids => (this.listBox_audios.SelectionMode == System.Windows.Forms.SelectionMode.MultiExtended || this.listBox_reserve.SelectionMode == System.Windows.Forms.SelectionMode.MultiExtended) ? this.listBox_audios.SelectedItems.Cast<AudioObj?>().Select(a => a!.Id).Concat(this.listBox_reserve.SelectedItems.Cast<AudioObj?>().Select(a => a!.Id)).Where(id => id != Guid.Empty).Distinct().ToList() : this.SelectedTrack != null ? [this.SelectedTrack.Id] : [];
        private int? anchorIndexMain;
        private int? anchorIndexReserve;
        private bool suppressScrollEvent = false;
        private bool isUserScroll = false;


        private bool isDragInitiated = false;
        private int dragStartIndex = -1;
        private ListBox? dragSourceListBox = null;

        public bool LoopEnabled { get; private set; } = false;
        public double LoopStep => this.button_loop.Tag == null ? 0.0 : (double) this.button_loop.Tag;

        private readonly Timer UpdateTimer;
        private readonly ConcurrentDictionary<Guid, CancellationToken> PlaybackCancellationTokens = [];

        private bool IsSelecting = false;
        private bool CursorOverPictureBox => this.GetCursorOverPictureBox();
        private DateTime lastSpaceToggleUtc = DateTime.MinValue;
        private bool spaceKeyDebounceActive = false;

        public double FrameRate => (double) this.numericUpDown_frameRate.Value;
        public bool DrawEachChannel => this.checkBox_drawEachChannel.Checked;
        public int CaretWidth => (int) this.numericUpDown_caretWidth.Value;
        public bool SmoothenWaveform => this.checkBox_smoothen.Checked;
        public Color WaveGraphColor => this.button_colorWave.BackColor;
        public Color WaveBackColor => this.button_colorBack.BackColor;
        public Color CaretColor => this.button_colorCaret.BackColor;
        public Color SelectionColor => this.button_colorSelection.BackColor;
        public float StoredHueValue { get; set; } = 0.0f;
        public float HueAdjustment { get; set; } = 0.0f;
        private float DefaultHueAdjustment => (float) this.numericUpDown_hue.Value;
        private float StrobeHueAdjustment => 144.7f;
        public bool HueEnabled => this.checkBox_hue.Checked;
        public Color HueColor { get; set; } = Color.BlueViolet;
        public bool StrobeEffect => this.button_strobe.ForeColor.GetBrightness() < 0.3f;
        public double TimingMarkerInterval => this.ShowTimingMarkers ? (double) this.numericUpDown_timeMarkers.Value : 0;
        public bool ShowTimingMarkers => this.checkBox_timeMarkers.Checked;
        public float CaretPosition => Math.Clamp((float) this.hScrollBar_caretPosition.Value / this.hScrollBar_caretPosition.Maximum, 0.0f, 1.0f);
        public bool SoloPlayback => this.checkBox_solo.Checked;



        public int SkipTracks => (int) this.numericUpDown_skipTracks.Value;
        public int StepsBack { get; private set; } = 0;
        public string SelectionMode { get; set; } = "Select";
        public bool UsingSamplesPerPixelToFit => this.numericUpDown_samplesPerPixel.Value == 0 || this.numericUpDown_samplesPerPixel.Enabled == false;
        private int samplesPerPixelToFit => (this.pictureBox_wave.Width > 0 && this.SelectedTrack != null && this.SelectedTrack.Length > 0) ? Math.Max(1, (int) Math.Ceiling((this.SelectedTrack.Length) / this.SelectedTrack.Channels / (this.pictureBox_wave.Width * 0.9))) : 256;
        public int SamplesPerPixel => this.UsingSamplesPerPixelToFit ? this.samplesPerPixelToFit : (int) this.numericUpDown_samplesPerPixel.Value;
        public int SamplesPerPixelSelected => (int) this.numericUpDown_samplesPerPixel.Value;
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

            this.numericUpDown_frameRate.Value = (decimal) WindowsScreenHelper.GetScreenRefreshRate();

            this.AudioProcessingViewModel = new(this, this.comboBox_basicProcessing, this.button_basicProcessingGo, this.panel_basicProcessingParameters, this.checkBox_autoParameters, this.checkBox_optionalParameters);

            this.Load += this.WindowMain_Load;

            this.UpdateTimer = new Timer()
            {
                Interval = (int) (1000f / this.FrameRate)
            };

            this.UpdateTimer.Tick += async (s, e) => await this.UpdateTimer_TickAsync();
            this.UpdateTimer.Start();
        }



        // ----- Load + Bindings -----
        private void WindowMain_Load(object? sender, EventArgs e)
        {
            this.label_info_caretPosition.Text = $"Caret Position: {this.CaretPosition:P1}";

            this.listBox_audios.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox_audios.Items.Clear();
            this.listBox_audios.DrawMode = DrawMode.OwnerDrawFixed;
            this.listBox_audios.DrawItem += this.ListBox_audios_DrawItem;
            this.listBox_audios.ValueMember = "Id";
            this.listBox_audios.DisplayMember = "Name";
            this.RebindAudioListForSkip();

            this.listBox_reserve.Items.Clear();
            this.listBox_reserve.DrawMode = DrawMode.OwnerDrawFixed;
            this.listBox_reserve.DrawItem += this.ListBox_audios_DrawItem;
            this.listBox_reserve.ValueMember = "Id";
            this.listBox_reserve.DisplayMember = "Name";
            this.listBox_reserve.DataSource = this.AudioC_res.Audios;
            this.listBox_reserve.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;

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

            this.listBox_audios.SelectedIndexChanged += (s, ev) => this.ListBox_Audios_SelectedValueChanged(s, ev, this.listBox_reserve);
            this.listBox_reserve.SelectedIndexChanged += (s, ev) => this.ListBox_Audios_SelectedValueChanged(s, ev, this.listBox_audios);

            // WICHTIG: Eigene MouseDown-Selektion deaktivieren, Standardverhalten der ListBox verwenden
            // this.listBox_audios.MouseDown += (s, ev) => this.ListBox_HandleSelection(this.listBox_audios, ev);
            // this.listBox_reserve.MouseDown += (s, ev) => this.ListBox_HandleSelection(this.listBox_reserve, ev);

            this.listBox_audios.MouseUp += this.ListBox_Audios_RightClickMenu;
            this.listBox_reserve.MouseUp += this.ListBox_Audios_RightClickMenu;

            this.listBox_audios.DoubleClick += (s, ev) => this.button_move_Click(this.listBox_audios, EventArgs.Empty);
            this.listBox_reserve.DoubleClick += (s, ev) => this.button_move_Click(this.listBox_reserve, EventArgs.Empty);

            this.listBox_audios.SelectedIndex = -1;
            this.listBox_reserve.SelectedIndex = -1;

            this.listBox_log.Items.Clear();
            this.listBox_log.DataSource = LogCollection.Logs;
            LogCollection.Logs.ListChanged += (s, ev) =>
            {
                try
                {
                    this.listBox_log.Invoke(() =>
                    {
                        this.listBox_log.TopIndex = this.listBox_log.Items.Count - 1;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
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
            this.numericUpDown_skipTracks.MouseDown += (s, ev) =>
            {
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    this.numericUpDown_skipTracks.Value = 0;
                }
            };

            // wire color button events correctly
            this.button_colorBack.MouseDown -= this.button_colorBack_Click; // remove any accidental binding
            this.button_colorBack.MouseDown += this.button_colorBack_MouseDown;
            this.button_colorWave.Click -= this.button_colorWave_Click;
            this.button_colorWave.Click += this.button_colorWave_Click;

            this.button_colorSelection.BackColor = GetFadedColor(this.button_colorWave.BackColor, 0.33f);

            this.KeyDown += this.Form_CtrlZ_Pressed;
            this.KeyDown += this.Form_CtrlY_Pressed;
            this.KeyDown += this.Form_CtrlC_Pressed;
            this.KeyDown += this.Form_Del_Pressed;
            this.KeyDown += this.Form_Back_Pressed;
            this.KeyDown += this.Form_Space_Pressed;
            this.Register_PictureBox_Events(this.pictureBox_wave);
            this.Register_NumericUpDown_ToBePowOf2(this.numericUpDown_scanWidth);
            this.listBox_audios.AllowDrop = true;
            this.listBox_reserve.AllowDrop = true;
            this.listBox_audios.MouseDown += this.ListBox_MouseDown_Drag;
            this.listBox_reserve.MouseDown += this.ListBox_MouseDown_Drag;
            this.listBox_audios.MouseMove += this.ListBox_MouseMove_Drag;
            this.listBox_reserve.MouseMove += this.ListBox_MouseMove_Drag;
            this.listBox_audios.DragOver += this.ListBox_DragOver;
            this.listBox_reserve.DragOver += this.ListBox_DragOver;
            this.listBox_audios.DragDrop += this.ListBox_DragDrop;
            this.listBox_reserve.DragDrop += this.ListBox_DragDrop;
            this.AllowDrop = true;
            this.DragEnter += this.WindowMain_DragEnter;
            this.DragDrop += this.WindowMain_DragDrop;
            this.comboBox_basicProcessing.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void PreviousSteps_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action(this.UpdateUndoLabel)); } catch { }
            }
            else
            {
                this.UpdateUndoLabel();
            }
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




        // ----- Global KeyDown Events -----
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

                this.StepsBack = 0;
                this.UpdateSelectedCollectionListBox(); // statt listBox_audios.Refresh()
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
                this.UpdateSelectedCollectionListBox(); // statt listBox_audios.Refresh()
                this.UpdateViewingElements();
                this.UpdateUndoLabel();
                LogCollection.Log($"Redo applied on track: {track.Name}");
            }
        }

        private async void Form_CtrlC_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
                {
                    var track = this.SelectedTrack;
                    if (track == null)
                    {
                        return;
                    }

                    this.button_copy.PerformClick();
                }
            }

            await Task.CompletedTask;
        }

        private async void Form_Del_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                // ... unverändert bis zur Operation ...
                if (this.SelectionMode.Equals("Select", StringComparison.OrdinalIgnoreCase))
                {
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

                    await track.CreateSnapshotAsync();
                    this.UpdateUndoLabel();
                    await track.EraseSelectionAsync(startSample, endSample);
                    LogCollection.Log($"Deleted selection on track: {track.Name}");

                    this.UpdateSelectedCollectionListBox(); // statt listBox_audios.Refresh()
                    this.UpdateViewingElements();
                }
            }
        }

        private async void Form_Back_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Back)
            {
                return;
            }

            // Editierkontext schützen (z.B. Textbox in GroupBox)
            if (this.IsEditingContext())
            {
                return;
            }

            // Ctrl+Backspace: Play/Stop toggle
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                await this.AudioC.StopAllAsync();
                await this.AudioC_res.StopAllAsync();
                this.button_playback.Invoke(() => { this.button_playback.Text = "▶"; });
                this.PlaybackCancellationTokens.Clear();
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

                // Zielposition: bevorzugt StartingOffset, sonst 0
                long targetFrame = track.StartingOffset > 0 ? track.StartingOffset / ch : 0;

                if (isPlaying || isPaused)
                {
                    // Playback reposition: kurz pausieren → SetPosition → View neu setzen → fortsetzen
                    try { await track.PauseAsync(); } catch { /* ignore */ }

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
                    this.viewOffsetFrames = Math.Clamp(newOffset, 0, maxOffset);
                    this.ClampViewOffset();
                    this.RecalculateScrollBar();
                    track.ScrollOffset = this.viewOffsetFrames;

                    if (!track.Playing || track.Paused)
                    {
                        try { await track.PauseAsync(); } catch { /* toggle resume */ }
                    }
                }
                else
                {
                    // Paused/Stopped: NUR repositionieren, BITTE automatisch starten
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
                    this.viewOffsetFrames = Math.Clamp(newOffset, 0, maxOffset);
                    this.ClampViewOffset();
                    this.RecalculateScrollBar();
                    track.ScrollOffset = this.viewOffsetFrames;

                    try { await track.PlayAsync(CancellationToken.None, null, this.Volume, 90); } catch { /* ignore */ }
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
                await track.CreateSnapshotAsync();
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

            // this.UpdateViewingElements();
        }

        private async void Form_Space_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Space)
            {
                return;
            }

            // 1) Wenn in einem editierbaren Kontext: Space NICHT als Play/Pause ausführen
            if (this.IsEditingContext())
            {
                return;
            }

            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            // 2) Debounce: mehrere sehr schnelle Space-Events ignorieren
            var now = DateTime.UtcNow;
            if (this.spaceKeyDebounceActive || (now - this.lastSpaceToggleUtc).TotalMilliseconds < 180)
            {
                return;
            }

            this.spaceKeyDebounceActive = true;
            this.lastSpaceToggleUtc = now;

            try
            {
                if (track.Playing || track.Paused)
                {
                    await track.PauseAsync();
                    this.button_pause.ForeColor = track.Paused ? Color.DarkGray : Color.Black;
                }
                else
                {
                    // Playback starten
                    var onPlaybackStopped = new Action(() =>
                    {
                        this.button_playback.Invoke(() => this.button_playback.Text = "▶");
                        this.PlaybackCancellationTokens.TryRemove(track.Id, out _);
                    });

                    var cts = new CancellationTokenSource();
                    this.PlaybackCancellationTokens[track.Id] = cts.Token;
                    this.button_playback.Text = "■";
                    await track.PlayAsync(cts.Token, onPlaybackStopped, this.Volume, 120);
                }
            }
            finally
            {
                // Kurz verzögert freigeben, damit „halb gedrückte“ Space-Spam-Events nicht stottern
                _ = Task.Run(async () =>
                {
                    await Task.Delay(120);
                    this.spaceKeyDebounceActive = false;
                });
            }
        }




        // ----- Further Events + Tick -----
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
                    // Scrolling jetzt auch bei Pause / nicht gestartet erlaubt
                    if (track.PlayerPlaying && this.checkBox_sync.Checked)
                    {
                        return; // Während Wiedergabe + Sync keine manuelle Verschiebung
                    }

                    int deltaCols = (int) (notches * this.hScrollBar_scroll.SmallChange);
                    long newColumn = this.hScrollBar_scroll.Value - deltaCols;
                    newColumn = Math.Clamp(newColumn, 0, (long) this.hScrollBar_scroll.Maximum);
                    this.hScrollBar_scroll.Value = (int) newColumn;

                    this.viewOffsetFrames = this.hScrollBar_scroll.Value * (long) this.SamplesPerPixel;
                    track.ScrollOffset = this.viewOffsetFrames;

                    _ = this.RedrawWaveformImmediateAsync(); // sofort sichtbar machen
                    this.UpdateViewingElements(skipScrollbarSync: true);
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
                    this.SelectedTrack.ScrollOffset = this.viewOffsetFrames;
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

                // UI-Thread sicherstellen (ohne InvokeAsync-Erweiterung):
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

                // Merker: ob wir aktuell "Follow" machen (nur bei Playback + Sync)
                bool follow = this.checkBox_sync.Checked && track.PlayerPlaying;

                // ====== A) Nur BEI PLAYBACK: Auto-Follow & Recenter ======
                if (follow)
                {
                    long viewFrames = (long) this.pictureBox_wave.Width * Math.Max(1, spp);
                    long leftMargin = 8L * Math.Max(1, spp);
                    long rightMargin = 16L * Math.Max(1, spp);

                    long caret = track.Position;
                    long start = this.viewOffsetFrames;

                    if (caret < start + leftMargin)
                    {
                        start = Math.Max(0, caret - leftMargin);
                    }
                    else if (caret > start + viewFrames - rightMargin)
                    {
                        start = Math.Max(0, caret - (viewFrames - rightMargin));
                    }

                    // Recenter auf Caret-Position innerhalb der View
                    int ch = Math.Max(1, track.Channels);
                    long totalFrames = Math.Max(1, track.Length / ch);
                    long maxOffset = Math.Max(0, totalFrames - viewFrames);

                    float caretPosClamped = Math.Clamp(this.CaretPosition, 0f, 1f);
                    long caretInViewFrames = (long) Math.Round(viewFrames * caretPosClamped);
                    long computedOffset = Math.Clamp(caret - caretInViewFrames, 0, maxOffset);

                    long wanted = Math.Max(start, computedOffset);
                    if (wanted != this.viewOffsetFrames)
                    {
                        this.viewOffsetFrames = wanted;
                        this.ClampViewOffset();

                        // Scrollbar programmgesteuert synchronisieren (Event unterdrücken)
                        this.suppressScrollEvent = true;
                        try { this.RecalculateScrollBar(); }
                        finally { this.suppressScrollEvent = false; }

                        track.ScrollOffset = this.viewOffsetFrames;
                    }
                }
                // ====== B) Paused/Stopped: KEIN Auto-Recenter ======
                // Nichts verändern; der User darf die Ansicht frei setzen.
                // Optional: nur wenn extern track.ScrollOffset geändert wurde und der User NICHT scrollt, übernehmen:
                else if (!this.isUserScroll && track.ScrollOffset != this.viewOffsetFrames)
                {
                    this.viewOffsetFrames = track.ScrollOffset;
                    this.ClampViewOffset();

                    this.suppressScrollEvent = true;
                    try { this.RecalculateScrollBar(); }
                    finally { this.suppressScrollEvent = false; }
                }

                // Hue (wie gehabt)
                if (this.HueEnabled)
                {
                    this.GetNextHue(spp);
                }

                // **WICHTIG**: Offset-Parameter für DrawWaveform:
                //  - follow == true (Playback + Sync): offsetFrames = null  -> Recenter durch DrawWaveformAsync
                //  - sonst (Pause/Stop): offsetFrames = viewOffsetFrames    -> exakt dieser View-Start
                // In UpdateTimer_TickAsync – Erzeugung des Bitmaps:
                long? offsetFrames = this.viewOffsetFrames; // immer explizit übergeben

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

                this.textBox_timestamp.Text = ((track.PlayerPlaying || track.Paused) ? track.CurrentTime : track.Duration).ToString("hh\\:mm\\:ss\\.fff");

                this.pictureBox_wave.Image = bmp;
            }
            catch (Exception ex)
            {
                LogCollection.Log($"Error in UpdateTimer_TickAsync: {ex.Message}");
            }
            finally
            {
                // NICHT die Scrollbar gegen User-Scroll neu setzen:
                this.UpdateViewingElements(skipScrollbarSync: true);
            }
        }

        private async void ListBox_Audios_SelectedValueChanged(object? sender, EventArgs e, ListBox? contraryListBox = null)
        {
            if (sender is not ListBox listBox)
            {
                return;
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
            this.textBox_trackMetrics.Text = track.GetMetricsString();
            this.label_audioName.Text = track.Name;
            this.numericUpDown_samplesPerPixel.Value = track.LastSamplesPerPixel > 0 ? track.LastSamplesPerPixel : this.samplesPerPixelToFit;
            this.hScrollBar_scroll.Enabled = true;

            // Wichtig: clampen und Scrollbar setzen
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

            // Anker auswählen je nach ListBox
            ref int? anchor = ref listBox == this.listBox_audios ? ref this.anchorIndexMain : ref this.anchorIndexReserve;

            if (ctrl)
            {
                // Toggle des einen Items
                bool currentlySelected = listBox.GetSelected(index);
                listBox.SetSelected(index, !currentlySelected);
                if (!currentlySelected)
                {
                    anchor = index;
                }
                return;
            }

            if (shift && anchor.HasValue && anchor.Value >= 0 && anchor.Value < listBox.Items.Count)
            {
                int start = Math.Min(anchor.Value, index);
                int end = Math.Max(anchor.Value, index);

                // Vorher alles deselektieren
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

            // Normaler Klick: alles andere deselektieren, nur dieses auswählen und Anker setzen
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                if (i != index && listBox.GetSelected(i))
                {
                    listBox.SetSelected(i, false);
                }
            }
            listBox.SetSelected(index, true);
            anchor = index;
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

            // Windows-Logik: ohne Ctrl nur den Eintrag selektieren, mit Ctrl toggeln
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

            // create context menu without immediate disposal
            var contextMenu = new ContextMenuStrip();
            try
            {
                var menuItemRename = new ToolStripMenuItem("Rename");
                menuItemRename.Click += (s, ev) =>
                {
                    // show input box synchronously
                    string? input = Microsoft.VisualBasic.Interaction.InputBox("Enter new name for the audio sample:", "Rename Audio Sample", track.Name);
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        string oldName = track.Name;
                        track.Name = input.Trim();

                        // refresh the listbox that opened the menu
                        listBox.Refresh();

                        LogCollection.Log($"Renamed audio sample: \"{oldName}\" to \"{track.Name}\"");
                    }
                };

                contextMenu.Items.Add(menuItemRename);

                // show the menu; the application must keep the ContextMenuStrip alive while it's open
                contextMenu.Show(listBox, e.Location);

                // dispose the menu after it was closed by the user
                contextMenu.Closed += (s, ev) =>
                {
                    // Verzögertes Dispose nach Beendigung der aktuellen Ereignisverarbeitung
                    this.BeginInvoke(new Action(() => contextMenu.Dispose()));
                };
            }
            catch
            {
                // ensure disposal on unexpected exceptions
                contextMenu.Dispose();
                throw;
            }
            finally
            {
                await Task.CompletedTask;
            }
        }



        // ----- View Settings -----
        private void button_colorWave_Click(object? sender, EventArgs e)
        {
            // Open color dialog synchronously on click; do not register further handlers inside the handler
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.WaveGraphColor,
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                // Update button and ensure readable forecolor
                var chosen = colorDialog.Color;
                this.button_colorWave.BackColor = chosen;
                this.button_colorWave.ForeColor = chosen.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void button_colorBack_Click(object? sender, EventArgs e)
        {
            // Left-click handler: open color picker for form BackColor
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.BackColor,
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.button_colorBack.BackColor = colorDialog.Color;
                this.button_colorBack.ForeColor = this.BackColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void button_colorBack_MouseDown(object? sender, MouseEventArgs e)
        {
            // Right-click: invert the form background color and update the button visual
            if (e.Button == MouseButtons.Right)
            {
                this.BackColor = GetNegativeColor(this.BackColor);
                this.button_colorBack.BackColor = GetShadedColor(this.BackColor, 0.95f);
                this.button_colorBack.ForeColor = this.BackColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
            else if (e.Button == MouseButtons.Left)
            {
                // Left click -> show dialog
                this.button_colorBack_Click(sender, EventArgs.Empty);
            }
        }

        private void button_colorCaret_Click(object sender, EventArgs e)
        {
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.CaretColor,
            };
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                this.button_colorCaret.BackColor = colorDialog.Color;
                this.button_colorCaret.ForeColor = this.CaretColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void button_colorSelection_Click(object sender, EventArgs e)
        {
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.SelectionColor
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                this.button_colorSelection.BackColor = colorDialog.Color;
                this.button_colorSelection.BackColor = GetFadedColor(colorDialog.Color, 0.25f);
                this.button_colorSelection.ForeColor = this.SelectionColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void hScrollBar_caretPosition_Scroll(object sender, ScrollEventArgs e)
        {
            this.label_info_caretPosition.Text = $"Caret Position: {(this.CaretPosition * 100f):F1}%";
        }

        private void checkBox_hue_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_hue.Checked)
            {
                this.numericUpDown_hue.Enabled = !this.StrobeEffect;
                if (this.numericUpDown_hue.Value <= 0)
                {
                    this.numericUpDown_hue.Value = 1.75m;
                }
                this.StoredHueValue = (float) this.numericUpDown_hue.Value;

                if (this.StrobeEffect)
                {
                    this.HueAdjustment = this.StrobeHueAdjustment;
                    this.numericUpDown_hue.Enabled = false;
                }
                else
                {
                    this.HueAdjustment = this.DefaultHueAdjustment;
                    this.numericUpDown_hue.Enabled = true;
                }
            }
            else
            {
                this.button_strobe.ForeColor = Color.Black;
                this.numericUpDown_hue.Enabled = false;
                this.StoredHueValue = 0.0f;
                this.HueAdjustment = 0.0f;
            }
        }

        private void button_strobe_Click(object sender, EventArgs e)
        {
            // Toggle strobe state
            bool strobeOn = this.button_strobe.ForeColor != Color.Red;

            if (strobeOn)
            {
                // Strobe aktivieren
                this.button_strobe.ForeColor = Color.Red;
                this.button_strobe.Text = "☠️";
                this.checkBox_hue.Checked = true;
                this.HueAdjustment = this.StrobeHueAdjustment;
                this.numericUpDown_hue.Enabled = false;
            }
            else
            {
                // Strobe deaktivieren
                this.button_strobe.ForeColor = Color.Black;
                this.button_strobe.Text = "⚡";
                this.HueAdjustment = this.DefaultHueAdjustment;
                this.numericUpDown_hue.Enabled = true;
            }
        }

        private void numericUpDown_hue_ValueChanged(object sender, EventArgs e)
        {
            this.StoredHueValue = (float) this.numericUpDown_hue.Value;
            if (!this.StrobeEffect && this.HueEnabled)
            {
                this.HueAdjustment = this.DefaultHueAdjustment;
            }
        }



        // ----- I/O -----
        private async void button_load_Click(object sender, EventArgs e)
        {
            int selectedIndex = this.listBox_audios.SelectedIndex;

            // If Ctrl down, LoadDialog
            if (ModifierKeys.HasFlag(Keys.Control) & !ModifierKeys.HasFlag(Keys.Shift))
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
            else if (ModifierKeys.HasFlag(Keys.Shift) & !ModifierKeys.HasFlag(Keys.Control))
            {
                // Load Entire Directory
                using var fbd = new FolderBrowserDialog()
                {
                    Description = "Select Directory Containing Audio Files",
                    UseDescriptionForTitle = true,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
                };
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    var dirPath = fbd.SelectedPath;
                    // Diese Methode fügt bereits in AudioC.Audios ein:
                    var loadedAudioObjs = await this.AudioC.LoadDirectoryAsync(dirPath);

                    // Nicht erneut hinzufügen! Nur Anzahl ermitteln:
                    int loadedCount = loadedAudioObjs.Count(a => a != null);

                    // Auswahl ans Ende setzen
                    this.listBox_audios.SelectedIndex = -1;
                    this.listBox_audios.SelectedIndex = this.listBox_audios.Items.Count - 1;

                    LogCollection.Log($"{loadedCount} audio samples loaded from directory.");
                }
            }
            else
            {
                string initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

                if (ModifierKeys.HasFlag(Keys.Shift) && ModifierKeys.HasFlag(Keys.Control))
                {
                    initialDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                }

                using var ofd = new OpenFileDialog()
                {
                    Filter = "Audio Files|*.wav;*.mp3;*.flac",
                    Title = "Select Audio File(s)",
                    Multiselect = true,
                    InitialDirectory = initialDir,
                    RestoreDirectory = true
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
            string exportDirectory = Path.GetFullPath(this.AudioC.ExportPath);
            string batchExportPath = exportDirectory;
            if (this.SelectedGuids.Count > 1)
            {
                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    // Create sub dir named batch_ + selected track name MAX first 16 chars + _ + count:D3
                    string firstTrackName = (this.SelectedTrack ?? this.AudioC.Audios.FirstOrDefault(a => a.Id == this.SelectedGuids.First()))?.Name ?? "export";
                    string safeTrackName = string.Concat(firstTrackName.Take(16).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                    string batchDirName = $"batch_{safeTrackName}_{this.SelectedGuids.Count:D3}";
                    batchExportPath = Path.Combine(exportDirectory, batchDirName);
                    Directory.CreateDirectory(batchExportPath);
                }

                var exportTasks = this.SelectedGuids.Select(async id =>
                {
                    var track = this.AudioC[id] ?? this.AudioC_res[id];
                    if (track != null)
                    {
                        string? exportedPath = await this.AudioC.Exporter.ExportWavAsync(track, 24, batchExportPath);
                        if (string.IsNullOrEmpty(exportedPath))
                        {
                            LogCollection.Log($"Failed to export audio sample: {track.Name}");
                        }
                        else
                        {
                            LogCollection.Log($"Exported audio sample: {track.Name} to {exportedPath}");
                        }
                    }
                });

                await Task.WhenAll(exportTasks);
            }
            else
            {
                var track = this.SelectedTrack;
                if (track == null)
                {
                    return;
                }

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
            // Aktive ListBox bestimmen (Priorität Main)
            ListBox? active = this.listBox_audios.SelectedItems.Count > 0
                ? this.listBox_audios
                : (this.listBox_reserve.SelectedItems.Count > 0 ? this.listBox_reserve : null);

            if (active == null)
            {
                LogCollection.Log("No audio samples selected to remove.");
                return;
            }

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                if (active == this.listBox_audios)
                {
                    await this.AudioC.ClearAsync();
                    this.RebindAudioListForSkip();
                }
                else
                {
                    await this.AudioC_res.ClearAsync();
                    active.Refresh();
                }
            }

            var selectedAudioObjs = active.SelectedItems.Cast<AudioObj>().ToList();
            if (selectedAudioObjs.Count == 0)
            {
                LogCollection.Log("No audio samples selected to remove.");
                return;
            }

            int rememberedIndex = (selectedAudioObjs.Count == 1) ? active.SelectedIndex : -1;

            var tasks = selectedAudioObjs.Select(async track =>
            {
                bool inMain = this.AudioC.Audios.Any(a => a.Id == track.Id);
                bool inReserve = this.AudioC_res.Audios.Any(a => a.Id == track.Id);

                if (inMain)
                {
                    await this.AudioC.RemoveAsync(track.Id);
                    if (this.AudioC.Audios.Any(a => a.Id == track.Id))
                    {
                        LogCollection.Log($"Failed to remove audio sample: {track.Name}");
                    }
                    else
                    {
                        LogCollection.Log($"Removed audio sample: {track.Name}");
                    }
                }
                else if (inReserve)
                {
                    await this.AudioC_res.RemoveAsync(track.Id);
                    if (this.AudioC_res.Audios.Any(a => a.Id == track.Id))
                    {
                        LogCollection.Log($"Failed to remove audio sample: {track.Name}");
                    }
                    else
                    {
                        LogCollection.Log($"Removed audio sample: {track.Name}");
                    }
                }
            });

            await Task.WhenAll(tasks);

            // Aktualisieren der ListBox-Daten
            if (active == this.listBox_audios)
            {
                this.RebindAudioListForSkip(); // wegen Slice durch SkipTracks
            }
            else
            {
                active.Refresh(); // Reserve direkt gebunden
            }

            // Bei Einzelentfernung Auswahl wiederherstellen
            if (rememberedIndex >= 0 && active.Items.Count > 0)
            {
                active.SelectedIndex = Math.Min(rememberedIndex, active.Items.Count - 1);
            }

            this.UpdateViewingElements();
        }

        private async void button_move_Click(object sender, EventArgs e)
        {
            var guidsToMove = this.SelectedGuids;

            if (guidsToMove == null || guidsToMove.Count == 0)
            {
                LogCollection.Log("No audio samples selected to move.");
                return;
            }

            var moveTasks = guidsToMove.Select(async id =>
            {
                // resolve the track (could be in main or reserve)
                var trackToMove = this.AudioC.Audios.FirstOrDefault(a => a.Id == id) ?? this.AudioC_res.Audios.FirstOrDefault(a => a.Id == id);

                if (trackToMove == null)
                {
                    LogCollection.Log($"Selected track not found for id: {id}");
                    return;
                }

                // perform move: check which collection currently owns it
                if (this.AudioC.Audios.Any(a => a.Id == id))
                {
                    // move from main -> reserve
                    this.AudioC_res.Audios.Add(await trackToMove.CloneAsync());
                    await this.AudioC.RemoveAsync(trackToMove.Id);
                    LogCollection.Log($"Moved audio sample to reserve list: {trackToMove.Name}");
                }
                else if (this.AudioC_res.Audios.Any(a => a.Id == id))
                {
                    // move from reserve -> main
                    this.AudioC.Audios.Add(await trackToMove.CloneAsync());
                    await this.AudioC_res.RemoveAsync(trackToMove.Id);
                    LogCollection.Log($"Moved audio sample to main list: {trackToMove.Name}");
                }
                else
                {
                    LogCollection.Log($"Selected track not found in either list: {trackToMove.Name}");
                }

                // if you need to update UI or selection, do it on UI thread:
                await Task.Yield();
            });

            await Task.WhenAll(moveTasks);
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




        // ----- Drag & Drop -----
        private void WindowMain_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // Form DragDrop (wenn NICHT über einer ListBox, dann in Main laden)
        private async void WindowMain_DragDrop(object? sender, DragEventArgs e)
        {
            try
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                {
                    return;
                }

                var dropped = (string[]?) e.Data.GetData(DataFormats.FileDrop);
                if (dropped == null || dropped.Length == 0)
                {
                    return;
                }

                // Position prüfen: liegt der Drop innerhalb einer ListBox? Dann gezielt in deren Collection.
                Point clientPoint = this.PointToClient(new Point(e.X, e.Y));
                bool overMain = this.listBox_audios.Bounds.Contains(clientPoint);
                bool overReserve = this.listBox_reserve.Bounds.Contains(clientPoint);

                if (overMain)
                {
                    await this.ImportDroppedItemsAsync(dropped, this.AudioC);
                }
                else if (overReserve)
                {
                    await this.ImportDroppedItemsAsync(dropped, this.AudioC_res);
                }
                else
                {
                    // Default: Main
                    await this.ImportDroppedItemsAsync(dropped, this.AudioC);
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log($"Drop: Fehler beim Import: {ex.Message}");
            }
        }

        private void ListBox_MouseDown_Drag(object? sender, MouseEventArgs e)
        {
            if (sender is not ListBox lb)
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // Nur Einzel-Reorder wenn KEIN Ctrl gedrückt ist und genau 1 Element selektiert ist.
            if (!ModifierKeys.HasFlag(Keys.Control) && lb.SelectedIndices.Count == 1)
            {
                int idx = lb.IndexFromPoint(e.Location);
                if (idx >= 0 && lb.SelectedIndex == idx)
                {
                    this.isDragInitiated = true;
                    this.dragStartIndex = idx;
                    this.dragSourceListBox = lb;
                }
            }
            else
            {
                // Bei Ctrl: normales Verhalten (Mehrfachauswahl durch Clicks), kein Reorder.
                this.isDragInitiated = false;
                this.dragStartIndex = -1;
                this.dragSourceListBox = null;
            }
        }

        private void ListBox_MouseMove_Drag(object? sender, MouseEventArgs e)
        {
            if (!this.isDragInitiated || this.dragSourceListBox == null)
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // Start des Drag&Drop
            this.isDragInitiated = false; // Nur einmal starten
            var item = this.dragSourceListBox.SelectedItem;
            if (item != null)
            {
                this.dragSourceListBox.DoDragDrop(item, DragDropEffects.Move);
            }
        }

        private void ListBox_DragOver(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox lb)
            {
                return;
            }

            // Externer File-/Ordner-Drop?
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            // Interner AudioObj-Reorder (bestehend)
            if (!ModifierKeys.HasFlag(Keys.Control) && e.Data?.GetDataPresent(typeof(AudioObj)) == true)
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void ListBox_DragDrop(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox targetListBox)
            {
                return;
            }

            // Externer File-/Ordner-Drop
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var dropped = (string[]?) e.Data.GetData(DataFormats.FileDrop);
                if (dropped == null || dropped.Length == 0)
                {
                    return;
                }

                var targetCollection = targetListBox == this.listBox_reserve ? this.AudioC_res : this.AudioC;
                await this.ImportDroppedItemsAsync(dropped, targetCollection);
                return;
            }

            // Interner Reorder (bestehend)
            if (this.dragSourceListBox == null)
            {
                return;
            }

            if (e.Data?.GetDataPresent(typeof(AudioObj)) == false)
            {
                return;
            }

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                return;
            }

            Point clientPoint = targetListBox.PointToClient(new Point(e.X, e.Y));
            int dropIndex = targetListBox.IndexFromPoint(clientPoint);
            if (dropIndex < 0)
            {
                dropIndex = targetListBox.Items.Count - 1;
            }

            // Nur wenn gleiche ListBox und einzelnes Item
            if (targetListBox == this.dragSourceListBox && this.dragStartIndex >= 0 && dropIndex >= 0 && targetListBox.SelectedIndices.Count == 1)
            {
                bool isMain = targetListBox == this.listBox_audios;
                if (isMain)
                {
                    int skip = this.SkipTracks;
                    int actualFrom = skip + this.dragStartIndex;
                    int actualTo = skip + dropIndex;
                    ReorderInCollection(this.AudioC.Audios, actualFrom, actualTo);
                    this.RebindAudioListForSkip();
                    targetListBox.SelectedIndex = Math.Min(dropIndex, targetListBox.Items.Count - 1);
                }
                else
                {
                    ReorderInCollection(this.AudioC_res.Audios, this.dragStartIndex, dropIndex);
                    targetListBox.SelectedIndex = Math.Min(dropIndex, targetListBox.Items.Count - 1);
                }
            }

            this.dragSourceListBox = null;
            this.dragStartIndex = -1;
            this.isDragInitiated = false;
        }

        private static void ReorderInCollection(IList<AudioObj> list, int from, int to)
        {
            if (from == to)
            {
                return;
            }

            if (from < 0 || to < 0 || from >= list.Count || to >= list.Count)
            {
                return;
            }

            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
        }




        // ----- Playback -----
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

            // If CTRL down, stop all
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
            this.label_volume.Text = $"Vol {this.Volume * 100.0f:0.0}%";

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

            bool shiftFlag = ModifierKeys.HasFlag(Keys.Shift);

            // Determine source list and preserve its current selection (by Id)
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
                return;
            }

            if (isInMain)
            {
                // Normal: copy stays in main | Shift: copy goes to reserve
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

                // Restore previous selection in main list
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
                // Normal: copy stays in reserve | Shift: copy goes to main
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

                // Restore previous selection in reserve list
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
                return;
            }
        }

        private void button_loop_Click(object sender, EventArgs e)
        {
            // 8 steps: 1./1 - 1 / 64
            double[] loopOptions = [1.0, 0.5, 0.25, 0.125, 0.0625, 0.03125, 0.015625];
            this.button_loop.Tag = this.button_loop.Tag is double currentLoop ? currentLoop : 0.0;

            // When enabled, cycle through options, else enable and set first option (1.0) if disabled and something selected. ForeColor set to Green and Text to the option (fraction like 1/4)
            // If nothing selected, stay disabled
            // If Ctrl down, either way, disable. ForeColor set to Blackand Text to "↺"


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

            if (this.suppressScrollEvent)
            {
                return;
            }

            // Während Playback + Sync weiterhin gesperrt
            if (track.PlayerPlaying && this.checkBox_sync.Checked)
            {
                this.suppressScrollEvent = true;
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
                finally { this.suppressScrollEvent = false; }
                return;
            }

            try
            {
                this.isUserScroll = true;
                this.viewOffsetFrames = (long) this.hScrollBar_scroll.Value * Math.Max(1, this.SamplesPerPixel);
                this.ClampViewOffset();
                track.ScrollOffset = this.viewOffsetFrames;
                _ = this.RedrawWaveformImmediateAsync(); // NEU: direkt zeichnen
            }
            finally
            {
                this.isUserScroll = false;
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

            // Offset clampen & persistieren
            this.ClampViewOffset();
            this.RecalculateScrollBar();
            track.ScrollOffset = this.viewOffsetFrames;

            _ = this.RedrawWaveformImmediateAsync(); // NEU: sofortige Aktualisierung (verhindert sichtbaren „Sprung“)
            this.UpdateViewingElements();
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

        private void checkBox_timeMarkers_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_timeMarkers.Checked)
            {
                this.numericUpDown_timeMarkers.Enabled = true;
                this.numericUpDown_timeMarkers.Tag = this.TimingMarkerInterval;
                this.numericUpDown_timeMarkers.Value = this.SelectedTrack?.Bpm > 0 ? (decimal) (60.0 / this.SelectedTrack.Bpm) : 1.0M;
            }
            else
            {
                this.numericUpDown_timeMarkers.Value = this.numericUpDown_timeMarkers.Tag is double oldVal ? (decimal) oldVal : 1.0M;
                this.numericUpDown_timeMarkers.Enabled = false;
            }
        }

        private void button_infoHotkeys_Click(object sender, EventArgs e)
        {
            bool ctrlDown = ModifierKeys.HasFlag(Keys.Control);

            using var dialog = new Dialogs.HotkeysInfoDialog(ctrlDown);
            dialog.ShowDialog(this);
        }

        private async Task ImportDroppedItemsAsync(IEnumerable<string> paths, AudioCollection targetCollection)
        {
            if (paths == null) return;
            var distinctPaths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();
            if (distinctPaths.Length == 0) return;

            List<string> fileCandidates = new();
            foreach (var p in distinctPaths)
            {
                try
                {
                    if (File.Exists(p))
                    {
                        fileCandidates.Add(p);
                    }
                    else if (Directory.Exists(p))
                    {
                        // Alle unterstützten Files im Ordner (nicht rekursiv – bei Bedarf rekursiv erweitern)
                        var files = Directory.EnumerateFiles(p)
                            .Where(IsAudioFileExtension)
                            .ToArray();
                        fileCandidates.AddRange(files);
                    }
                }
                catch (Exception ex)
                {
                    LogCollection.Log($"Drop: Zugriff auf Pfad fehlgeschlagen: {p} ({ex.Message})");
                }
            }

            if (fileCandidates.Count == 0)
            {
                LogCollection.Log("Drop: Keine validen Audio-Dateien gefunden.");
                return;
            }

            int added = 0;
            var loadTasks = fileCandidates.Select(async file =>
            {
                try
                {
                    var verified = AudioCollection.VerifyAudioFile(file);
                    if (verified == null)
                    {
                        return false;
                    }
                    var audio = await AudioObj.FromFileAsync(verified);
                    if (audio == null)
                    {
                        return false;
                    }
                    // Duplicate vermeiden (Id oder Dateiname)
                    if (!targetCollection.Audios.Any(a => string.Equals(a.DisplayName, audio.DisplayName, StringComparison.OrdinalIgnoreCase)))
                    {
                        targetCollection.Audios.Add(audio);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    LogCollection.Log($"Drop: Fehler beim Laden: {file} ({ex.Message})");
                    return false;
                }
            });

            var results = await Task.WhenAll(loadTasks);
            added = results.Count(r => r);

            if (added > 0)
            {
                LogCollection.Log($"Drop: {added} Audio-Datei(en) importiert in {(ReferenceEquals(targetCollection, this.AudioC) ? "Main" : "Reserve")}.");
                if (ReferenceEquals(targetCollection, this.AudioC))
                {
                    this.RebindAudioListForSkip();
                }
                else
                {
                    this.listBox_reserve.Refresh();
                }
            }
            else
            {
                LogCollection.Log("Drop: Keine neuen Audio-Dateien importiert (evtl. alle bereits vorhanden).");
            }
        }



        // ----- Processing -----
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
                    if (this.SelectedCollectionListBox == this.listBox_reserve)
                    {
                        foreach (var item in results)
                        {
                            this.AudioC.Audios.Add(item);
                        }
                    }
                    else
                    {
                        foreach (var item in results)
                        {
                            this.AudioC_res.Audios.Add(item);
                        }
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
                var resultObjs = drumsetDialog.ResultSamples;
                if (result == DialogResult.OK && resultObjs?.Count() > 0)
                {
                    int beforeCount = this.AudioC.Audios.Count;
                    foreach (var item in resultObjs)
                    {
                        this.AudioC.Audios.Add(item);
                    }

                    // Set SkipTracks to first added index - 1 so new items appear first
                    if (this.AudioC.Audios.Count > beforeCount)
                    {
                        int firstIndex = beforeCount;
                        int desiredSkip = Math.Max(0, firstIndex);
                        this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;
                        this.numericUpDown_skipTracks.Value = Math.Min(desiredSkip, (int) this.numericUpDown_skipTracks.Maximum);
                    }

                    // Refresh ListBox
                    this.listBox_audios.SelectedIndex = -1;
                    this.listBox_audios.SelectedIndex = this.listBox_audios.Items.Count - 1;
                }

            }
        }




        // ----- Scanning -----
        private async void button_keyScan_Click(object sender, EventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                LogCollection.Log("No track selected for key scan.");
                return;
            }

            string key = await BeatScanner.ScanKeyAsync(track, (int) this.numericUpDown_scanWidth.Value, (int) this.numericUpDown_lookingRange.Value);
            track.ScannedKey = key;
            LogCollection.Log($"Scanned key for {track.Name}: {key}");

            this.textBox_scannedKey.Text = key;
            this.UpdateSelectedCollectionListBox();
        }

        private async void button_timingScan_Click(object sender, EventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                LogCollection.Log("No track selected for timing scan.");
                return;
            }

            float timing = await BeatScanner.ScanTimingAsync(track, (int) this.numericUpDown_scanWidth.Value, (int) this.numericUpDown_lookingRange.Value);
            track.ScannedTiming = timing;
            LogCollection.Log($"Scanned timing for {track.Name}: {timing:F2}");
            this.textBox_scannedTiming.Text = this.GetTimingFractionString(timing);
            this.UpdateSelectedCollectionListBox();
        }

        private async void button_bpmScan_Click(object sender, EventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                LogCollection.Log("No track selected for timing scan.");
                return;
            }

            double bpm = await BeatScanner.ScanBpmAsync(track, (int) this.numericUpDown_scanWidth.Value, (int) this.numericUpDown_lookingRange.Value);
            track.ScannedBpm = (float) bpm;
            if (track.Bpm < 10)
            {
                track.Bpm = (float) bpm;
            }
            LogCollection.Log($"Scanned BPM for {track.Name}: {bpm:F2}");
            this.textBox_scannedBpm.Text = bpm > 0 ? bpm.ToString("F2") : "N/A";
            this.UpdateSelectedCollectionListBox();
        }




        // Private helpers
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

                // Scrollbar synchronisieren, aber nicht wenn:
                //  - gerade User scrollt (_isUserScroll)
                //  - explizit übersprungen (skipScrollbarSync)
                if (!skipScrollbarSync && !this.isUserScroll)
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
                Bitmap bmp = await track.DrawWaveformAsync(
                    this.pictureBox_wave.Width,
                    this.pictureBox_wave.Height,
                    spp,
                    this.DrawEachChannel,
                    this.CaretWidth,
                    this.viewOffsetFrames, // expliziter Offset
                    this.HueEnabled ? this.HueColor : this.WaveGraphColor,
                    this.WaveBackColor,
                    this.CaretColor,
                    this.SelectionColor,
                    this.SmoothenWaveform,
                    this.TimingMarkerInterval,
                    this.CaretPosition,
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

        private void UpdateUndoLabel()
        {
            var track = this.SelectedTrack;
            this.label_undoSteps.Text = track != null ? $"Undo's: {track.PreviousSteps.Count}" : "Undo's: 0";
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

        private void ListBox_audios_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            ListBox listBox = (ListBox) sender;

            // Sicherstellen, dass das Element existiert
            if (e.Index < 0 || e.Index >= listBox.Items.Count)
            {
                e.DrawBackground();
                return;
            }

            //1. Text und Hintergrund zeichnen

            // Zeichen den Hintergrund für den aktuellen Zustand (selected, normal)
            e.DrawBackground();

            // Hole das Datenobjekt für das aktuelle Element
            object item = listBox.Items[e.Index];
            // Hole den anzuzeigenden Text über DisplayMember ("Name")
            string itemText = item.GetType().GetProperty(listBox.DisplayMember)?.GetValue(item)?.ToString() ?? "(Error)";

            //2. Die Zeichenfläche definieren
            Rectangle textRect = e.Bounds;

            // Optional: Füge etwas Padding auf der linken Seite hinzu
            textRect.X += 2;
            textRect.Width -= 4;

            //3. Textformatierung festlegen
            TextFormatFlags flags = TextFormatFlags.VerticalCenter |
                            TextFormatFlags.Left |
                            TextFormatFlags.SingleLine |
                            TextFormatFlags.EndEllipsis;

            // Farbe des Textes bestimmen
            Color textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                        ? SystemColors.HighlightText
                        : SystemColors.WindowText;

            //4. Text zeichnen
            TextRenderer.DrawText(
                e.Graphics,
                itemText,
                e.Font,
                textRect,
                textColor,
                flags
            );

            //5. Fokus-Rechteck zeichnen (wichtig für Zugänglichkeit, falls das Element ausgewählt ist)
            e.DrawFocusRectangle();
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
            int maxAllowed = Math.Max(0, (maxValue + large - 1) - large + 1); // standard WinForms-Pattern

            bool needApply = this.hScrollBar_scroll.LargeChange != large
                          || this.hScrollBar_scroll.SmallChange != small
                          || this.hScrollBar_scroll.Maximum != (maxValue + large - 1)
                          || this.hScrollBar_scroll.Value != Math.Min(targetValue, maxAllowed);

            if (!needApply)
            {
                return;
            }

            this.suppressScrollEvent = true;
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
                this.suppressScrollEvent = false;
            }
        }

        private bool IsEditingContext()
        {
            Control? c = this.ActiveControl;
            while (c != null)
            {
                if (c is TextBoxBase or NumericUpDown or ComboBox or ListView or TreeView or DataGridView or RichTextBox or MaskedTextBox)
                {
                    return true;
                }

                if (c is UpDownBase)
                {
                    return true;
                }
                // GroupBox selbst: wenn Fokus in ihr liegt, reicht das – wir prüfen nur konkrete Edit-Controls
                c = c.Parent;
            }
            return false;
        }

        private bool IsAudioFileExtension(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                return ext is ".wav" or ".mp3" or ".flac";
            }
            catch { return false; }
        }



        // Static Helpers
        public static Color GetNegativeColor(Color color)
        {
            return Color.FromArgb(color.A, 255 - color.R, 255 - color.G, 255 - color.B);
        }

        public static Color GetShadedColor(Color color, float factor = 0.67f)
        {
            factor = Math.Clamp(factor, 0.0f, 1.0f);
            return Color.FromArgb(
                color.A,
                (int) (color.R * factor),
                (int) (color.G * factor),
                (int) (color.B * factor)
            );
        }

        public static Color GetFadedColor(Color color, float alphaFactor = 0.5f)
        {
            alphaFactor = Math.Clamp(alphaFactor, 0.0f, 1.0f);
            return Color.FromArgb(
                (int) (color.A * alphaFactor),
                color.R,
                color.G,
                color.B
            );
        }

        private Color GetNextHue(float? increment = null, bool updateHueColor = true)
        {
            if (increment == null)
            {
                increment = this.HueAdjustment;
            }

            float currentHue = this.HueColor.GetHue();
            float newHue = (currentHue + increment.Value) % 360f;

            if (updateHueColor)
            {
                this.HueColor = ColorFromHSV(newHue, 1.0f, 1.0f);
            }

            return ColorFromHSV(newHue, 1.0f, 1.0f);
        }

        public static Color ColorFromHSV(float hue, float saturation, float value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (float) Math.Floor(hue / 60);
            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));
            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q),
            };
        }

        private string GetTimingFractionString(float timing)
        {
            // Return beat timing like "1/4", "1/8", "1/16", "1/3", "2/3", etc.
            if (timing <= 0.0f)
            {
                return "-";
            }
            int denominator = (int) Math.Round(1.0f / timing);
            if (denominator <= 0)
            {
                return "-";
            }

            // Check for common triplet and dotted note timings
            if (Math.Abs(timing - (1.0f / 3.0f)) < 0.001f)
            {
                return "1/3";
            }
            else if (Math.Abs(timing - (2.0f / 3.0f)) < 0.001f)
            {
                return "2/3";
            }
            else if (Math.Abs(timing - (1.0f / 6.0f)) < 0.001f)
            {
                return "1/6";
            }
            else if (Math.Abs(timing - (1.0f / 12.0f)) < 0.001f)
            {
                return "1/12";
            }

            return $"1/{denominator}";
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

            // Anker aktualisieren
            if (listBox == this.listBox_audios)
            {
                this.anchorIndexMain = listBox.SelectedIndex >= 0 ? listBox.SelectedIndex : null;
            }
            else if (listBox == this.listBox_reserve)
            {
                this.anchorIndexReserve = listBox.SelectedIndex >= 0 ? listBox.SelectedIndex : null;
            }
        }

        private void SelectShiftedSuccessor(ListBox listBox, List<int> removedIndices)
        {
            if (removedIndices.Count == 0)
            {
                return;
            }

            // Vor dem Entfernen zwischengespeichertes Abbild der alten Reihenfolge holen
            var beforeItems = listBox.Items.Cast<AudioObj>().ToList();
            int firstRemoved = removedIndices.Min();

            // Kandidat bestimmen: erstes nicht entferntes Element ab firstRemoved (rutscht hoch)
            AudioObj? candidate = null;
            for (int i = firstRemoved; i < beforeItems.Count; i++)
            {
                if (!removedIndices.Contains(i))
                {
                    candidate = beforeItems[i];
                    break;
                }
            }

            // Falls alle ab firstRemoved entfernt wurden: vorheriges nicht entferntes Element suchen
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

            // Nach dem Entfernen aktuelle Items holen
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
                    // Fallback falls Objekt durch Filter (SkipTracks) nicht sichtbar
                    newIndex = Math.Min(firstRemoved, afterItems.Count - 1);
                }
            }
            else
            {
                // Alle entfernten lagen am Ende → letztes verbleibendes Element wählen
                newIndex = Math.Min(firstRemoved, afterItems.Count - 1);
            }

            listBox.SelectedIndex = newIndex;

            // Anker für evtl. Shift-Auswahl aktualisieren
            if (listBox == this.listBox_audios)
            {
                this.anchorIndexMain = newIndex >= 0 ? newIndex : null;
            }
            else if (listBox == this.listBox_reserve)
            {
                this.anchorIndexReserve = newIndex >= 0 ? newIndex : null;
            }
        }

        private static bool TryInvalidateUpdate(ListBox lb)
        {
            try { lb.Invalidate(); lb.Update(); return true; } catch { return false; }
        }

        private static bool TryReselectInvalidateUpdate(ListBox lb)
        {
            try
            {
                if (lb.IsDisposed)
                {
                    return false;
                }

                // Reselection erzwingen, damit SelectedIndexChanged / SelectionChangeCommitted etc. feuern
                switch (lb.SelectionMode)
                {
                    case System.Windows.Forms.SelectionMode.None:
                        // Nichts zu reselektieren
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

                    default: // MultiSimple / MultiExtended
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

                // Neu zeichnen (performanter als Refresh)
                lb.Invalidate();
                lb.Update();

                return true;
            }
            catch
            {
                // Optional: Logging
                return false;
            }
        }

    }
}
