using CSharpSamplesCutter.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSharpSamplesCutter.Forms.Dialogs
{
	public partial class DrumsetDialog : Form
	{
		internal readonly List<AudioObj> Samples = [];
		private readonly BindingList<AudioObj> MappedSamples = [];
		public AudioObj? ResultSample { get; private set; } = null;

		internal AudioObj? SelectedSample => this.listBox_samples.SelectedIndex >= 0 && this.listBox_samples.SelectedIndex < this.Samples.Count ? this.Samples[this.listBox_samples.SelectedIndex] : null;
		public CancellationTokenSource? Cts { get; private set; } = null;

		public int BarsCount => (int) this.numericUpDown_bars.Value;
		public int LoopsCount => (int) this.numericUpDown_loops.Value;
		public double StretchFactor => (double) this.numericUpDown_timeStretch.Value;

		// Erweiterte Parameter (Defaults)
		private double bpmOverride = 0.0;
		private double swing = 0.0;
		private double humanizeMs = 0.0;
		private int? seed = null;
		private float targetPeak = 0.95f;

		public DrumsetDialog(IEnumerable<AudioObj> samples, string[]? drums = null)
		{
			drums ??= ["Kick", "Snare (closed)", "Snare (open)", "Hi-Hat (closed)", "Hi-Hat (open)", "Tom (low)", "Tom (mid)", "Tom (high)", "Ride", "Crash", "Clap", "Think-Break"];

			this.InitializeComponent();
			this.Samples = samples.ToList();
			this.Cts = new CancellationTokenSource();
			this.comboBox_drums.Items.AddRange(drums);
			this.listBox_samples.Items.AddRange(this.Samples.Select(s => s.Name).ToArray());
			this.listBox_drumSet.DataSource = this.MappedSamples;
		}

		private void comboBox_drums_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.SelectedSample == null || this.comboBox_drums.SelectedIndex < 0)
			{
				return;
			}

			this.SelectedSample.SampleTag = this.comboBox_drums.SelectedItem?.ToString() ?? string.Empty;
			this.MappedSamples.Add(this.SelectedSample);

			this.comboBox_drums.SelectedIndex = -1;
		}

		private void button_remove_Click(object sender, EventArgs e)
		{

		}

		private void button_cut_Click(object sender, EventArgs e)
		{

		}

		private void button_cancel_Click(object sender, EventArgs e)
		{
			// Request cancel if a run is in progress
			this.Cts?.Cancel();
			this.DialogResult = DialogResult.Cancel;
		}

		private async void button_process_Click(object sender, EventArgs e)
		{
			if (this.Cts == null || this.Cts.IsCancellationRequested)
			{
				this.Cts = new CancellationTokenSource();
			}
			var token = this.Cts.Token;

			IProgress<double> progress = new Progress<double>(value =>
			{
				double val = Math.Clamp(value * this.progressBar_processing.Maximum, this.progressBar_processing.Minimum, this.progressBar_processing.Maximum);
				this.progressBar_processing.Value = Math.Max((int) val, (int) this.progressBar_processing.Value);
			});

			try
			{
				this.button_process.Enabled = false;
				this.progressBar_processing.Value = 0;

				// Optional Time-Stretch vor Loop-Erstellung (nur wenn != 1.0)
				if (Math.Abs(this.StretchFactor - 1.0) > 0.0001)
				{
					double factor = this.StretchFactor;
					var stretchTasks = this.Samples.Select(async sample =>
					{
						await TimeStretcher.TimeStretchAllThreadsAsync(sample, 8192, 0.5f, factor, false, 1.0f);
						progress.Report(0.25 / Math.Max(1, this.Samples.Count));
					});
					await Task.WhenAll(stretchTasks);
					progress.Report(0.25);
				}
				else
				{
					progress.Report(0.05);
				}

				// Loops generieren (DrumLooper)
				var loops = await DrumLooper.CreateDrumLoopsAsync(
					this.MappedSamples,
					barsCount: this.BarsCount,
					loopsCount: this.LoopsCount,
					bpmOverride: this.bpmOverride > 0 ? this.bpmOverride : null,
					swing: this.swing,
					humanizeMs: this.humanizeMs,
					seed: this.seed,
					targetPeak: this.targetPeak,
					progress: progress,
					cancellationToken: token);

				this.DialogResult = DialogResult.OK;
				progress.Report(1.0);
			}
			catch (OperationCanceledException)
			{
				this.label_sampleInfo.Text = "Operation canceled.";
				this.DialogResult = DialogResult.Cancel;
			}
			catch (Exception ex)
			{
				this.label_sampleInfo.Text = $"Error: {ex.Message}";
				this.DialogResult = DialogResult.Abort;
			}
			finally
			{
				this.button_process.Enabled = true;
			}
		}

		private void button_addDrum_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(this.textBox_drumTag.Text))
			{
				// Check for duplicates (by tolowercase)
				bool exists = this.comboBox_drums.Items.Cast<object>().Any(item => string.Equals(item?.ToString(), this.textBox_drumTag.Text.Trim(), StringComparison.OrdinalIgnoreCase));
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

		private void textBox_drumTag_TextChanged(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(this.textBox_drumTag.Text))
			{
				this.button_addDrum.Enabled = false;
			}
			else
			{
				this.button_addDrum.Enabled = true;
			}
		}
	}
}
