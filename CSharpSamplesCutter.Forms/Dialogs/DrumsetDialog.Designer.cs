namespace CSharpSamplesCutter.Forms.Dialogs
{
	partial class DrumsetDialog
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.comboBox_drums = new ComboBox();
			this.listBox_drumSet = new ListBox();
			this.button_remove = new Button();
			this.listBox_samples = new ListBox();
			this.label_sampleInfo = new Label();
			this.button_process = new Button();
			this.button_cancel = new Button();
			this.label_info_loops = new Label();
			this.numericUpDown_loops = new NumericUpDown();
			this.pictureBox_view = new PictureBox();
			this.button_cut = new Button();
			this.label_info_timeStretch = new Label();
			this.numericUpDown_timeStretch = new NumericUpDown();
			this.label_selection = new Label();
			this.progressBar_processing = new ProgressBar();
			this.label_info_bars = new Label();
			this.numericUpDown_bars = new NumericUpDown();
			this.button_addDrum = new Button();
			this.textBox_drumTag = new TextBox();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_loops).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.pictureBox_view).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_timeStretch).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_bars).BeginInit();
			this.SuspendLayout();
			// 
			// comboBox_drums
			// 
			this.comboBox_drums.FormattingEnabled = true;
			this.comboBox_drums.Location = new Point(12, 12);
			this.comboBox_drums.Name = "comboBox_drums";
			this.comboBox_drums.Size = new Size(200, 23);
			this.comboBox_drums.TabIndex = 0;
			this.comboBox_drums.Text = "Select Drum ...";
			this.comboBox_drums.SelectedIndexChanged += this.comboBox_drums_SelectedIndexChanged;
			// 
			// listBox_drumSet
			// 
			this.listBox_drumSet.FormattingEnabled = true;
			this.listBox_drumSet.ItemHeight = 15;
			this.listBox_drumSet.Location = new Point(508, 12);
			this.listBox_drumSet.Name = "listBox_drumSet";
			this.listBox_drumSet.Size = new Size(280, 229);
			this.listBox_drumSet.TabIndex = 1;
			// 
			// button_remove
			// 
			this.button_remove.BackColor = Color.FromArgb(  255,   192,   192);
			this.button_remove.Location = new Point(508, 247);
			this.button_remove.Name = "button_remove";
			this.button_remove.Size = new Size(60, 23);
			this.button_remove.TabIndex = 2;
			this.button_remove.Text = "Remove";
			this.button_remove.UseVisualStyleBackColor = false;
			this.button_remove.Click += this.button_remove_Click;
			// 
			// listBox_samples
			// 
			this.listBox_samples.FormattingEnabled = true;
			this.listBox_samples.ItemHeight = 15;
			this.listBox_samples.Location = new Point(12, 41);
			this.listBox_samples.Name = "listBox_samples";
			this.listBox_samples.Size = new Size(200, 289);
			this.listBox_samples.TabIndex = 3;
			// 
			// label_sampleInfo
			// 
			this.label_sampleInfo.AutoSize = true;
			this.label_sampleInfo.Location = new Point(12, 333);
			this.label_sampleInfo.Name = "label_sampleInfo";
			this.label_sampleInfo.Size = new Size(113, 15);
			this.label_sampleInfo.TabIndex = 4;
			this.label_sampleInfo.Text = "No sample selected.";
			// 
			// button_process
			// 
			this.button_process.Location = new Point(713, 415);
			this.button_process.Name = "button_process";
			this.button_process.Size = new Size(75, 23);
			this.button_process.TabIndex = 5;
			this.button_process.Text = "Process";
			this.button_process.UseVisualStyleBackColor = true;
			this.button_process.Click += this.button_process_Click;
			// 
			// button_cancel
			// 
			this.button_cancel.BackColor = Color.Gainsboro;
			this.button_cancel.Location = new Point(652, 415);
			this.button_cancel.Name = "button_cancel";
			this.button_cancel.Size = new Size(55, 23);
			this.button_cancel.TabIndex = 7;
			this.button_cancel.Text = "Cancel";
			this.button_cancel.UseVisualStyleBackColor = false;
			this.button_cancel.Click += this.button_cancel_Click;
			// 
			// label_info_loops
			// 
			this.label_info_loops.AutoSize = true;
			this.label_info_loops.Location = new Point(78, 397);
			this.label_info_loops.Name = "label_info_loops";
			this.label_info_loops.Size = new Size(39, 15);
			this.label_info_loops.TabIndex = 9;
			this.label_info_loops.Text = "Loops";
			// 
			// numericUpDown_loops
			// 
			this.numericUpDown_loops.Location = new Point(78, 415);
			this.numericUpDown_loops.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
			this.numericUpDown_loops.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numericUpDown_loops.Name = "numericUpDown_loops";
			this.numericUpDown_loops.Size = new Size(60, 23);
			this.numericUpDown_loops.TabIndex = 8;
			this.numericUpDown_loops.Value = new decimal(new int[] { 4, 0, 0, 0 });
			// 
			// pictureBox_view
			// 
			this.pictureBox_view.BackColor = Color.White;
			this.pictureBox_view.Location = new Point(218, 276);
			this.pictureBox_view.Name = "pictureBox_view";
			this.pictureBox_view.Size = new Size(570, 72);
			this.pictureBox_view.TabIndex = 10;
			this.pictureBox_view.TabStop = false;
			// 
			// button_cut
			// 
			this.button_cut.BackColor = SystemColors.Info;
			this.button_cut.Location = new Point(574, 247);
			this.button_cut.Name = "button_cut";
			this.button_cut.Size = new Size(60, 23);
			this.button_cut.TabIndex = 11;
			this.button_cut.Text = "Cut";
			this.button_cut.UseVisualStyleBackColor = false;
			this.button_cut.Click += this.button_cut_Click;
			// 
			// label_info_timeStretch
			// 
			this.label_info_timeStretch.AutoSize = true;
			this.label_info_timeStretch.Location = new Point(218, 397);
			this.label_info_timeStretch.Name = "label_info_timeStretch";
			this.label_info_timeStretch.Size = new Size(74, 15);
			this.label_info_timeStretch.TabIndex = 13;
			this.label_info_timeStretch.Text = "Time Stretch";
			// 
			// numericUpDown_timeStretch
			// 
			this.numericUpDown_timeStretch.DecimalPlaces = 15;
			this.numericUpDown_timeStretch.Increment = new decimal(new int[] { 5, 0, 0, 196608 });
			this.numericUpDown_timeStretch.Location = new Point(218, 415);
			this.numericUpDown_timeStretch.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
			this.numericUpDown_timeStretch.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericUpDown_timeStretch.Name = "numericUpDown_timeStretch";
			this.numericUpDown_timeStretch.Size = new Size(120, 23);
			this.numericUpDown_timeStretch.TabIndex = 12;
			this.numericUpDown_timeStretch.Value = new decimal(new int[] { 1, 0, 0, 0 });
			// 
			// label_selection
			// 
			this.label_selection.AutoSize = true;
			this.label_selection.Location = new Point(218, 351);
			this.label_selection.Name = "label_selection";
			this.label_selection.Size = new Size(97, 15);
			this.label_selection.TabIndex = 14;
			this.label_selection.Text = "No area selected.";
			// 
			// progressBar_processing
			// 
			this.progressBar_processing.Location = new Point(344, 415);
			this.progressBar_processing.Name = "progressBar_processing";
			this.progressBar_processing.Size = new Size(302, 23);
			this.progressBar_processing.TabIndex = 15;
			// 
			// label_info_bars
			// 
			this.label_info_bars.AutoSize = true;
			this.label_info_bars.Location = new Point(12, 397);
			this.label_info_bars.Name = "label_info_bars";
			this.label_info_bars.Size = new Size(29, 15);
			this.label_info_bars.TabIndex = 17;
			this.label_info_bars.Text = "Bars";
			// 
			// numericUpDown_bars
			// 
			this.numericUpDown_bars.Location = new Point(12, 415);
			this.numericUpDown_bars.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
			this.numericUpDown_bars.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numericUpDown_bars.Name = "numericUpDown_bars";
			this.numericUpDown_bars.Size = new Size(60, 23);
			this.numericUpDown_bars.TabIndex = 16;
			this.numericUpDown_bars.Value = new decimal(new int[] { 8, 0, 0, 0 });
			// 
			// button_addDrum
			// 
			this.button_addDrum.BackColor = Color.FromArgb(  192,   255,   255);
			this.button_addDrum.Location = new Point(218, 12);
			this.button_addDrum.Name = "button_addDrum";
			this.button_addDrum.Size = new Size(50, 23);
			this.button_addDrum.TabIndex = 18;
			this.button_addDrum.Text = "Add";
			this.button_addDrum.UseVisualStyleBackColor = false;
			this.button_addDrum.Click += this.button_addDrum_Click;
			// 
			// textBox_drumTag
			// 
			this.textBox_drumTag.Location = new Point(274, 12);
			this.textBox_drumTag.Name = "textBox_drumTag";
			this.textBox_drumTag.PlaceholderText = "Enter sample tag ...";
			this.textBox_drumTag.Size = new Size(120, 23);
			this.textBox_drumTag.TabIndex = 19;
			this.textBox_drumTag.Text = "Sample";
			this.textBox_drumTag.TextChanged += this.textBox_drumTag_TextChanged;
			// 
			// DrumsetDialog
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.WhiteSmoke;
			this.ClientSize = new Size(800, 450);
			this.Controls.Add(this.textBox_drumTag);
			this.Controls.Add(this.button_addDrum);
			this.Controls.Add(this.label_info_bars);
			this.Controls.Add(this.numericUpDown_bars);
			this.Controls.Add(this.progressBar_processing);
			this.Controls.Add(this.label_selection);
			this.Controls.Add(this.label_info_timeStretch);
			this.Controls.Add(this.numericUpDown_timeStretch);
			this.Controls.Add(this.button_cut);
			this.Controls.Add(this.pictureBox_view);
			this.Controls.Add(this.label_info_loops);
			this.Controls.Add(this.numericUpDown_loops);
			this.Controls.Add(this.button_cancel);
			this.Controls.Add(this.button_process);
			this.Controls.Add(this.label_sampleInfo);
			this.Controls.Add(this.listBox_samples);
			this.Controls.Add(this.button_remove);
			this.Controls.Add(this.listBox_drumSet);
			this.Controls.Add(this.comboBox_drums);
			this.Name = "DrumsetDialog";
			this.Text = "DrumsetDialog";
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_loops).EndInit();
			((System.ComponentModel.ISupportInitialize) this.pictureBox_view).EndInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_timeStretch).EndInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_bars).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private ComboBox comboBox_drums;
		private ListBox listBox_drumSet;
		private Button button_remove;
		private ListBox listBox_samples;
		private Label label_sampleInfo;
		private Button button_process;
		private Button button_cancel;
		private Label label_info_loops;
		private NumericUpDown numericUpDown_loops;
		private PictureBox pictureBox_view;
		private Button button_cut;
		private Label label_info_timeStretch;
		private NumericUpDown numericUpDown_timeStretch;
		private Label label_selection;
		private ProgressBar progressBar_processing;
		private Label label_info_bars;
		private NumericUpDown numericUpDown_bars;
		private Button button_addDrum;
		private TextBox textBox_drumTag;
	}
}