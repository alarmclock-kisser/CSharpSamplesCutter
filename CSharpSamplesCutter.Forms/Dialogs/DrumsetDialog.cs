using CSharpSamplesCutter.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSharpSamplesCutter.Forms.Dialogs
{
    public partial class DrumsetDialog : Form
    {
        private readonly AudioCollection AudioC = new();

        // korrekt initialisierte Collections
        internal readonly BindingList<AudioObj> Samples = [];
        public IEnumerable<AudioObj>? ResultSamples { get; private set; } = null;

        // SelectedSample aus listBox_samples (bindet an Samples)
        internal AudioObj? SelectedSample =>
            this.listBox_samples != null &&
            this.listBox_samples.SelectedIndex >= 0 &&
            this.listBox_samples.SelectedIndex < this.Samples.Count
            ? this.Samples[this.listBox_samples.SelectedIndex]
            : null;

        // Selected drum from mapped list
        private AudioObj? SelectedDrum =>
            this.listBox_drumSet != null &&
            this.listBox_drumSet.SelectedIndex >= 0 &&
            this.listBox_drumSet.SelectedIndex < this.AudioC.Audios.Count
            ? this.AudioC.Audios[this.listBox_drumSet.SelectedIndex]
            : null;

        public CancellationTokenSource? Cts { get; private set; } = null;
        private readonly Random random = new();

        public int BarsCount => (int) this.numericUpDown_bars.Value;
        public int LoopsCount => (int) this.numericUpDown_loops.Value;
        public double StretchFactor => (double) this.numericUpDown_timeStretch.Value;

        // Erweiterte Parameter (Defaults)
        private double bpmOverride => (double) this.numericUpDown_bpmOverride.Value;
        private double swing => (double) this.numericUpDown_swing.Value;
        private double humanizeMs => (double) this.numericUpDown_humanize.Value;
        private int seed => (int) this.numericUpDown_seed.Value;
        private float targetPeak => (float) this.numericUpDown_targetPeak.Value;

        private string smallestNote => this.domainUpDown_smallestNote.SelectedItem?.ToString() ?? "16th"; // WICHTIG: Muss noch geparst werden oder in der DrumEngine geparst werden !!!

        // Selection state for waveform in pictureBox_view
        private bool isSelecting = false;
        private long selectionStartFrame = -1; // inclusive
        private long selectionEndFrame = -1;   // exclusive
        private long hoverFrame = -1;
        private CancellationTokenSource? renderCts;

        public DrumsetDialog(IEnumerable<AudioObj> samples, string[]? drums = null)
        {
            drums ??= [
        "Kick", "Snare (closed)", "Snare (open)", "Hi-Hat (closed)",
        "Hi-Hat (open)", "Tom (low)", "Tom (mid)", "Tom (high)",
        "Ride", "Crash", "Clap", "Think-Break"
    ];

            this.InitializeComponent();
            this.StartPosition = FormStartPosition.Manual;
            this.Location = WindowsScreenHelper.GetCenterStartingPoint(this);
            this.KeyPreview = true; // capture Delete key for erase

            // Samples lokal kopieren
            this.Samples = new BindingList<AudioObj>(samples.ToList());
            this.Cts = new CancellationTokenSource();

            // ComboBox: nicht editierbar, Format aktiviert
            this.comboBox_drums.FormattingEnabled = true;
            this.comboBox_drums.DropDownStyle = ComboBoxStyle.DropDownList; // << wichtig: kein Text-Edit / Caret
            this.comboBox_drums.Items.AddRange(drums);

            // listBox_samples: data-bind an Samples (zeigt Name)
            this.listBox_samples.DataSource = null;
            this.listBox_samples.DataSource = this.Samples;
            this.listBox_samples.DisplayMember = nameof(AudioObj.Name);
            this.listBox_samples.ValueMember = nameof(AudioObj.Id);

            // listBox_drumSet: bind to AudioC.Audios and format to "SampleTag — Name"
            this.listBox_drumSet.DataSource = null;
            this.listBox_drumSet.DataSource = this.AudioC.Audios;
            this.listBox_drumSet.ValueMember = nameof(AudioObj.Id);
            this.listBox_drumSet.FormattingEnabled = true;
            this.listBox_drumSet.Format += (s, e) =>
            {
                if (e.ListItem is AudioObj a)
                {
                    var tag = string.IsNullOrWhiteSpace(a.SampleTag) ? "(untagged)" : a.SampleTag;
                    var name = string.IsNullOrWhiteSpace(a.Name) ? "(unnamed)" : a.Name;
                    e.Value = $"{tag} — {name}";
                }
                else
                {
                    e.Value = e.ListItem?.ToString() ?? string.Empty;
                }
            };

            this.button_addDrum.Enabled = false;

            // Wire interactions (Designer events can stay)
            this.pictureBox_view.MouseDown += this.pictureBox_view_MouseDown;
            this.pictureBox_view.MouseMove += this.pictureBox_view_MouseMove;
            this.pictureBox_view.MouseUp += this.pictureBox_view_MouseUp;
            this.pictureBox_view.MouseLeave += this.pictureBox_view_MouseLeave;
            this.KeyDown += this.DrumsetDialog_KeyDown;
            this.KeyDown += this.Form_CtrlZ_Pressed;
            this.KeyDown += this.Form_CtrlY_Pressed;
        }

        private void comboBox_drums_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                if (this.SelectedSample == null || this.comboBox_drums.SelectedIndex < 0)
                {
                    return;
                }

                // setze tag und füge hinzu — vermeide Duplikate in AudioC.Audios (by Id)
                var tag = this.comboBox_drums.SelectedItem?.ToString() ?? string.Empty;
                this.SelectedSample.SampleTag = tag;

                bool alreadyMapped = this.AudioC.Audios.Any(ms => ms.Id == this.SelectedSample.Id);
                if (!alreadyMapped)
                {
                    this.AudioC.Audios.Add(this.SelectedSample);
                    this.Samples.Remove(this.SelectedSample);
                }
                else
                {
                    var existing = this.AudioC.Audios.First(ms => ms.Id == this.SelectedSample.Id);
                    existing.SampleTag = tag;
                    this.listBox_drumSet.Refresh();
                }

                // --- Reset ComboBox selection & remove focus so caret/text edit isn't visible ---
                // Für DropDownList: einfach SelectedIndex = -1 -> kein Eintrag angezeigt
                this.comboBox_drums.SelectedIndex = -1;
                this.comboBox_drums.DroppedDown = false;

                // Verschiebe Fokus sicher auf ein anderes Control (BeginInvoke damit die UI-Message-Loop sauber ist)
                this.BeginInvoke((Action) (() =>
                {
                    if (this.listBox_samples.CanFocus)
                    {
                        this.listBox_samples.Focus();
                    }
                    else
                    {
                        // Fallback: setze ActiveControl (kein caret)
                        this.ActiveControl = this.listBox_samples;
                    }
                }));
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private void button_remove_Click(object? sender, EventArgs e)
        {
            try
            {
                if (this.listBox_drumSet.SelectedIndex < 0 || this.listBox_drumSet.SelectedIndex >= this.AudioC.Audios.Count)
                {
                    return;
                }

                var toRemove = this.AudioC.Audios[this.listBox_drumSet.SelectedIndex];
                if (toRemove != null)
                {
                    this.AudioC.Audios.Remove(toRemove);
                }

                // Clear selection visuals
                this.selectionStartFrame = this.selectionEndFrame = -1;
                this.hoverFrame = -1;
                _ = this.RenderSelectedDrumAsync();
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private async void button_cut_Click(object? sender, EventArgs e)
        {
            try
            {
                var s = this.listBox_drumSet.SelectedItem as AudioObj;
                if (s == null)
                {
                    MessageBox.Show("No mapped sample selected.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // require a valid selection
                if (this.selectionStartFrame < 0 || this.selectionEndFrame <= this.selectionStartFrame)
                {
                    MessageBox.Show("No selection.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var cutAudio = await s.CloneFromSelectionAsync();
                if (cutAudio != null)
                {
                    int originalIndex = this.listBox_samples.SelectedIndex;
                    this.AudioC.Audios[originalIndex] = cutAudio;
                    this.listBox_samples.Refresh();
                }
                // collapse selection
                this.selectionStartFrame = this.selectionEndFrame = -1;
                await this.RenderSelectedDrumAsync();
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private void button_cancel_Click(object? sender, EventArgs e)
        {
            // Request cancel if a run is in progress
            try
            {
                this.Cts?.Cancel();
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }

            this.DialogResult = DialogResult.Cancel;
        }

        private async void button_process_Click(object? sender, EventArgs e)
        {
            if (this.Cts == null || this.Cts.IsCancellationRequested)
            {
                this.Cts = new CancellationTokenSource();
            }
            var token = this.Cts.Token;

            // Progress auf 0 setzen
            this.progressBar_processing.Value = 0;
            IProgress<double>? progress = new Progress<double>(p =>
            {
                try
                {
                    double clamped = Math.Clamp(p, 0.0, 1.0);
                    int val = (int) Math.Round(clamped * this.progressBar_processing.Maximum);
                    if (!this.progressBar_processing.IsDisposed)
                    {
                        this.progressBar_processing.Value = Math.Min(this.progressBar_processing.Maximum, Math.Max(this.progressBar_processing.Minimum, val));
                    }
                }
                catch { /* ignore */ }
            });

            try
            {
                this.button_process.Enabled = false;
                this.label_sampleInfo.Text = "Processing…";

                // Eingänge: bereits gemappte Drums (AudioC.Audios). Falls leer -> Abbruch.
                var inputs = this.AudioC.Audios.Where(a => a != null && a.Data != null && a.Data.Length > 0).ToList();
                if (inputs.Count == 0)
                {
                    MessageBox.Show("Keine gemappten Drums vorhanden.", "Drum Engine", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // BPM bestimmen:
                // Vorrang: bpmOverride > 0, sonst Durchschnitt aus (Bpm > 0 ? Bpm : ScannedBpm) oder Fallback 120
                float bpm = (float) (
                    this.bpmOverride > 0
                        ? this.bpmOverride
                        : inputs
                            .Select(a => a.Bpm > 0 ? a.Bpm : (a.ScannedBpm > 0 ? a.ScannedBpm : 0))
                            .Where(v => v > 40 && v < 300)
                            .DefaultIfEmpty(120)
                            .Average());

                // Ziel-SampleRate / Channels (optionale Normalisierung):
                // Falls heterogen -> null übergeben (DrumEngine soll übernehmen).
                int? targetSampleRate = null;
                int? targetChannels = null;
                if (inputs.Select(i => i.SampleRate).Distinct().Count() == 1)
                {
                    targetSampleRate = inputs.First().SampleRate;
                }
                if (inputs.Select(i => i.Channels).Distinct().Count() == 1)
                {
                    targetChannels = inputs.First().Channels;
                }

                // Kleinste Note direkt verwenden (Dialog-Eigenschaft smallestNote)
                string smallest = this.smallestNote;

                // Optionales Seed erzwingen (Reproduzierbarkeit)
                int seedLocal = this.seed;
                this.random.Next(); // kein Muss, nur Konsum

                // Aufbereitung: ggf. vorab Time-Stretch aller Roh-Samples (StretchFactor != 1)
                if (Math.Abs(this.StretchFactor - 1.0) > 0.0001)
                {
                    int idx = 0;
                    foreach (var s in inputs)
                    {
                        token.ThrowIfCancellationRequested();
                        await TimeStretcher.TimeStretchAllThreadsAsync(s, 8192, 0.5f, this.StretchFactor, false, 1.0f);
                        progress?.Report(0.10 * (++idx / (double) inputs.Count));
                    }
                }
                else
                {
                    progress?.Report(0.05);
                }

                // DrumEngine aufrufen
                var loops = await DrumEngine.GenerateLoopsAsync(
                    inputs,
                    smallestNote: smallest,
                    bpm: bpm,
                    bars: this.BarsCount,
                    count: this.LoopsCount,
                    progress: progress,
                    beatsPerBar: 4,
                    targetSampleRate: targetSampleRate,
                    targetChannels: targetChannels,
                    cancellationToken: token);

                token.ThrowIfCancellationRequested();

                // Optional: Normalisieren auf targetPeak (falls > 0 und < 1)
                if (this.targetPeak > 0.0f && this.targetPeak <= 1.0f)
                {
                    int i = 0;
                    foreach (var loop in loops)
                    {
                        token.ThrowIfCancellationRequested();
                        if (loop.Data != null && loop.Data.Length > 0)
                        {
                            float maxAbs = loop.Data.Max(v => Math.Abs(v));
                            if (maxAbs > 0.00001f)
                            {
                                float factor = this.targetPeak / maxAbs;
                                for (int s = 0; s < loop.Data.Length; s++)
                                {
                                    loop.Data[s] *= factor;
                                }
                            }
                        }
                        progress?.Report(0.80 + 0.15 * (i++ / (double) Math.Max(1, loops.Count)));
                    }
                }

                this.ResultSamples = loops;
                this.DialogResult = DialogResult.OK;
                this.label_sampleInfo.Text = $"Fertig ({loops.Count} Loops).";
                progress?.Report(1.0);
            }
            catch (OperationCanceledException)
            {
                this.label_sampleInfo.Text = "Abgebrochen.";
                this.DialogResult = DialogResult.Cancel;
            }
            catch (Exception ex)
            {
                this.label_sampleInfo.Text = $"Fehler: {ex.Message}";
                LogCollection.Log(ex);
                this.DialogResult = DialogResult.Abort;
            }
            finally
            {
                this.button_process.Enabled = true;
            }
        }

        private void button_addDrum_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this.textBox_drumTag.Text))
            {
                // Check for duplicates (by tolowercase)
                bool exists = this.comboBox_drums.Items.Cast<object>()
                    .Any(item => string.Equals(item?.ToString(), this.textBox_drumTag.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    MessageBox.Show("This drum tag already exists in the list.", "Duplicate Drum Tag", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.textBox_drumTag.Text = string.Empty;
                    return;
                }

                this.comboBox_drums.Items.Add(this.textBox_drumTag.Text.Trim());
                this.textBox_drumTag.Text = string.Empty;
            }
        }

        private void textBox_drumTag_TextChanged(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.textBox_drumTag.Text) && !this.comboBox_drums.Items.Cast<string>().Any(i => i.Equals(this.textBox_drumTag.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                this.button_addDrum.Enabled = false;
            }
            else
            {
                this.button_addDrum.Enabled = true;
            }
        }

        private void numericUpDown_seed_ValueChanged(object? sender, EventArgs e)
        {
            this.numericUpDown_seed.ValueChanged -= this.numericUpDown_seed_ValueChanged;

            this.numericUpDown_seed.Value = this.random.Next((int) this.numericUpDown_seed.Minimum, (int) this.numericUpDown_seed.Maximum);

            this.numericUpDown_seed.ValueChanged += this.numericUpDown_seed_ValueChanged;
        }

        private async void listBox_drumSet_SelectedIndexChanged(object sender, EventArgs e)
        {
            // When selecting a mapped drum, render its waveform and reset selection
            this.selectionStartFrame = this.selectionEndFrame = -1;
            this.hoverFrame = -1;
            await this.RenderSelectedDrumAsync();

            // If it was triggered by mouse click selection, play sample (mouse down, left on listbox)
            if (this.listBox_drumSet.ClientRectangle.Contains(this.listBox_drumSet.PointToClient(Cursor.Position)))
            {
                var track = this.listBox_drumSet.SelectedItem as AudioObj;
                if (track != null)
                {
                    await track.PlayAsync(CancellationToken.None, null, this.targetPeak / 2, 120);
                }
            }
        }

        // Waveform rendering and selection helpers
        private async Task RenderSelectedDrumAsync()
        {
            try
            {
                // cancel previous render if any
                this.renderCts?.Cancel();
                this.renderCts = new CancellationTokenSource();
                var ct = this.renderCts.Token;

                var s = this.SelectedDrum;
                if (s == null || this.pictureBox_view.Width <= 0 || this.pictureBox_view.Height <= 0)
                {
                    this.SetSelectionLabel();
                    this.pictureBox_view.Image?.Dispose();
                    this.pictureBox_view.Image = null;
                    return;
                }

                int width = this.pictureBox_view.Width;
                int height = this.pictureBox_view.Height;

                long totalFrames = GetTotalFrames(s);
                if (totalFrames <= 0)
                {
                    this.SetSelectionLabel();
                    this.pictureBox_view.Image?.Dispose();
                    this.pictureBox_view.Image = new Bitmap(width, height);
                    return;
                }

                int spp = (int) Math.Ceiling((double) totalFrames / Math.Max(1, width));
                spp = Math.Max(1, spp);

                // draw base waveform
                var bmp = await s.DrawWaveformAsync(width, height, samplesPerPixel: spp, drawEachChannel: false, caretWidth: 1, offset: null, waveColor: Color.Black, backColor: Color.White, caretColor: Color.Red, selectionColor: null, smoothen: true, timingMarkersInterval: 0, caretPosition: 0.0f, maxWorkers: 2);

                // overlay selection if any
                if (this.selectionStartFrame >= 0 && this.selectionEndFrame > this.selectionStartFrame)
                {
                    using var g = Graphics.FromImage(bmp);
                    int x1 = (int) Math.Round((double) this.selectionStartFrame * width / totalFrames);
                    int x2 = (int) Math.Round((double) this.selectionEndFrame * width / totalFrames);
                    if (x2 > x1)
                    {
                        using var brush = new SolidBrush(Color.FromArgb(64, Color.Khaki));
                        g.FillRectangle(brush, new Rectangle(x1, 0, Math.Min(width, x2) - x1, height));
                    }
                }

                // swap image
                this.pictureBox_view.Image?.Dispose();
                this.pictureBox_view.Image = bmp;

                this.SetSelectionLabel();
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private static long GetTotalFrames(AudioObj s)
        {
            try
            {
                if (s.Data == null || s.Data.Length == 0 || s.Channels <= 0)
                {
                    return 0;
                }
                return s.Data.LongLength / Math.Max(1, s.Channels);
            }
            catch
            {
                return 0;
            }
        }

        private void pictureBox_view_MouseDown(object? sender, MouseEventArgs e)
        {
            try
            {
                this.pictureBox_view.Focus();
                var s = this.SelectedDrum;
                if (s == null)
                {
                    return;
                }

                if (e.Button == MouseButtons.Left)
                {
                    this.isSelecting = true;
                    long total = GetTotalFrames(s);
                    long pos = this.PixelToFrame(e.X, total);
                    this.selectionStartFrame = Math.Clamp(pos, 0, Math.Max(0, total - 1));
                    this.selectionEndFrame = this.selectionStartFrame;
                    _ = this.RenderSelectedDrumAsync();
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private void pictureBox_view_MouseMove(object? sender, MouseEventArgs e)
        {
            try
            {
                var s = this.SelectedDrum;
                if (s == null)
                {
                    return;
                }

                long total = GetTotalFrames(s);
                long pos = this.PixelToFrame(e.X, total);
                this.hoverFrame = Math.Clamp(pos, 0, Math.Max(0, total - 1));

                if (this.isSelecting)
                {
                    this.selectionEndFrame = Math.Clamp(pos, 0, total);
                    // normalize order: keep start <= end
                    if (this.selectionStartFrame > this.selectionEndFrame)
                    {
                        (this.selectionStartFrame, this.selectionEndFrame) = (this.selectionEndFrame, this.selectionStartFrame);
                    }
                    _ = this.RenderSelectedDrumAsync();
                }
                else
                {
                    this.SetSelectionLabel();
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private void pictureBox_view_MouseUp(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    this.isSelecting = false;
                    this.SetSelectionLabel();
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private void pictureBox_view_MouseLeave(object? sender, EventArgs e)
        {
            try
            {
                this.hoverFrame = -1;
                this.SetSelectionLabel();
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private void DrumsetDialog_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Delete)
                {
                    var s = this.SelectedDrum;
                    if (s != null && this.selectionStartFrame >= 0 && this.selectionEndFrame > this.selectionStartFrame)
                    {
                        _ = this.EraseSelectionWithSnapshotAsync(s);
                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private async Task EraseSelectionWithSnapshotAsync(AudioObj s)
        {
            try
            {
                // Snapshot BEFORE mutation
                await this.AudioC.PushSnapshotAsync(s.Id);

                long start = this.selectionStartFrame;
                long end = this.selectionEndFrame;
                long total = GetTotalFrames(s);
                start = Math.Clamp(start, 0, Math.Max(0, total - 1));
                end = Math.Clamp(end, 0, total);
                if (end <= start)
                {
                    return;
                }

                await s.EraseSelectionAsync(start, end);

                // collapse selection to start
                this.selectionEndFrame = this.selectionStartFrame = -1;
                await this.RenderSelectedDrumAsync();
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }

        private long PixelToFrame(int x, long totalFrames)
        {
            int width = Math.Max(1, this.pictureBox_view.Width);
            double t = Math.Clamp(x, 0, width - 1) / (double) width;
            return (long) Math.Round(t * totalFrames);
        }

        private void SetSelectionLabel()
        {
            try
            {
                var s = this.SelectedDrum;
                if (s == null)
                {
                    this.label_selection.Text = "No area selected.";
                    return;
                }

                long total = GetTotalFrames(s);
                if (this.selectionStartFrame >= 0 && this.selectionEndFrame > this.selectionStartFrame)
                {
                    long len = this.selectionEndFrame - this.selectionStartFrame;
                    this.label_selection.Text = $"Selection: {this.selectionStartFrame:N0} .. {this.selectionEndFrame:N0} ({len:N0} frames)";
                }
                else if (this.hoverFrame >= 0)
                {
                    this.label_selection.Text = $"Cursor: {this.hoverFrame:N0} / {total:N0} frames";
                }
                else
                {
                    this.label_selection.Text = "No area selected.";
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }



        private async void Form_CtrlZ_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                // Versuche zuerst das aktuell ausgewählte Sample (aus SelectedSample oder SelectedDrum)
                AudioObj? track = this.SelectedSample ?? this.SelectedDrum;
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

                // Refresh beide ListBoxen
                this.listBox_drumSet.Refresh();
                this.listBox_samples.Refresh();
                
                // Re-render waveform wenn das aktuell sichtbare Sample geändert wurde
                await this.RenderSelectedDrumAsync();
                
                LogCollection.Log($"Undo applied on track: {track.Name}");
                e.Handled = true;
            }
        }

        private async void Form_CtrlY_Pressed(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Y)
            {
                // Versuche zuerst das aktuell ausgewählte Sample (aus SelectedSample oder SelectedDrum)
                AudioObj? track = this.SelectedSample ?? this.SelectedDrum;
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

                // Refresh beide ListBoxen
                this.listBox_drumSet.Refresh();
                this.listBox_samples.Refresh();
                
                // Re-render waveform wenn das aktuell sichtbare Sample geändert wurde
                await this.RenderSelectedDrumAsync();
                
                LogCollection.Log($"Redo applied on track: {track.Name}");
                e.Handled = true;
            }
        }

        private async void listBox_samples_SelectedIndexChanged(object sender, EventArgs e)
        {
            var track = this.SelectedSample;
            if (track != null)
            {
                this.label_sampleInfo.Text = $"{track.Duration.TotalSeconds:F3} sec";

                // If it was triggered by mouse click selection, play sample (mouse down, left on listbox)
                if (this.listBox_samples.ClientRectangle.Contains(this.listBox_samples.PointToClient(Cursor.Position)))
                {
                    await track.PlayAsync(CancellationToken.None, null, this.targetPeak / 2, 120);
                }
            }
            else
            {
                this.label_sampleInfo.Text = "No sample selected.";
            }

        }

        private async void button_autoMap_Click(object sender, EventArgs e)
        {
            try
            {
                var ct = this.Cts?.Token ?? CancellationToken.None;
                // Collect unmapped samples snapshot (avoid modifying collection during enumeration)
                var toMap = this.Samples.ToList();
                if (toMap.Count == 0)
                {
                    return;
                }

                // Parallel classification tasks
                var tasks = toMap.Select(async sample =>
                {
                    string tag = await DrumLooper.GetClosestDrumAsync(sample, onlyRegardFrequencies: false, cancellationToken: ct).ConfigureAwait(false);
                    sample.SampleTag = tag;
                    return sample;
                }).ToList();

                var mapped = await Task.WhenAll(tasks).ConfigureAwait(true);

                // Move mapped samples into AudioC (avoid duplicates)
                foreach (var s in mapped)
                {
                    if (!this.AudioC.Audios.Any(a => a.Id == s.Id))
                    {
                        this.AudioC.Audios.Add(s);
                        this.Samples.Remove(s);
                    }
                }

                this.listBox_drumSet.Refresh();
                this.listBox_samples.Refresh();
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
        }
    }
}
