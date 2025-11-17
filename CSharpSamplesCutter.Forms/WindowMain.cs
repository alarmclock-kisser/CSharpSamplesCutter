using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;
using CSharpSamplesCutter.Forms.Forms.MainWindow.ViewModels;
using CSharpSamplesCutter.Forms.MainWindow.ViewModels;
using Dialogs = CSharpSamplesCutter.Forms.Dialogs;
using Timer = System.Windows.Forms.Timer;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain : Form
    {
        private const int DefaultFrameRate = 60;
        private const int MinFrameIntervalMs = 5;
        private const int MaxFrameIntervalMs = 1000;
        private const int CaretInfoIntervalMs = 150;
        private const int DefaultSamplesPerPixelToFit = 128;
        private const float DefaultHueAdjustmentValue = 1.75f;
        private const float StrobeHueAdjustmentValue = 18.5f;

        private readonly Timer viewRefreshTimer;
        private readonly Timer caretInfoTimer;

        private readonly SelectionViewModel selectionState = new();
        private readonly ScrollStateViewModel scrollState = new();
        private readonly PlaybackViewModel playbackState = new();
        private readonly HueSettingsViewModel hueSettings = new();
        private readonly DragDropViewModel dragDropState = new();

        private readonly DynamicAudioProcessing_ViewModel processingViewModel;

        private long viewOffsetFrames;
        private int samplesPerPixelToFit = DefaultSamplesPerPixelToFit;
        private readonly HashSet<Guid> selectedGuidsBuffer = new();

        // Loop ViewModel hinzufügen
        private readonly LoopViewModel loopState = new();

        // View-Persistierungs-Backup (für Copy/Processing)
        private long savedViewOffsetFrames = 0;
        private int savedSamplesPerPixel = DefaultSamplesPerPixelToFit;

        public WindowMain()
        {
            this.InitializeComponent();

            this.StartPosition = FormStartPosition.Manual;
            this.Location = WindowsScreenHelper.GetCenterStartingPoint(this);

            this.AudioC = new AudioCollection();
            this.AudioC_res = new AudioCollection();

            this.selectionState.SelectionMode = "Select";
            this.hueSettings.StoredHueValue = (float) this.numericUpDown_hue.Value;
            this.hueSettings.HueAdjustment = DefaultHueAdjustmentValue;
            this.hueSettings.HueColor = this.button_colorWave.BackColor;

            this.processingViewModel = new DynamicAudioProcessing_ViewModel(
                this,
                this.comboBox_basicProcessing,
                this.button_basicProcessingGo,
                this.panel_basicProcessingParameters,
                this.checkBox_autoParameters,
                this.checkBox_optionalParameters);

            this.numericUpDown_timeMarkers.Enabled = this.checkBox_timeMarkers.Checked;
            this.label_volume.Text = $"Vol {this.Volume * 100.0f:0.0}%";

            this.viewRefreshTimer = new Timer { Interval = this.CalculateFrameIntervalMs() };
            this.viewRefreshTimer.Tick += async (s, e) => await this.UpdateTimer_TickAsync().ConfigureAwait(false);
            this.viewRefreshTimer.Start();

            this.caretInfoTimer = new Timer { Interval = CaretInfoIntervalMs };
            this.caretInfoTimer.Tick += this.CaretInfoTimer_Tick;
            this.caretInfoTimer.Start();

            this.numericUpDown_frameRate.ValueChanged += this.NumericUpDown_frameRate_ValueChanged;
            this.checkBox_timeMarkers.CheckedChanged += this.checkBox_timeMarkers_CheckedChanged;

            // Register global keyboard event handler with editing-context protection
            this.KeyDown += this.Form_KeyDown;
            this.KeyPreview = true;
            RegisterPreviewKeyDownRecursive(this);
            this.Load += this.WindowMain_Load;
        }

        // Rekursiv für alle Controls außer den beiden ListBoxen PreviewKeyDown registrieren
        private void RegisterPreviewKeyDownRecursive(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl != this.listBox_audios && ctrl != this.listBox_reserve)
                {
                    ctrl.PreviewKeyDown += Control_PreviewKeyDown_ForceArrowKeys;
                }
                if (ctrl.HasChildren)
                {
                    RegisterPreviewKeyDownRecursive(ctrl);
                }
            }
        }

        // Links/Rechts/Hoch/Runter als InputKey markieren, damit KeyDown im Form immer ausgelöst wird
        private void Control_PreviewKeyDown_ForceArrowKeys(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                e.IsInputKey = true;
            }
        }

        public AudioCollection AudioC { get; }

        public AudioCollection AudioC_res { get; }

        private bool CursorOverPictureBox => this.GetCursorOverPictureBox();

        private long CurrentScrollOffsetFrames => this.viewOffsetFrames;

        private int SamplesPerPixel => Math.Max(1, (int) this.numericUpDown_samplesPerPixel.Value);

        private float Volume => 1.0f - Math.Clamp(this.vScrollBar_volume.Value / (float) Math.Max(1, this.vScrollBar_volume.Maximum), 0f, 1f);

        private bool DrawEachChannel => this.checkBox_drawEachChannel.Checked;

        private bool SmoothenWaveform => this.checkBox_smoothen.Checked;

        private Color WaveGraphColor => this.button_colorWave.BackColor;

        private Color WaveBackColor => this.button_colorBack.BackColor;

        private Color SelectionColor => this.button_colorSelection.BackColor;

        private Color CaretColor => this.button_colorCaret.BackColor;

        private int CaretWidth => Math.Max(1, (int) this.numericUpDown_caretWidth.Value);

        private float CaretPosition => this.hScrollBar_caretPosition.Maximum > 0
            ? this.hScrollBar_caretPosition.Value / (float) this.hScrollBar_caretPosition.Maximum
            : 0.5f;

        private int TimingMarkerInterval => this.checkBox_timeMarkers.Checked
            ? Math.Max(1, (int) this.numericUpDown_timeMarkers.Value)
            : 0;

        private bool HueEnabled => this.checkBox_hue.Checked;

        private bool StrobeEffect => this.button_strobe.ForeColor == Color.Red;

        private float StoredHueValue
        {
            get => this.hueSettings.StoredHueValue;
            set => this.hueSettings.StoredHueValue = value;
        }

        private float HueAdjustment
        {
            get => this.hueSettings.HueAdjustment;
            set => this.hueSettings.HueAdjustment = value;
        }

        private Color HueColor
        {
            get => this.hueSettings.HueColor;
            set => this.hueSettings.HueColor = value;
        }

        private float DefaultHueAdjustment => DefaultHueAdjustmentValue;

        private float StrobeHueAdjustment => StrobeHueAdjustmentValue;

        private bool SoloPlayback => this.checkBox_solo.Checked;

        internal IReadOnlyCollection<Guid> SelectedGuids
        {
            get
            {
                this.selectedGuidsBuffer.Clear();
                foreach (var audio in this.listBox_audios.SelectedItems.OfType<AudioObj>())
                {
                    this.selectedGuidsBuffer.Add(audio.Id);
                }

                foreach (var audio in this.listBox_reserve.SelectedItems.OfType<AudioObj>())
                {
                    this.selectedGuidsBuffer.Add(audio.Id);
                }

                return this.selectedGuidsBuffer;
            }
        }

        internal AudioObj? SelectedTrack =>
            this.listBox_audios.SelectedItem as AudioObj ??
            this.listBox_reserve.SelectedItem as AudioObj;

        private ListBox? SelectedCollectionListBox
        {
            get
            {
                if (this.listBox_audios.SelectedIndices.Count > 0)
                {
                    return this.listBox_audios;
                }

                if (this.listBox_reserve.SelectedIndices.Count > 0)
                {
                    return this.listBox_reserve;
                }

                return null;
            }
        }

        private AudioObj? LastSelectedTrack
        {
            get => this.selectionState.LastSelectedTrack;
            set => this.selectionState.LastSelectedTrack = value;
        }

        private int? AnchorIndexMain
        {
            get => this.selectionState.AnchorIndexMain;
            set => this.selectionState.AnchorIndexMain = value;
        }

        private int? AnchorIndexReserve
        {
            get => this.selectionState.AnchorIndexReserve;
            set => this.selectionState.AnchorIndexReserve = value;
        }

        private string SelectionMode
        {
            get => this.selectionState.SelectionMode;
            set => this.selectionState.SelectionMode = value;
        }

        private bool IsSelecting
        {
            get => this.selectionState.IsSelecting;
            set => this.selectionState.IsSelecting = value;
        }

        private int StepsBack
        {
            get => this.selectionState.StepsBack;
            set => this.selectionState.StepsBack = value;
        }

        private bool SuppressScrollEvent
        {
            get => this.scrollState.SuppressScrollEvent;
            set => this.scrollState.SuppressScrollEvent = value;
        }

        private bool IsUserScroll
        {
            get => this.scrollState.IsUserScroll;
            set => this.scrollState.IsUserScroll = value;
        }

        private long ViewOffsetFrames
        {
            get => this.viewOffsetFrames;
            set
            {
                this.viewOffsetFrames = value;
                this.scrollState.ViewOffsetFrames = value;
            }
        }

        private ConcurrentDictionary<Guid, CancellationToken> PlaybackCancellationTokens => this.playbackState.CancellationTokens;

        private bool SpaceKeyDebounceActive
        {
            get => this.playbackState.SpaceKeyDebounceActive;
            set => this.playbackState.SpaceKeyDebounceActive = value;
        }

        private DateTime LastSpaceToggleUtc
        {
            get => this.playbackState.LastSpaceToggleUtc;
            set => this.playbackState.LastSpaceToggleUtc = value;
        }

        private bool LoopEnabled
        {
            get => this.playbackState.LoopEnabled;
            set => this.playbackState.LoopEnabled = value;
        }

        private bool IsDragInitiated
        {
            get => this.dragDropState.IsDragInitiated;
            set => this.dragDropState.IsDragInitiated = value;
        }

        private int DragStartIndex
        {
            get => this.dragDropState.DragStartIndex;
            set => this.dragDropState.DragStartIndex = value;
        }

        private ListBox? DragSourceListBox
        {
            get => this.dragDropState.SourceListBox;
            set => this.dragDropState.SourceListBox = value;
        }

        private int SkipTracks => Math.Max(0, (int) this.numericUpDown_skipTracks.Value);

        private readonly object playbackForceFollowGate = new object();
        private readonly System.Collections.Generic.HashSet<Guid> playbackForceFollow = new();

        private void AddPlaybackForceFollow(Guid id)
        {
            lock (this.playbackForceFollowGate)
            {
                this.playbackForceFollow.Add(id);
            }
        }

        private void RemovePlaybackForceFollow(Guid id)
        {
            lock (this.playbackForceFollowGate)
            {
                this.playbackForceFollow.Remove(id);
            }
        }

        private bool IsPlaybackForceFollow(Guid id)
        {
            lock (this.playbackForceFollowGate)
            {
                return this.playbackForceFollow.Contains(id);
            }
        }

        // Fügen Sie diese Methode in die Klasse WindowMain ein (z.B. im Bereich der privaten Methoden):

        private int CalculateFrameIntervalMs()
        {
            // Berechnet das Frame-Intervall basierend auf dem Wert von numericUpDown_frameRate
            int frameRate = (int) this.numericUpDown_frameRate.Value;
            if (frameRate <= 0)
            {
                frameRate = DefaultFrameRate;
            }

            int interval = 1000 / frameRate;
            return Math.Clamp(interval, MinFrameIntervalMs, MaxFrameIntervalMs);
        }

        private void CaretInfoTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var track = this.SelectedTrack;
                if (track == null || this.pictureBox_wave.IsDisposed)
                {
                    return;
                }

                // Get frame and time info
                long frameIndex = this.GetFrameUnderCursor();
                double timeSeconds = frameIndex / (double) Math.Max(1, track.SampleRate);
                var ts = TimeSpan.FromSeconds(timeSeconds);

                // Update sample info labels
                this.label_sampleAtCursor.Text =
                    $"Sample at {(this.CursorOverPictureBox ? "Cursor" : "Caret")}: {frameIndex} ({ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3} sec.)";
                this.label_sampleAtCursor.ForeColor = this.CursorOverPictureBox ? Color.Blue : Color.Black;
            }
            catch (Exception ex)
            {
                LogCollection.Log($"Error in CaretInfoTimer_Tick: {ex.Message}");
            }
        }

        private bool LoopEnabled_Internal
        {
            get => this.loopState.LoopEnabled;
            set => this.loopState.LoopEnabled = value;
        }

        private int LoopFractionIndex_Internal
        {
            get => this.loopState.LoopFractionIndex;
            set => this.loopState.LoopFractionIndex = value;
        }

        private int LoopFractionDenominator_Internal =>
            this.loopState.CurrentLoopFractionDenominator;

        private string LoopFractionString_Internal =>
            this.loopState.GetLoopFractionString();

        private void SaveViewState()
        {
            this.savedViewOffsetFrames = this.ViewOffsetFrames;
            this.savedSamplesPerPixel = this.SamplesPerPixel;
        }

        private void RestoreViewState()
        {
            this.numericUpDown_samplesPerPixel.Value = this.savedSamplesPerPixel;
            this.ViewOffsetFrames = this.savedViewOffsetFrames;
            this.ClampViewOffset();
            this.RecalculateScrollBar();
            var track = this.SelectedTrack;
            if (track != null)
            {
                track.ScrollOffset = this.ViewOffsetFrames;
            }
            _ = this.RedrawWaveformImmediateAsync();
        }

        private void button_autoCut_Click(object sender, EventArgs e)
        {
            using var loadDialog = new Dialogs.LoadDialog(this.SelectedTrack);
            if (loadDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var results = loadDialog.Results;
            if (results == null || results.Count == 0)
            {
                return;
            }

            // Save view state vor Operation
            this.SaveViewState();

            // Swap: wenn aus audios, dann in reserve; wenn aus reserve, dann in audios
            bool sourceIsMainList = this.SelectedCollectionListBox == this.listBox_audios;
            var targetCollection = sourceIsMainList ? this.AudioC_res : this.AudioC;

            foreach (var item in results)
            {
                targetCollection.Audios.Add(item);
            }

            // Neue Samples in der ANDEREN ListBox selektieren
            var targetList = sourceIsMainList ? this.listBox_reserve : this.listBox_audios;
            targetList.SelectedIndex = -1;
            targetList.SelectedIndex = targetList.Items.Count - 1;

            // Restore view state nach Operation
            this.RestoreViewState();
        }

        // Fügen Sie diese Methode in die Klasse WindowMain ein (z.B. im Bereich der privaten Methoden):

        private void NumericUpDown_frameRate_ValueChanged(object? sender, EventArgs e)
        {
            // Aktualisiere das Timer-Intervall, wenn sich der Wert ändert
            this.viewRefreshTimer.Interval = this.CalculateFrameIntervalMs();
        }

        private void checkBox_timeMarkers_CheckedChanged(object? sender, EventArgs e)
        {
            this.numericUpDown_timeMarkers.Enabled = this.checkBox_timeMarkers.Checked;
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

        // Fügen Sie diese Methode in die Klasse WindowMain ein

        private void button_infoHotkeys_Click(object sender, EventArgs e)
        {
            // Beispiel: Zeigen Sie eine MessageBox mit Hotkey-Informationen an
            MessageBox.Show(
                "Hotkeys:\n\n" +
                "Leertaste: Wiedergabe/Pause\n" +
                "Strg+C: Kopieren\n" +
                "Strg+Z: Rückgängig\n" +
                "Strg+Y: Wiederholen\n" +
                "Entf: Entfernen\n" +
                "Weitere Hotkeys siehe Dokumentation.",
                "Hotkey-Übersicht",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

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
            LogCollection.Log($"Scanned timing for {track.Name}: {timing}");

            this.textBox_scannedTiming.Text = timing.ToString("F3");
            this.UpdateSelectedCollectionListBox();
        }

        private async void button_bpmScan_Click(object sender, EventArgs e)
        {
            var track = this.SelectedTrack;
            if (track == null)
            {
                LogCollection.Log("No track selected for BPM scan.");
                return;
            }

            double bpm = await BeatScanner.ScanBpmAsync(track, (int) this.numericUpDown_scanWidth.Value, (int) this.numericUpDown_lookingRange.Value);
            LogCollection.Log($"Scanned BPM for {track.Name}: {bpm}");

            this.textBox_scannedBpm.Text = bpm.ToString("F2");
            this.UpdateSelectedCollectionListBox();
        }

        // Setzt die Caret-Position (0.0 bis 1.0) über den Scrollbar
        private void SetCaretPosition(float value)
        {
            if (this.hScrollBar_caretPosition.Maximum > 0)
            {
                int newValue = (int)Math.Round(Math.Clamp(value, 0f, 1f) * this.hScrollBar_caretPosition.Maximum);
                this.hScrollBar_caretPosition.Value = Math.Clamp(newValue, this.hScrollBar_caretPosition.Minimum, this.hScrollBar_caretPosition.Maximum);
            }
        }

        // Restore focus to a neutral control for global playback keys after actions
        internal void RestoreFocusAfterProcessing()
        {
            try
            {
                // Prefer the waveform surface so Space/Backspace work immediately
                if (this.pictureBox_wave != null && !this.pictureBox_wave.IsDisposed && this.pictureBox_wave.CanFocus)
                {
                    this.pictureBox_wave.Focus();
                    this.ActiveControl = this.pictureBox_wave;
                    return;
                }

                // Fallback: focus the list box that currently has a selection
                if (this.listBox_audios.SelectedIndices.Count > 0 && this.listBox_audios.CanFocus)
                {
                    this.listBox_audios.Focus();
                    this.ActiveControl = this.listBox_audios;
                }
                else if (this.listBox_reserve.SelectedIndices.Count > 0 && this.listBox_reserve.CanFocus)
                {
                    this.listBox_reserve.Focus();
                    this.ActiveControl = this.listBox_reserve;
                }
                else
                {
                    // Last resort: focus the form
                    this.Focus();
                }
            }
            catch { }
        }
    }
}
