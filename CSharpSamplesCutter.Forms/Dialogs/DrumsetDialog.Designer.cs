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
            this.numericUpDown_bpmOverride = new NumericUpDown();
            this.label_info_bpmOverride = new Label();
            this.label_info_swing = new Label();
            this.numericUpDown_swing = new NumericUpDown();
            this.label_info_humanize = new Label();
            this.numericUpDown_humanize = new NumericUpDown();
            this.numericUpDown_seed = new NumericUpDown();
            this.label_info_seed = new Label();
            this.label_info_targetPeak = new Label();
            this.numericUpDown_targetPeak = new NumericUpDown();
            this.label_info_mapping = new Label();
            this.button_autoMap = new Button();
            this.domainUpDown_smallestNote = new DomainUpDown();
            this.label_info_clickToPlay = new Label();
            this.label_info_smallestNote = new Label();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_loops).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.pictureBox_view).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_timeStretch).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_bars).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_bpmOverride).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_swing).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_humanize).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_seed).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_targetPeak).BeginInit();
            this.SuspendLayout();
            // 
            // comboBox_drums
            // 
            this.comboBox_drums.FormattingEnabled = true;
            this.comboBox_drums.Location = new Point(12, 27);
            this.comboBox_drums.Name = "comboBox_drums";
            this.comboBox_drums.Size = new Size(200, 23);
            this.comboBox_drums.TabIndex = 0;
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
            this.listBox_drumSet.SelectedIndexChanged += this.listBox_drumSet_SelectedIndexChanged;
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
            this.listBox_samples.Location = new Point(12, 59);
            this.listBox_samples.Name = "listBox_samples";
            this.listBox_samples.Size = new Size(200, 289);
            this.listBox_samples.TabIndex = 3;
            this.listBox_samples.SelectedIndexChanged += this.listBox_samples_SelectedIndexChanged;
            // 
            // label_sampleInfo
            // 
            this.label_sampleInfo.AutoSize = true;
            this.label_sampleInfo.Location = new Point(12, 351);
            this.label_sampleInfo.Name = "label_sampleInfo";
            this.label_sampleInfo.Size = new Size(113, 15);
            this.label_sampleInfo.TabIndex = 4;
            this.label_sampleInfo.Text = "No sample selected.";
            // 
            // button_process
            // 
            this.button_process.Location = new Point(713, 476);
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
            this.button_cancel.Location = new Point(652, 476);
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
            this.label_info_loops.Location = new Point(78, 458);
            this.label_info_loops.Name = "label_info_loops";
            this.label_info_loops.Size = new Size(39, 15);
            this.label_info_loops.TabIndex = 9;
            this.label_info_loops.Text = "Loops";
            // 
            // numericUpDown_loops
            // 
            this.numericUpDown_loops.Location = new Point(78, 476);
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
            this.label_info_timeStretch.Location = new Point(218, 458);
            this.label_info_timeStretch.Name = "label_info_timeStretch";
            this.label_info_timeStretch.Size = new Size(74, 15);
            this.label_info_timeStretch.TabIndex = 13;
            this.label_info_timeStretch.Text = "Time Stretch";
            // 
            // numericUpDown_timeStretch
            // 
            this.numericUpDown_timeStretch.DecimalPlaces = 15;
            this.numericUpDown_timeStretch.Increment = new decimal(new int[] { 5, 0, 0, 196608 });
            this.numericUpDown_timeStretch.Location = new Point(218, 476);
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
            this.progressBar_processing.Location = new Point(344, 476);
            this.progressBar_processing.Name = "progressBar_processing";
            this.progressBar_processing.Size = new Size(302, 23);
            this.progressBar_processing.TabIndex = 15;
            // 
            // label_info_bars
            // 
            this.label_info_bars.AutoSize = true;
            this.label_info_bars.Location = new Point(12, 458);
            this.label_info_bars.Name = "label_info_bars";
            this.label_info_bars.Size = new Size(29, 15);
            this.label_info_bars.TabIndex = 17;
            this.label_info_bars.Text = "Bars";
            // 
            // numericUpDown_bars
            // 
            this.numericUpDown_bars.Location = new Point(12, 476);
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
            // numericUpDown_bpmOverride
            // 
            this.numericUpDown_bpmOverride.DecimalPlaces = 3;
            this.numericUpDown_bpmOverride.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.numericUpDown_bpmOverride.Location = new Point(218, 432);
            this.numericUpDown_bpmOverride.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
            this.numericUpDown_bpmOverride.Minimum = new decimal(new int[] { 20, 0, 0, 0 });
            this.numericUpDown_bpmOverride.Name = "numericUpDown_bpmOverride";
            this.numericUpDown_bpmOverride.Size = new Size(80, 23);
            this.numericUpDown_bpmOverride.TabIndex = 20;
            this.numericUpDown_bpmOverride.Value = new decimal(new int[] { 178, 0, 0, 0 });
            // 
            // label_info_bpmOverride
            // 
            this.label_info_bpmOverride.AutoSize = true;
            this.label_info_bpmOverride.Location = new Point(218, 414);
            this.label_info_bpmOverride.Name = "label_info_bpmOverride";
            this.label_info_bpmOverride.Size = new Size(80, 15);
            this.label_info_bpmOverride.TabIndex = 21;
            this.label_info_bpmOverride.Text = "BPM Override";
            // 
            // label_info_swing
            // 
            this.label_info_swing.AutoSize = true;
            this.label_info_swing.Location = new Point(344, 414);
            this.label_info_swing.Name = "label_info_swing";
            this.label_info_swing.Size = new Size(39, 15);
            this.label_info_swing.TabIndex = 23;
            this.label_info_swing.Text = "Swing";
            // 
            // numericUpDown_swing
            // 
            this.numericUpDown_swing.DecimalPlaces = 5;
            this.numericUpDown_swing.Increment = new decimal(new int[] { 5, 0, 0, 196608 });
            this.numericUpDown_swing.Location = new Point(344, 432);
            this.numericUpDown_swing.Maximum = new decimal(new int[] { 75, 0, 0, 131072 });
            this.numericUpDown_swing.Name = "numericUpDown_swing";
            this.numericUpDown_swing.Size = new Size(65, 23);
            this.numericUpDown_swing.TabIndex = 22;
            // 
            // label_info_humanize
            // 
            this.label_info_humanize.AutoSize = true;
            this.label_info_humanize.Location = new Point(415, 414);
            this.label_info_humanize.Name = "label_info_humanize";
            this.label_info_humanize.Size = new Size(61, 15);
            this.label_info_humanize.TabIndex = 25;
            this.label_info_humanize.Text = "Humanize\r";
            // 
            // numericUpDown_humanize
            // 
            this.numericUpDown_humanize.DecimalPlaces = 1;
            this.numericUpDown_humanize.Location = new Point(415, 432);
            this.numericUpDown_humanize.Maximum = new decimal(new int[] { 220, 0, 0, 0 });
            this.numericUpDown_humanize.Name = "numericUpDown_humanize";
            this.numericUpDown_humanize.Size = new Size(65, 23);
            this.numericUpDown_humanize.TabIndex = 24;
            // 
            // numericUpDown_seed
            // 
            this.numericUpDown_seed.Location = new Point(486, 432);
            this.numericUpDown_seed.Maximum = new decimal(new int[] { 999999999, 0, 0, 0 });
            this.numericUpDown_seed.Name = "numericUpDown_seed";
            this.numericUpDown_seed.Size = new Size(80, 23);
            this.numericUpDown_seed.TabIndex = 26;
            this.numericUpDown_seed.Value = new decimal(new int[] { 123456789, 0, 0, 0 });
            this.numericUpDown_seed.ValueChanged += this.numericUpDown_seed_ValueChanged;
            // 
            // label_info_seed
            // 
            this.label_info_seed.AutoSize = true;
            this.label_info_seed.Location = new Point(486, 414);
            this.label_info_seed.Name = "label_info_seed";
            this.label_info_seed.Size = new Size(32, 15);
            this.label_info_seed.TabIndex = 27;
            this.label_info_seed.Text = "Seed";
            // 
            // label_info_targetPeak
            // 
            this.label_info_targetPeak.AutoSize = true;
            this.label_info_targetPeak.Location = new Point(572, 414);
            this.label_info_targetPeak.Name = "label_info_targetPeak";
            this.label_info_targetPeak.Size = new Size(68, 15);
            this.label_info_targetPeak.TabIndex = 29;
            this.label_info_targetPeak.Text = "Target Peak";
            // 
            // numericUpDown_targetPeak
            // 
            this.numericUpDown_targetPeak.DecimalPlaces = 5;
            this.numericUpDown_targetPeak.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this.numericUpDown_targetPeak.Location = new Point(572, 432);
            this.numericUpDown_targetPeak.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            this.numericUpDown_targetPeak.Minimum = new decimal(new int[] { 5, 0, 0, 131072 });
            this.numericUpDown_targetPeak.Name = "numericUpDown_targetPeak";
            this.numericUpDown_targetPeak.Size = new Size(74, 23);
            this.numericUpDown_targetPeak.TabIndex = 28;
            this.numericUpDown_targetPeak.Value = new decimal(new int[] { 95, 0, 0, 131072 });
            // 
            // label_info_mapping
            // 
            this.label_info_mapping.AutoSize = true;
            this.label_info_mapping.Location = new Point(12, 9);
            this.label_info_mapping.Name = "label_info_mapping";
            this.label_info_mapping.Size = new Size(193, 15);
            this.label_info_mapping.TabIndex = 30;
            this.label_info_mapping.Text = "Select a drum tag to map sample ...";
            // 
            // button_autoMap
            // 
            this.button_autoMap.BackColor = Color.FromArgb(  192,   255,   192);
            this.button_autoMap.Location = new Point(12, 369);
            this.button_autoMap.Name = "button_autoMap";
            this.button_autoMap.Size = new Size(75, 23);
            this.button_autoMap.TabIndex = 31;
            this.button_autoMap.Text = "Auto Map";
            this.button_autoMap.UseVisualStyleBackColor = false;
            this.button_autoMap.Click += this.button_autoMap_Click;
            // 
            // domainUpDown_smallestNote
            // 
            this.domainUpDown_smallestNote.Items.Add("1 /   1");
            this.domainUpDown_smallestNote.Items.Add("1 /   2");
            this.domainUpDown_smallestNote.Items.Add("1 /   4");
            this.domainUpDown_smallestNote.Items.Add("1 /   8");
            this.domainUpDown_smallestNote.Items.Add("1 /  16");
            this.domainUpDown_smallestNote.Items.Add("1 /  32");
            this.domainUpDown_smallestNote.Items.Add("1 /  64");
            this.domainUpDown_smallestNote.Items.Add("1 / 128");
            this.domainUpDown_smallestNote.Location = new Point(144, 476);
            this.domainUpDown_smallestNote.Name = "domainUpDown_smallestNote";
            this.domainUpDown_smallestNote.Size = new Size(68, 23);
            this.domainUpDown_smallestNote.TabIndex = 32;
            this.domainUpDown_smallestNote.Text = "1 /  16";
            // 
            // label_info_clickToPlay
            // 
            this.label_info_clickToPlay.AutoSize = true;
            this.label_info_clickToPlay.Location = new Point(218, 87);
            this.label_info_clickToPlay.Name = "label_info_clickToPlay";
            this.label_info_clickToPlay.Size = new Size(170, 15);
            this.label_info_clickToPlay.TabIndex = 33;
            this.label_info_clickToPlay.Text = "<-- Click on a sample to play it";
            // 
            // label_info_smallestNote
            // 
            this.label_info_smallestNote.AutoSize = true;
            this.label_info_smallestNote.Location = new Point(144, 443);
            this.label_info_smallestNote.Name = "label_info_smallestNote";
            this.label_info_smallestNote.Size = new Size(51, 30);
            this.label_info_smallestNote.TabIndex = 34;
            this.label_info_smallestNote.Text = "Smallest\r\nNote";
            // 
            // DrumsetDialog
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.WhiteSmoke;
            this.ClientSize = new Size(800, 511);
            this.Controls.Add(this.label_info_smallestNote);
            this.Controls.Add(this.label_info_clickToPlay);
            this.Controls.Add(this.domainUpDown_smallestNote);
            this.Controls.Add(this.button_autoMap);
            this.Controls.Add(this.label_info_mapping);
            this.Controls.Add(this.label_info_targetPeak);
            this.Controls.Add(this.numericUpDown_targetPeak);
            this.Controls.Add(this.label_info_seed);
            this.Controls.Add(this.numericUpDown_seed);
            this.Controls.Add(this.label_info_humanize);
            this.Controls.Add(this.numericUpDown_humanize);
            this.Controls.Add(this.label_info_swing);
            this.Controls.Add(this.numericUpDown_swing);
            this.Controls.Add(this.label_info_bpmOverride);
            this.Controls.Add(this.numericUpDown_bpmOverride);
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
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_bpmOverride).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_swing).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_humanize).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_seed).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_targetPeak).EndInit();
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
        private NumericUpDown numericUpDown_bpmOverride;
        private Label label_info_bpmOverride;
        private Label label_info_swing;
        private NumericUpDown numericUpDown_swing;
        private Label label_info_humanize;
        private NumericUpDown numericUpDown_humanize;
        private NumericUpDown numericUpDown_seed;
        private Label label_info_seed;
        private Label label_info_targetPeak;
        private NumericUpDown numericUpDown_targetPeak;
        private Label label_info_mapping;
        private Button button_autoMap;
        private DomainUpDown domainUpDown_smallestNote;
        private Label label_info_clickToPlay;
        private Label label_info_smallestNote;
    }
}