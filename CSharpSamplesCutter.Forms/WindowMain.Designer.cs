namespace CSharpSamplesCutter.Forms
{
    partial class WindowMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.listBox_audios = new ListBox();
			this.pictureBox_wave = new PictureBox();
			this.button_remove = new Button();
			this.button_reload = new Button();
			this.button_export = new Button();
			this.button_load = new Button();
			this.button_copy = new Button();
			this.textBox_timestamp = new TextBox();
			this.button_playback = new Button();
			this.listBox_log = new ListBox();
			this.label_audioName = new Label();
			this.button_autoCut = new Button();
			this.textBox_audioInfo = new TextBox();
			this.vScrollBar_volume = new VScrollBar();
			this.label_sampleAtCursor = new Label();
			this.label_sampleArea = new Label();
			this.button_selectionMode = new Button();
			this.label_info_copy = new Label();
			this.label_selectionMode = new Label();
			this.groupBox_view = new GroupBox();
			this.checkBox_sync = new CheckBox();
			this.label_undoSteps = new Label();
			this.label_info_skipTracks = new Label();
			this.numericUpDown_skipTracks = new NumericUpDown();
			this.panel_enableSamplesPerPixel = new Panel();
			this.numericUpDown_samplesPerPixel = new NumericUpDown();
			this.label_info_samplesPerPixel = new Label();
			this.hScrollBar_scroll = new HScrollBar();
			this.button_drumSet = new Button();
			((System.ComponentModel.ISupportInitialize) this.pictureBox_wave).BeginInit();
			this.groupBox_view.SuspendLayout();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_skipTracks).BeginInit();
			this.panel_enableSamplesPerPixel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_samplesPerPixel).BeginInit();
			this.SuspendLayout();
			// 
			// listBox_audios
			// 
			this.listBox_audios.FormattingEnabled = true;
			this.listBox_audios.ItemHeight = 15;
			this.listBox_audios.Location = new Point(532, 374);
			this.listBox_audios.Name = "listBox_audios";
			this.listBox_audios.Size = new Size(160, 139);
			this.listBox_audios.TabIndex = 0;
			// 
			// pictureBox_wave
			// 
			this.pictureBox_wave.BackColor = Color.White;
			this.pictureBox_wave.BorderStyle = BorderStyle.Fixed3D;
			this.pictureBox_wave.Location = new Point(12, 519);
			this.pictureBox_wave.Name = "pictureBox_wave";
			this.pictureBox_wave.Size = new Size(663, 118);
			this.pictureBox_wave.TabIndex = 1;
			this.pictureBox_wave.TabStop = false;
			// 
			// button_remove
			// 
			this.button_remove.BackColor = Color.FromArgb(  255,   192,   192);
			this.button_remove.Location = new Point(451, 490);
			this.button_remove.Name = "button_remove";
			this.button_remove.Size = new Size(75, 23);
			this.button_remove.TabIndex = 11;
			this.button_remove.Text = "Remove";
			this.button_remove.UseVisualStyleBackColor = false;
			this.button_remove.Click += this.button_remove_Click;
			// 
			// button_reload
			// 
			this.button_reload.BackColor = Color.FromArgb(  255,   224,   192);
			this.button_reload.Location = new Point(451, 461);
			this.button_reload.Name = "button_reload";
			this.button_reload.Size = new Size(75, 23);
			this.button_reload.TabIndex = 10;
			this.button_reload.Text = "Reload";
			this.button_reload.UseVisualStyleBackColor = false;
			this.button_reload.Click += this.button_reload_Click;
			// 
			// button_export
			// 
			this.button_export.BackColor = Color.FromArgb(  192,   255,   255);
			this.button_export.Location = new Point(451, 432);
			this.button_export.Name = "button_export";
			this.button_export.Size = new Size(75, 23);
			this.button_export.TabIndex = 9;
			this.button_export.Text = "Export";
			this.button_export.UseVisualStyleBackColor = false;
			this.button_export.Click += this.button_export_Click;
			// 
			// button_load
			// 
			this.button_load.BackColor = SystemColors.Info;
			this.button_load.Location = new Point(451, 374);
			this.button_load.Name = "button_load";
			this.button_load.Size = new Size(75, 23);
			this.button_load.TabIndex = 8;
			this.button_load.Text = "Load";
			this.button_load.UseVisualStyleBackColor = false;
			this.button_load.Click += this.button_load_Click;
			// 
			// button_copy
			// 
			this.button_copy.Location = new Point(503, 345);
			this.button_copy.Name = "button_copy";
			this.button_copy.Size = new Size(23, 23);
			this.button_copy.TabIndex = 12;
			this.button_copy.Text = "⿻";
			this.button_copy.UseVisualStyleBackColor = true;
			this.button_copy.Click += this.button_copy_Click;
			// 
			// textBox_timestamp
			// 
			this.textBox_timestamp.Location = new Point(612, 345);
			this.textBox_timestamp.Name = "textBox_timestamp";
			this.textBox_timestamp.PlaceholderText = "0:00:00.000";
			this.textBox_timestamp.Size = new Size(80, 23);
			this.textBox_timestamp.TabIndex = 15;
			// 
			// button_playback
			// 
			this.button_playback.Location = new Point(532, 345);
			this.button_playback.Name = "button_playback";
			this.button_playback.Size = new Size(23, 23);
			this.button_playback.TabIndex = 13;
			this.button_playback.Text = "▶";
			this.button_playback.UseVisualStyleBackColor = true;
			this.button_playback.Click += this.button_playback_Click;
			// 
			// listBox_log
			// 
			this.listBox_log.FormattingEnabled = true;
			this.listBox_log.HorizontalScrollbar = true;
			this.listBox_log.ItemHeight = 15;
			this.listBox_log.Location = new Point(451, 12);
			this.listBox_log.Name = "listBox_log";
			this.listBox_log.Size = new Size(241, 229);
			this.listBox_log.TabIndex = 16;
			// 
			// label_audioName
			// 
			this.label_audioName.AutoSize = true;
			this.label_audioName.Location = new Point(12, 501);
			this.label_audioName.Name = "label_audioName";
			this.label_audioName.Size = new Size(105, 15);
			this.label_audioName.TabIndex = 17;
			this.label_audioName.Text = "No audio selected.";
			// 
			// button_autoCut
			// 
			this.button_autoCut.BackColor = SystemColors.Info;
			this.button_autoCut.Location = new Point(451, 403);
			this.button_autoCut.Name = "button_autoCut";
			this.button_autoCut.Size = new Size(75, 23);
			this.button_autoCut.TabIndex = 18;
			this.button_autoCut.Text = "Auto Cut";
			this.button_autoCut.UseVisualStyleBackColor = false;
			this.button_autoCut.Click += this.button_autoCut_Click;
			// 
			// textBox_audioInfo
			// 
			this.textBox_audioInfo.BackColor = Color.White;
			this.textBox_audioInfo.Location = new Point(204, 12);
			this.textBox_audioInfo.Multiline = true;
			this.textBox_audioInfo.Name = "textBox_audioInfo";
			this.textBox_audioInfo.PlaceholderText = "No audio selected.";
			this.textBox_audioInfo.ReadOnly = true;
			this.textBox_audioInfo.Size = new Size(241, 229);
			this.textBox_audioInfo.TabIndex = 19;
			// 
			// vScrollBar_volume
			// 
			this.vScrollBar_volume.Location = new Point(678, 519);
			this.vScrollBar_volume.Maximum = 1000;
			this.vScrollBar_volume.Name = "vScrollBar_volume";
			this.vScrollBar_volume.Size = new Size(17, 118);
			this.vScrollBar_volume.TabIndex = 20;
			this.vScrollBar_volume.Scroll += this.vScrollBar_volume_Scroll;
			// 
			// label_sampleAtCursor
			// 
			this.label_sampleAtCursor.AutoSize = true;
			this.label_sampleAtCursor.Location = new Point(451, 657);
			this.label_sampleAtCursor.Name = "label_sampleAtCursor";
			this.label_sampleAtCursor.Size = new Size(108, 15);
			this.label_sampleAtCursor.TabIndex = 21;
			this.label_sampleAtCursor.Text = "Sample at Cursor: -";
			// 
			// label_sampleArea
			// 
			this.label_sampleArea.AutoSize = true;
			this.label_sampleArea.Location = new Point(12, 657);
			this.label_sampleArea.Name = "label_sampleArea";
			this.label_sampleArea.Size = new Size(201, 15);
			this.label_sampleArea.TabIndex = 22;
			this.label_sampleArea.Text = "No sample area available or selected.";
			// 
			// button_selectionMode
			// 
			this.button_selectionMode.Location = new Point(451, 323);
			this.button_selectionMode.Name = "button_selectionMode";
			this.button_selectionMode.Size = new Size(23, 23);
			this.button_selectionMode.TabIndex = 23;
			this.button_selectionMode.Text = "⛶";
			this.button_selectionMode.UseVisualStyleBackColor = true;
			this.button_selectionMode.Click += this.button_selectionMode_Click;
			// 
			// label_info_copy
			// 
			this.label_info_copy.AutoSize = true;
			this.label_info_copy.Location = new Point(451, 349);
			this.label_info_copy.Name = "label_info_copy";
			this.label_info_copy.Size = new Size(38, 15);
			this.label_info_copy.TabIndex = 24;
			this.label_info_copy.Text = "Copy:";
			// 
			// label_selectionMode
			// 
			this.label_selectionMode.AutoSize = true;
			this.label_selectionMode.Font = new Font("Segoe UI", 9F, FontStyle.Underline, GraphicsUnit.Point,  0);
			this.label_selectionMode.ForeColor = SystemColors.ControlText;
			this.label_selectionMode.Location = new Point(480, 327);
			this.label_selectionMode.Name = "label_selectionMode";
			this.label_selectionMode.Size = new Size(38, 15);
			this.label_selectionMode.TabIndex = 25;
			this.label_selectionMode.Text = "Select";
			// 
			// groupBox_view
			// 
			this.groupBox_view.BackColor = SystemColors.ControlLight;
			this.groupBox_view.Controls.Add(this.checkBox_sync);
			this.groupBox_view.Controls.Add(this.label_undoSteps);
			this.groupBox_view.Controls.Add(this.label_info_skipTracks);
			this.groupBox_view.Controls.Add(this.numericUpDown_skipTracks);
			this.groupBox_view.Controls.Add(this.panel_enableSamplesPerPixel);
			this.groupBox_view.Controls.Add(this.label_info_samplesPerPixel);
			this.groupBox_view.Location = new Point(532, 247);
			this.groupBox_view.Name = "groupBox_view";
			this.groupBox_view.Size = new Size(160, 92);
			this.groupBox_view.TabIndex = 26;
			this.groupBox_view.TabStop = false;
			this.groupBox_view.Text = "View Settings";
			// 
			// checkBox_sync
			// 
			this.checkBox_sync.AutoSize = true;
			this.checkBox_sync.Checked = true;
			this.checkBox_sync.CheckState = CheckState.Checked;
			this.checkBox_sync.Location = new Point(79, 22);
			this.checkBox_sync.Name = "checkBox_sync";
			this.checkBox_sync.Size = new Size(51, 19);
			this.checkBox_sync.TabIndex = 29;
			this.checkBox_sync.Text = "Sync";
			this.checkBox_sync.UseVisualStyleBackColor = true;
			// 
			// label_undoSteps
			// 
			this.label_undoSteps.AutoSize = true;
			this.label_undoSteps.Location = new Point(6, 19);
			this.label_undoSteps.Name = "label_undoSteps";
			this.label_undoSteps.Size = new Size(56, 15);
			this.label_undoSteps.TabIndex = 29;
			this.label_undoSteps.Text = "Undo's: 0";
			// 
			// label_info_skipTracks
			// 
			this.label_info_skipTracks.AutoSize = true;
			this.label_info_skipTracks.Location = new Point(6, 45);
			this.label_info_skipTracks.Name = "label_info_skipTracks";
			this.label_info_skipTracks.Size = new Size(65, 15);
			this.label_info_skipTracks.TabIndex = 29;
			this.label_info_skipTracks.Text = "Skip Tracks";
			// 
			// numericUpDown_skipTracks
			// 
			this.numericUpDown_skipTracks.Location = new Point(6, 63);
			this.numericUpDown_skipTracks.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
			this.numericUpDown_skipTracks.Name = "numericUpDown_skipTracks";
			this.numericUpDown_skipTracks.Size = new Size(67, 23);
			this.numericUpDown_skipTracks.TabIndex = 29;
			this.numericUpDown_skipTracks.ValueChanged += this.numericUpDown_skipTracks_ValueChanged;
			// 
			// panel_enableSamplesPerPixel
			// 
			this.panel_enableSamplesPerPixel.Controls.Add(this.numericUpDown_samplesPerPixel);
			this.panel_enableSamplesPerPixel.Location = new Point(80, 63);
			this.panel_enableSamplesPerPixel.Name = "panel_enableSamplesPerPixel";
			this.panel_enableSamplesPerPixel.Size = new Size(74, 23);
			this.panel_enableSamplesPerPixel.TabIndex = 27;
			// 
			// numericUpDown_samplesPerPixel
			// 
			this.numericUpDown_samplesPerPixel.Location = new Point(-1, 0);
			this.numericUpDown_samplesPerPixel.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
			this.numericUpDown_samplesPerPixel.Name = "numericUpDown_samplesPerPixel";
			this.numericUpDown_samplesPerPixel.Size = new Size(75, 23);
			this.numericUpDown_samplesPerPixel.TabIndex = 27;
			this.numericUpDown_samplesPerPixel.Value = new decimal(new int[] { 128, 0, 0, 0 });
			this.numericUpDown_samplesPerPixel.ValueChanged += this.numericUpDown_samplesPerPixel_ValueChanged;
			// 
			// label_info_samplesPerPixel
			// 
			this.label_info_samplesPerPixel.AutoSize = true;
			this.label_info_samplesPerPixel.Location = new Point(79, 45);
			this.label_info_samplesPerPixel.Name = "label_info_samplesPerPixel";
			this.label_info_samplesPerPixel.Size = new Size(74, 15);
			this.label_info_samplesPerPixel.TabIndex = 27;
			this.label_info_samplesPerPixel.Text = "Samples / px";
			// 
			// hScrollBar_scroll
			// 
			this.hScrollBar_scroll.Location = new Point(12, 640);
			this.hScrollBar_scroll.Name = "hScrollBar_scroll";
			this.hScrollBar_scroll.Size = new Size(663, 17);
			this.hScrollBar_scroll.TabIndex = 27;
			this.hScrollBar_scroll.Scroll += this.hScrollBar_scroll_Scroll;
			// 
			// button_drumSet
			// 
			this.button_drumSet.BackColor = SystemColors.Info;
			this.button_drumSet.Location = new Point(370, 403);
			this.button_drumSet.Name = "button_drumSet";
			this.button_drumSet.Size = new Size(75, 23);
			this.button_drumSet.TabIndex = 28;
			this.button_drumSet.Text = "Drum Set";
			this.button_drumSet.UseVisualStyleBackColor = false;
			this.button_drumSet.Click += this.button_drumSet_Click;
			// 
			// WindowMain
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.WhiteSmoke;
			this.ClientSize = new Size(704, 681);
			this.Controls.Add(this.button_drumSet);
			this.Controls.Add(this.hScrollBar_scroll);
			this.Controls.Add(this.groupBox_view);
			this.Controls.Add(this.label_selectionMode);
			this.Controls.Add(this.label_info_copy);
			this.Controls.Add(this.button_selectionMode);
			this.Controls.Add(this.label_sampleArea);
			this.Controls.Add(this.label_sampleAtCursor);
			this.Controls.Add(this.vScrollBar_volume);
			this.Controls.Add(this.textBox_audioInfo);
			this.Controls.Add(this.button_autoCut);
			this.Controls.Add(this.label_audioName);
			this.Controls.Add(this.listBox_log);
			this.Controls.Add(this.button_copy);
			this.Controls.Add(this.textBox_timestamp);
			this.Controls.Add(this.button_playback);
			this.Controls.Add(this.button_remove);
			this.Controls.Add(this.button_reload);
			this.Controls.Add(this.button_export);
			this.Controls.Add(this.button_load);
			this.Controls.Add(this.pictureBox_wave);
			this.Controls.Add(this.listBox_audios);
			this.MaximizeBox = false;
			this.MaximumSize = new Size(720, 720);
			this.MinimumSize = new Size(720, 720);
			this.Name = "WindowMain";
			this.Text = "CSharpSamplesCutter (Forms UI)";
			((System.ComponentModel.ISupportInitialize) this.pictureBox_wave).EndInit();
			this.groupBox_view.ResumeLayout(false);
			this.groupBox_view.PerformLayout();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_skipTracks).EndInit();
			this.panel_enableSamplesPerPixel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_samplesPerPixel).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private ListBox listBox_audios;
		private PictureBox pictureBox_wave;
		private Button button_remove;
		private Button button_reload;
		private Button button_export;
		private Button button_load;
		private Button button_copy;
		private TextBox textBox_timestamp;
		private Button button_playback;
		private ListBox listBox_log;
		private Label label_audioName;
		private Button button_autoCut;
		private TextBox textBox_audioInfo;
		private VScrollBar vScrollBar_volume;
		private Label label_sampleAtCursor;
		private Label label_sampleArea;
		private Button button_selectionMode;
		private Label label_info_copy;
		private Label label_selectionMode;
		private GroupBox groupBox_view;
		private Label label_info_samplesPerPixel;
		private NumericUpDown numericUpDown_samplesPerPixel;
		private Panel panel_enableSamplesPerPixel;
		private HScrollBar hScrollBar_scroll;
		private Button button_drumSet;
		private Label label_info_skipTracks;
		private NumericUpDown numericUpDown_skipTracks;
		private Label label_undoSteps;
		private CheckBox checkBox_sync;
	}
}
