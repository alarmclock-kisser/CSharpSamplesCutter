using CSharpSamplesCutter.Core;
using CSharpSamplesCutter.Core.Processors_V1;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSharpSamplesCutter.Forms.Dialogs
{
    public partial class LoadDialog : Form
    {
        public string? FilePath => string.IsNullOrWhiteSpace(this.textBox_path.Text) ? null : AudioCollection.VerifyAudioFile(Path.GetFullPath(this.textBox_path.Text));
        public Color TextColor => string.IsNullOrEmpty(this.textBox_path.Text) ? Color.Red : Color.Black;
        public TimeSpan? Duration => string.IsNullOrEmpty(this.FilePath) ? null : AudioCollection.GetTimeSpanFromFile(this.FilePath);

        public int SamplesCount => (int) this.numericUpDown_count.Value;
        public AudioObj? OriginalAudio { get; private set; } = null;
        public readonly float InitialSamplesPerMinute = 60;

        // Use CancellationTokenSource so we can actually cancel
        public CancellationTokenSource? Cts { get; private set; } = null;

        public List<AudioObj> Results { get; private set; } = [];

        public LoadDialog(AudioObj? originalAudio = null, float samplesPerMinute = 18)
        {
            this.InitializeComponent();

            this.StartPosition = FormStartPosition.Manual;
            this.Location = WindowsScreenHelper.GetCenterStartingPoint(this);

            this.OriginalAudio = originalAudio;
            this.InitialSamplesPerMinute = samplesPerMinute;
            this.button_process.Enabled = this.OriginalAudio != null || !string.IsNullOrEmpty(this.FilePath);

            if (this.OriginalAudio != null)
            {
                this.textBox_path.Text = this.OriginalAudio.FilePath;
                this.label_fileDuration.Text = $"Duration: {this.OriginalAudio.Duration.ToString("hh\\:mm\\:ss\\.fff")}";
                this.textBox_path.Enabled = false;
                this.numericUpDown_startTime.Maximum = (decimal) this.OriginalAudio.Duration.TotalSeconds;
                this.numericUpDown_endTime.Maximum = (decimal) this.OriginalAudio.Duration.TotalSeconds;
                this.numericUpDown_endTime.Value = this.numericUpDown_endTime.Maximum;
                this.button_browse.Enabled = false;
                this.numericUpDown_count.Value = Math.Clamp(
                    (int) (this.OriginalAudio.Duration.TotalMinutes * this.InitialSamplesPerMinute),
                    this.numericUpDown_count.Minimum,
                    this.numericUpDown_count.Maximum
                );
            }

            if (this.SamplesCount <= 0)
            {
                this.numericUpDown_minDuration.Enabled = true;
                this.numericUpDown_maxDuration.Enabled = true;
            }
            else
            {
                this.numericUpDown_minDuration.Enabled = false;
                this.numericUpDown_maxDuration.Enabled = false;
            }

            this.numericUpDown_count.MouseDown += this.numericUpDown_count_CtrlClicked;
        }

        private void textBox_path_Leave(object sender, EventArgs e)
        {
            this.textBox_path.ForeColor = this.TextColor;
            this.label_fileDuration.Text = this.Duration == null ? "Duration: -" : $"Duration: {this.Duration.Value.ToString("hh\\:mm\\:ss\\.fff")}";
            if (!string.IsNullOrEmpty(this.FilePath))
            {
                this.button_process.Enabled = true;
            }
            if (this.Duration != null)
            {
                this.numericUpDown_startTime.Maximum = (decimal) this.Duration.Value.TotalSeconds;
                this.numericUpDown_endTime.Maximum = (decimal) this.Duration.Value.TotalSeconds;
                this.numericUpDown_endTime.Value = this.numericUpDown_endTime.Maximum;
                this.numericUpDown_count.Value = Math.Clamp((int) (this.Duration.Value.TotalMinutes * this.InitialSamplesPerMinute), this.numericUpDown_count.Minimum, this.numericUpDown_count.Maximum
                );

            }
        }



        private void button_browse_Click(object sender, EventArgs e)
        {
            this.button_browse.Enabled = false;

            // OFD at MyMusic
            OpenFileDialog ofd = new()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Filter = "Audio Files|*.wav;*.mp3;*.flac",
                Title = "Select an Audio File"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.textBox_path.Text = Path.GetFullPath(ofd.FileName);
                this.textBox_path_Leave(sender, e);
            }

            this.button_browse.Enabled = true;
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            // Request cancel if a run is in progress
            this.Cts?.Cancel();
            this.DialogResult = DialogResult.Cancel;
        }

        private async void button_process_Click(object sender, EventArgs e)
        {
            // Disable buttons + create CTS
            this.button_process.Enabled = false;
            this.numericUpDown_count.Enabled = false;
            this.numericUpDown_startTime.Enabled = false;
            this.numericUpDown_endTime.Enabled = false;
            this.Cts = new CancellationTokenSource();
            this.progressBar_processing.Value = this.progressBar_processing.Minimum;

            // Start processing
            try
            {
                if (string.IsNullOrEmpty(this.FilePath) && this.OriginalAudio == null)
                {
                    throw new InvalidOperationException("Invalid file path.");
                }

                IProgress<double> progress = new Progress<double>(value =>
                {
                    double val = Math.Clamp(value * this.progressBar_processing.Maximum, this.progressBar_processing.Minimum, this.progressBar_processing.Maximum);
                    this.progressBar_processing.Value = Math.Max((int) val, (int) this.progressBar_processing.Value);
                });

                var audio = string.IsNullOrEmpty(this.FilePath)
                    ? this.OriginalAudio
                    : await AudioObj.FromFileAsync(this.FilePath);

                if (audio == null)
                {
                    throw new InvalidOperationException("Failed to load audio file.");
                }

                // Optional trim values from UI
                double? startSec = (double) this.numericUpDown_startTime.Value;
                double? endSec = (double) this.numericUpDown_endTime.Value;

                // Correct parameter order: audio, startSec, endSec, minDuration, maxDuration, cutSamplesCount, ct, progress
                this.Results = (await AudioCutter.AutoCutSamplesAsync(
                    audio,
                    startSec,
                    endSec,
                    this.SamplesCount <= 0 ? (double) this.numericUpDown_minDuration.Value : null,
                    this.SamplesCount <= 0 ? (double) this.numericUpDown_maxDuration.Value : null,
                    this.SamplesCount,
                    this.Cts.Token,
                    progress)).ToList();

                progress.Report(1.0);
                this.progressBar_processing.Value = this.progressBar_processing.Minimum;

                this.DialogResult = DialogResult.OK;
            }
            catch (OperationCanceledException)
            {
                // Canceled by user
                this.DialogResult = DialogResult.Cancel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during processing:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Abort;
            }
            finally
            {
                // Enable buttons
                this.button_process.Enabled = true;
                this.numericUpDown_count.Enabled = true;
                this.numericUpDown_startTime.Enabled = true;
                this.numericUpDown_endTime.Enabled = true;
                this.Cts?.Dispose();
                this.Cts = null;
            }
        }

        private void numericUpDown_count_ValueChanged(object sender, EventArgs e)
        {
            if (this.numericUpDown_count.Value <= 0)
            {
                this.numericUpDown_minDuration.Enabled = true;
                this.numericUpDown_maxDuration.Enabled = true;
            }
            else
            {
                this.numericUpDown_minDuration.Enabled = false;
                this.numericUpDown_maxDuration.Enabled = false;
            }
        }


        private void numericUpDown_startTime_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown_endTime_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown_count_CtrlClicked(object? sender, EventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                if (this.numericUpDown_count.Enabled == false || this.numericUpDown_count.Value <= 0)
                {
                    this.numericUpDown_count.Enabled = true;
                    this.numericUpDown_count.Value = Math.Clamp(
                        (int) (((float) (this.numericUpDown_endTime.Value - this.numericUpDown_startTime.Value) / 60f) * this.InitialSamplesPerMinute),
                        this.numericUpDown_count.Minimum,
                        this.numericUpDown_count.Maximum
                    );
                }
                else
                {
                    this.numericUpDown_count.Value = 0;
                }
            }
        }
    }
}
