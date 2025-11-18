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
            this.checkBox_solo = new CheckBox();
            this.checkBox_sync = new CheckBox();
            this.label_info_skipTracks = new Label();
            this.numericUpDown_skipTracks = new NumericUpDown();
            this.label_undoSteps = new Label();
            this.panel_enableSamplesPerPixel = new Panel();
            this.numericUpDown_samplesPerPixel = new NumericUpDown();
            this.label_info_samplesPerPixel = new Label();
            this.hScrollBar_scroll = new HScrollBar();
            this.button_drumSet = new Button();
            this.textBox_trackMetrics = new TextBox();
            this.groupBox1 = new GroupBox();
            this.button_colorSelection = new Button();
            this.label_info_caretPosition = new Label();
            this.hScrollBar_caretPosition = new HScrollBar();
            this.checkBox_timeMarkers = new CheckBox();
            this.numericUpDown_timeMarkers = new NumericUpDown();
            this.label_info_caretWidth = new Label();
            this.numericUpDown_caretWidth = new NumericUpDown();
            this.checkBox_scrollLog = new CheckBox();
            this.label_info_frameRate = new Label();
            this.numericUpDown_frameRate = new NumericUpDown();
            this.button_colorCaret = new Button();
            this.button_strobe = new Button();
            this.numericUpDown_hue = new NumericUpDown();
            this.checkBox_hue = new CheckBox();
            this.checkBox_smoothen = new CheckBox();
            this.checkBox_drawEachChannel = new CheckBox();
            this.label_info_colors = new Label();
            this.button_colorBack = new Button();
            this.button_colorWave = new Button();
            this.button_pause = new Button();
            this.label_volume = new Label();
            this.button_infoHotkeys = new Button();
            this.listBox_reserve = new ListBox();
            this.button_move = new Button();
            this.listBox_audios = new ListBox();
            this.groupBox_audioScanner = new GroupBox();
            this.label_info_multiplierSymbol = new Label();
            this.label_info_scanRange = new Label();
            this.numericUpDown_scanWidth = new NumericUpDown();
            this.label_info_scanWidth = new Label();
            this.numericUpDown_lookingRange = new NumericUpDown();
            this.button_keyScan = new Button();
            this.textBox_scannedKey = new TextBox();
            this.button_timingScan = new Button();
            this.button_bpmScan = new Button();
            this.textBox_scannedTiming = new TextBox();
            this.textBox_scannedBpm = new TextBox();
            this.groupBox_basicProcessing = new GroupBox();
            this.checkBox_autoParameters = new CheckBox();
            this.checkBox_optionalParameters = new CheckBox();
            this.panel_basicProcessingParameters = new Panel();
            this.button_basicProcessingGo = new Button();
            this.comboBox_basicProcessing = new ComboBox();
            this.button_loop = new Button();
            this.groupBox_processingV2 = new GroupBox();
            this.button_advancedProcessingGo = new Button();
            this.comboBox_advancedProcessing = new ComboBox();
            ((System.ComponentModel.ISupportInitialize) this.pictureBox_wave).BeginInit();
            this.groupBox_view.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_skipTracks).BeginInit();
            this.panel_enableSamplesPerPixel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_samplesPerPixel).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_timeMarkers).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_caretWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_frameRate).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_hue).BeginInit();
            this.groupBox_audioScanner.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_scanWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_lookingRange).BeginInit();
            this.groupBox_basicProcessing.SuspendLayout();
            this.groupBox_processingV2.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox_wave
            // 
            this.pictureBox_wave.BackColor = Color.White;
            this.pictureBox_wave.BorderStyle = BorderStyle.Fixed3D;
            this.pictureBox_wave.Location = new Point(12, 637);
            this.pictureBox_wave.Name = "pictureBox_wave";
            this.pictureBox_wave.Size = new Size(1383, 140);
            this.pictureBox_wave.TabIndex = 1;
            this.pictureBox_wave.TabStop = false;
            // 
            // button_remove
            // 
            this.button_remove.BackColor = Color.FromArgb(  255,   192,   192);
            this.button_remove.Location = new Point(1096, 608);
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
            this.button_reload.Location = new Point(1096, 579);
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
            this.button_export.Location = new Point(1096, 506);
            this.button_export.Name = "button_export";
            this.button_export.Size = new Size(75, 23);
            this.button_export.TabIndex = 9;
            this.button_export.Text = "Export";
            this.button_export.UseVisualStyleBackColor = false;
            this.button_export.Click += this.button_export_Click;
            // 
            // button_load
            // 
            this.button_load.BackColor = Color.FromArgb(  255,   255,   192);
            this.button_load.Location = new Point(1096, 247);
            this.button_load.Name = "button_load";
            this.button_load.Size = new Size(75, 23);
            this.button_load.TabIndex = 8;
            this.button_load.Text = "Load";
            this.button_load.UseVisualStyleBackColor = false;
            this.button_load.Click += this.button_load_Click;
            // 
            // button_copy
            // 
            this.button_copy.Location = new Point(48, 44);
            this.button_copy.Margin = new Padding(2, 3, 3, 3);
            this.button_copy.Name = "button_copy";
            this.button_copy.Size = new Size(23, 23);
            this.button_copy.TabIndex = 12;
            this.button_copy.Text = "⿻";
            this.button_copy.UseVisualStyleBackColor = true;
            this.button_copy.Click += this.button_copy_Click;
            // 
            // textBox_timestamp
            // 
            this.textBox_timestamp.Location = new Point(1332, 373);
            this.textBox_timestamp.Name = "textBox_timestamp";
            this.textBox_timestamp.PlaceholderText = "0:00:00.000";
            this.textBox_timestamp.Size = new Size(80, 23);
            this.textBox_timestamp.TabIndex = 15;
            // 
            // button_playback
            // 
            this.button_playback.Location = new Point(1174, 373);
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
            this.listBox_log.Location = new Point(1096, 12);
            this.listBox_log.Name = "listBox_log";
            this.listBox_log.Size = new Size(316, 229);
            this.listBox_log.TabIndex = 16;
            // 
            // label_audioName
            // 
            this.label_audioName.AutoSize = true;
            this.label_audioName.Location = new Point(12, 619);
            this.label_audioName.Name = "label_audioName";
            this.label_audioName.Size = new Size(105, 15);
            this.label_audioName.TabIndex = 17;
            this.label_audioName.Text = "No audio selected.";
            // 
            // button_autoCut
            // 
            this.button_autoCut.BackColor = SystemColors.Info;
            this.button_autoCut.Location = new Point(1096, 315);
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
            this.textBox_audioInfo.Location = new Point(849, 12);
            this.textBox_audioInfo.Multiline = true;
            this.textBox_audioInfo.Name = "textBox_audioInfo";
            this.textBox_audioInfo.PlaceholderText = "No audio selected.";
            this.textBox_audioInfo.ReadOnly = true;
            this.textBox_audioInfo.ScrollBars = ScrollBars.Vertical;
            this.textBox_audioInfo.Size = new Size(241, 110);
            this.textBox_audioInfo.TabIndex = 19;
            // 
            // vScrollBar_volume
            // 
            this.vScrollBar_volume.Location = new Point(1398, 637);
            this.vScrollBar_volume.Maximum = 1000;
            this.vScrollBar_volume.Name = "vScrollBar_volume";
            this.vScrollBar_volume.Size = new Size(17, 140);
            this.vScrollBar_volume.TabIndex = 20;
            this.vScrollBar_volume.Value = 200;
            this.vScrollBar_volume.Scroll += this.vScrollBar_volume_Scroll;
            // 
            // label_sampleAtCursor
            // 
            this.label_sampleAtCursor.AutoSize = true;
            this.label_sampleAtCursor.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.label_sampleAtCursor.Location = new Point(1027, 799);
            this.label_sampleAtCursor.Name = "label_sampleAtCursor";
            this.label_sampleAtCursor.Size = new Size(104, 13);
            this.label_sampleAtCursor.TabIndex = 21;
            this.label_sampleAtCursor.Text = "Sample at Cursor: -";
            // 
            // label_sampleArea
            // 
            this.label_sampleArea.AutoSize = true;
            this.label_sampleArea.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.label_sampleArea.Location = new Point(12, 799);
            this.label_sampleArea.Name = "label_sampleArea";
            this.label_sampleArea.Size = new Size(196, 13);
            this.label_sampleArea.TabIndex = 22;
            this.label_sampleArea.Text = "No sample area available or selected.";
            // 
            // button_selectionMode
            // 
            this.button_selectionMode.Location = new Point(6, 22);
            this.button_selectionMode.Margin = new Padding(3, 3, 2, 3);
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
            this.label_info_copy.Location = new Point(7, 48);
            this.label_info_copy.Margin = new Padding(3, 0, 1, 0);
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
            this.label_selectionMode.Location = new Point(32, 26);
            this.label_selectionMode.Margin = new Padding(1, 0, 3, 0);
            this.label_selectionMode.Name = "label_selectionMode";
            this.label_selectionMode.Size = new Size(38, 15);
            this.label_selectionMode.TabIndex = 25;
            this.label_selectionMode.Text = "Select";
            // 
            // groupBox_view
            // 
            this.groupBox_view.BackColor = SystemColors.ControlLight;
            this.groupBox_view.Controls.Add(this.checkBox_solo);
            this.groupBox_view.Controls.Add(this.checkBox_sync);
            this.groupBox_view.Controls.Add(this.label_info_skipTracks);
            this.groupBox_view.Controls.Add(this.numericUpDown_skipTracks);
            this.groupBox_view.Controls.Add(this.label_undoSteps);
            this.groupBox_view.Controls.Add(this.label_info_copy);
            this.groupBox_view.Controls.Add(this.label_selectionMode);
            this.groupBox_view.Controls.Add(this.panel_enableSamplesPerPixel);
            this.groupBox_view.Controls.Add(this.label_info_samplesPerPixel);
            this.groupBox_view.Controls.Add(this.button_selectionMode);
            this.groupBox_view.Controls.Add(this.button_copy);
            this.groupBox_view.Location = new Point(1177, 247);
            this.groupBox_view.Name = "groupBox_view";
            this.groupBox_view.Size = new Size(235, 120);
            this.groupBox_view.TabIndex = 26;
            this.groupBox_view.TabStop = false;
            this.groupBox_view.Text = "View Settings";
            // 
            // checkBox_solo
            // 
            this.checkBox_solo.AutoSize = true;
            this.checkBox_solo.Checked = true;
            this.checkBox_solo.CheckState = CheckState.Checked;
            this.checkBox_solo.Location = new Point(179, 71);
            this.checkBox_solo.Margin = new Padding(2);
            this.checkBox_solo.Name = "checkBox_solo";
            this.checkBox_solo.Size = new Size(49, 19);
            this.checkBox_solo.TabIndex = 31;
            this.checkBox_solo.Text = "Solo";
            this.checkBox_solo.UseVisualStyleBackColor = true;
            this.checkBox_solo.CheckedChanged += this.checkBox_solo_CheckedChanged;
            // 
            // checkBox_sync
            // 
            this.checkBox_sync.AutoSize = true;
            this.checkBox_sync.Checked = true;
            this.checkBox_sync.CheckState = CheckState.Checked;
            this.checkBox_sync.Location = new Point(179, 96);
            this.checkBox_sync.Margin = new Padding(2);
            this.checkBox_sync.Name = "checkBox_sync";
            this.checkBox_sync.Size = new Size(51, 19);
            this.checkBox_sync.TabIndex = 29;
            this.checkBox_sync.Text = "Sync";
            this.checkBox_sync.UseVisualStyleBackColor = true;
            // 
            // label_info_skipTracks
            // 
            this.label_info_skipTracks.AutoSize = true;
            this.label_info_skipTracks.Location = new Point(6, 73);
            this.label_info_skipTracks.Name = "label_info_skipTracks";
            this.label_info_skipTracks.Size = new Size(65, 15);
            this.label_info_skipTracks.TabIndex = 29;
            this.label_info_skipTracks.Text = "Skip Tracks";
            // 
            // numericUpDown_skipTracks
            // 
            this.numericUpDown_skipTracks.Location = new Point(6, 91);
            this.numericUpDown_skipTracks.Maximum = new decimal(new int[] { 0, 0, 0, 0 });
            this.numericUpDown_skipTracks.Name = "numericUpDown_skipTracks";
            this.numericUpDown_skipTracks.Size = new Size(67, 23);
            this.numericUpDown_skipTracks.TabIndex = 29;
            this.numericUpDown_skipTracks.ValueChanged += this.numericUpDown_skipTracks_ValueChanged;
            // 
            // label_undoSteps
            // 
            this.label_undoSteps.AutoSize = true;
            this.label_undoSteps.Location = new Point(173, 19);
            this.label_undoSteps.Name = "label_undoSteps";
            this.label_undoSteps.Size = new Size(56, 15);
            this.label_undoSteps.TabIndex = 29;
            this.label_undoSteps.Text = "Undo's: 0";
            // 
            // panel_enableSamplesPerPixel
            // 
            this.panel_enableSamplesPerPixel.Controls.Add(this.numericUpDown_samplesPerPixel);
            this.panel_enableSamplesPerPixel.Location = new Point(80, 91);
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
            this.label_info_samplesPerPixel.Location = new Point(79, 73);
            this.label_info_samplesPerPixel.Name = "label_info_samplesPerPixel";
            this.label_info_samplesPerPixel.Size = new Size(74, 15);
            this.label_info_samplesPerPixel.TabIndex = 27;
            this.label_info_samplesPerPixel.Text = "Samples / px";
            // 
            // hScrollBar_scroll
            // 
            this.hScrollBar_scroll.Location = new Point(12, 780);
            this.hScrollBar_scroll.Name = "hScrollBar_scroll";
            this.hScrollBar_scroll.Size = new Size(1403, 17);
            this.hScrollBar_scroll.TabIndex = 27;
            this.hScrollBar_scroll.Scroll += this.hScrollBar_scroll_Scroll;
            // 
            // button_drumSet
            // 
            this.button_drumSet.BackColor = SystemColors.Info;
            this.button_drumSet.Location = new Point(1096, 344);
            this.button_drumSet.Name = "button_drumSet";
            this.button_drumSet.Size = new Size(75, 23);
            this.button_drumSet.TabIndex = 28;
            this.button_drumSet.Text = "Drum Set";
            this.button_drumSet.UseVisualStyleBackColor = false;
            this.button_drumSet.Click += this.button_drumSet_Click;
            // 
            // textBox_trackMetrics
            // 
            this.textBox_trackMetrics.BackColor = Color.White;
            this.textBox_trackMetrics.Location = new Point(849, 128);
            this.textBox_trackMetrics.Multiline = true;
            this.textBox_trackMetrics.Name = "textBox_trackMetrics";
            this.textBox_trackMetrics.PlaceholderText = "No audio selected.";
            this.textBox_trackMetrics.ReadOnly = true;
            this.textBox_trackMetrics.ScrollBars = ScrollBars.Vertical;
            this.textBox_trackMetrics.Size = new Size(241, 113);
            this.textBox_trackMetrics.TabIndex = 29;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = SystemColors.ControlLight;
            this.groupBox1.Controls.Add(this.button_colorSelection);
            this.groupBox1.Controls.Add(this.label_info_caretPosition);
            this.groupBox1.Controls.Add(this.hScrollBar_caretPosition);
            this.groupBox1.Controls.Add(this.checkBox_timeMarkers);
            this.groupBox1.Controls.Add(this.numericUpDown_timeMarkers);
            this.groupBox1.Controls.Add(this.label_info_caretWidth);
            this.groupBox1.Controls.Add(this.numericUpDown_caretWidth);
            this.groupBox1.Controls.Add(this.checkBox_scrollLog);
            this.groupBox1.Controls.Add(this.label_info_frameRate);
            this.groupBox1.Controls.Add(this.numericUpDown_frameRate);
            this.groupBox1.Controls.Add(this.button_colorCaret);
            this.groupBox1.Controls.Add(this.button_strobe);
            this.groupBox1.Controls.Add(this.numericUpDown_hue);
            this.groupBox1.Controls.Add(this.checkBox_hue);
            this.groupBox1.Controls.Add(this.checkBox_smoothen);
            this.groupBox1.Controls.Add(this.checkBox_drawEachChannel);
            this.groupBox1.Controls.Add(this.label_info_colors);
            this.groupBox1.Controls.Add(this.button_colorBack);
            this.groupBox1.Controls.Add(this.button_colorWave);
            this.groupBox1.Location = new Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new Size(180, 334);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "View Settings";
            // 
            // button_colorSelection
            // 
            this.button_colorSelection.BackColor = SystemColors.AppWorkspace;
            this.button_colorSelection.Font = new Font("Bahnschrift SemiLight SemiConde", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.button_colorSelection.Location = new Point(124, 44);
            this.button_colorSelection.Margin = new Padding(1, 3, 1, 1);
            this.button_colorSelection.Name = "button_colorSelection";
            this.button_colorSelection.Size = new Size(38, 23);
            this.button_colorSelection.TabIndex = 37;
            this.button_colorSelection.Text = "Area";
            this.button_colorSelection.UseVisualStyleBackColor = false;
            this.button_colorSelection.Click += this.button_colorSelection_Click;
            // 
            // label_info_caretPosition
            // 
            this.label_info_caretPosition.AutoSize = true;
            this.label_info_caretPosition.Location = new Point(3, 297);
            this.label_info_caretPosition.Margin = new Padding(3, 0, 3, 2);
            this.label_info_caretPosition.Name = "label_info_caretPosition";
            this.label_info_caretPosition.Size = new Size(112, 15);
            this.label_info_caretPosition.TabIndex = 31;
            this.label_info_caretPosition.Text = "Caret Position: 0.0%";
            // 
            // hScrollBar_caretPosition
            // 
            this.hScrollBar_caretPosition.Location = new Point(3, 314);
            this.hScrollBar_caretPosition.Maximum = 1000;
            this.hScrollBar_caretPosition.Name = "hScrollBar_caretPosition";
            this.hScrollBar_caretPosition.Size = new Size(174, 17);
            this.hScrollBar_caretPosition.TabIndex = 31;
            this.hScrollBar_caretPosition.Value = 500;
            this.hScrollBar_caretPosition.Scroll += this.hScrollBar_caretPosition_Scroll;
            // 
            // checkBox_timeMarkers
            // 
            this.checkBox_timeMarkers.AutoSize = true;
            this.checkBox_timeMarkers.Location = new Point(6, 155);
            this.checkBox_timeMarkers.Name = "checkBox_timeMarkers";
            this.checkBox_timeMarkers.Size = new Size(101, 19);
            this.checkBox_timeMarkers.TabIndex = 30;
            this.checkBox_timeMarkers.Text = "Time Markers:";
            this.checkBox_timeMarkers.UseVisualStyleBackColor = true;
            this.checkBox_timeMarkers.CheckedChanged += this.checkBox_timeMarkers_CheckedChanged;
            // 
            // numericUpDown_timeMarkers
            // 
            this.numericUpDown_timeMarkers.DecimalPlaces = 3;
            this.numericUpDown_timeMarkers.Increment = new decimal(new int[] { 125, 0, 0, 196608 });
            this.numericUpDown_timeMarkers.Location = new Point(113, 154);
            this.numericUpDown_timeMarkers.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
            this.numericUpDown_timeMarkers.Minimum = new decimal(new int[] { 5, 0, 0, 131072 });
            this.numericUpDown_timeMarkers.Name = "numericUpDown_timeMarkers";
            this.numericUpDown_timeMarkers.Size = new Size(61, 23);
            this.numericUpDown_timeMarkers.TabIndex = 29;
            this.numericUpDown_timeMarkers.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // label_info_caretWidth
            // 
            this.label_info_caretWidth.AutoSize = true;
            this.label_info_caretWidth.Location = new Point(6, 185);
            this.label_info_caretWidth.Name = "label_info_caretWidth";
            this.label_info_caretWidth.Size = new Size(73, 15);
            this.label_info_caretWidth.TabIndex = 27;
            this.label_info_caretWidth.Text = "Caret Width:";
            // 
            // numericUpDown_caretWidth
            // 
            this.numericUpDown_caretWidth.Location = new Point(124, 183);
            this.numericUpDown_caretWidth.Maximum = new decimal(new int[] { 48, 0, 0, 0 });
            this.numericUpDown_caretWidth.Name = "numericUpDown_caretWidth";
            this.numericUpDown_caretWidth.Size = new Size(50, 23);
            this.numericUpDown_caretWidth.TabIndex = 28;
            this.numericUpDown_caretWidth.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // checkBox_scrollLog
            // 
            this.checkBox_scrollLog.AutoSize = true;
            this.checkBox_scrollLog.Checked = true;
            this.checkBox_scrollLog.CheckState = CheckState.Checked;
            this.checkBox_scrollLog.Location = new Point(6, 270);
            this.checkBox_scrollLog.Name = "checkBox_scrollLog";
            this.checkBox_scrollLog.Size = new Size(144, 19);
            this.checkBox_scrollLog.TabIndex = 25;
            this.checkBox_scrollLog.Text = "Auto scroll Log Entries";
            this.checkBox_scrollLog.UseVisualStyleBackColor = true;
            // 
            // label_info_frameRate
            // 
            this.label_info_frameRate.AutoSize = true;
            this.label_info_frameRate.Location = new Point(6, 243);
            this.label_info_frameRate.Name = "label_info_frameRate";
            this.label_info_frameRate.Size = new Size(69, 15);
            this.label_info_frameRate.TabIndex = 22;
            this.label_info_frameRate.Text = "Frame Rate:";
            // 
            // numericUpDown_frameRate
            // 
            this.numericUpDown_frameRate.Location = new Point(124, 241);
            this.numericUpDown_frameRate.Maximum = new decimal(new int[] { 144, 0, 0, 0 });
            this.numericUpDown_frameRate.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_frameRate.Name = "numericUpDown_frameRate";
            this.numericUpDown_frameRate.Size = new Size(50, 23);
            this.numericUpDown_frameRate.TabIndex = 23;
            this.numericUpDown_frameRate.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // button_colorCaret
            // 
            this.button_colorCaret.BackColor = Color.IndianRed;
            this.button_colorCaret.Font = new Font("Bahnschrift SemiLight SemiConde", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.button_colorCaret.ForeColor = Color.Black;
            this.button_colorCaret.Location = new Point(4, 44);
            this.button_colorCaret.Margin = new Padding(1, 3, 1, 1);
            this.button_colorCaret.Name = "button_colorCaret";
            this.button_colorCaret.Size = new Size(38, 23);
            this.button_colorCaret.TabIndex = 18;
            this.button_colorCaret.Text = "Caret";
            this.button_colorCaret.UseVisualStyleBackColor = false;
            this.button_colorCaret.Click += this.button_colorCaret_Click;
            // 
            // button_strobe
            // 
            this.button_strobe.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.button_strobe.ForeColor = Color.Black;
            this.button_strobe.Location = new Point(151, 125);
            this.button_strobe.Name = "button_strobe";
            this.button_strobe.Size = new Size(23, 23);
            this.button_strobe.TabIndex = 14;
            this.button_strobe.Text = "🕱";
            this.button_strobe.UseVisualStyleBackColor = true;
            this.button_strobe.Click += this.button_strobe_Click;
            // 
            // numericUpDown_hue
            // 
            this.numericUpDown_hue.DecimalPlaces = 3;
            this.numericUpDown_hue.Increment = new decimal(new int[] { 125, 0, 0, 196608 });
            this.numericUpDown_hue.Location = new Point(75, 126);
            this.numericUpDown_hue.Maximum = new decimal(new int[] { 720, 0, 0, 0 });
            this.numericUpDown_hue.Name = "numericUpDown_hue";
            this.numericUpDown_hue.Size = new Size(70, 23);
            this.numericUpDown_hue.TabIndex = 14;
            this.numericUpDown_hue.Value = new decimal(new int[] { 175, 0, 0, 131072 });
            this.numericUpDown_hue.ValueChanged += this.numericUpDown_hue_ValueChanged;
            // 
            // checkBox_hue
            // 
            this.checkBox_hue.AutoSize = true;
            this.checkBox_hue.Location = new Point(6, 128);
            this.checkBox_hue.Name = "checkBox_hue";
            this.checkBox_hue.Size = new Size(48, 19);
            this.checkBox_hue.TabIndex = 14;
            this.checkBox_hue.Text = "Hue";
            this.checkBox_hue.UseVisualStyleBackColor = true;
            this.checkBox_hue.CheckedChanged += this.checkBox_hue_CheckedChanged;
            // 
            // checkBox_smoothen
            // 
            this.checkBox_smoothen.AutoSize = true;
            this.checkBox_smoothen.Location = new Point(6, 101);
            this.checkBox_smoothen.Name = "checkBox_smoothen";
            this.checkBox_smoothen.Size = new Size(124, 19);
            this.checkBox_smoothen.TabIndex = 14;
            this.checkBox_smoothen.Text = "Smooth waveform";
            this.checkBox_smoothen.UseVisualStyleBackColor = true;
            // 
            // checkBox_drawEachChannel
            // 
            this.checkBox_drawEachChannel.AutoSize = true;
            this.checkBox_drawEachChannel.Location = new Point(6, 76);
            this.checkBox_drawEachChannel.Name = "checkBox_drawEachChannel";
            this.checkBox_drawEachChannel.Size = new Size(128, 19);
            this.checkBox_drawEachChannel.TabIndex = 12;
            this.checkBox_drawEachChannel.Text = "Draw each Channel";
            this.checkBox_drawEachChannel.UseVisualStyleBackColor = true;
            // 
            // label_info_colors
            // 
            this.label_info_colors.AutoSize = true;
            this.label_info_colors.Location = new Point(6, 26);
            this.label_info_colors.Name = "label_info_colors";
            this.label_info_colors.Size = new Size(44, 15);
            this.label_info_colors.TabIndex = 9;
            this.label_info_colors.Text = "Colors:";
            // 
            // button_colorBack
            // 
            this.button_colorBack.BackColor = Color.White;
            this.button_colorBack.Font = new Font("Bahnschrift SemiLight SemiConde", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.button_colorBack.Location = new Point(84, 44);
            this.button_colorBack.Margin = new Padding(1, 3, 1, 1);
            this.button_colorBack.Name = "button_colorBack";
            this.button_colorBack.Size = new Size(38, 23);
            this.button_colorBack.TabIndex = 10;
            this.button_colorBack.Text = "Back";
            this.button_colorBack.UseVisualStyleBackColor = false;
            this.button_colorBack.Click += this.button_colorBack_Click;
            // 
            // button_colorWave
            // 
            this.button_colorWave.BackColor = SystemColors.ActiveCaption;
            this.button_colorWave.Font = new Font("Bahnschrift SemiLight SemiConde", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.button_colorWave.Location = new Point(44, 44);
            this.button_colorWave.Margin = new Padding(1, 3, 1, 1);
            this.button_colorWave.Name = "button_colorWave";
            this.button_colorWave.Size = new Size(38, 23);
            this.button_colorWave.TabIndex = 9;
            this.button_colorWave.Text = "Wave";
            this.button_colorWave.UseVisualStyleBackColor = false;
            this.button_colorWave.Click += this.button_colorWave_Click;
            // 
            // button_pause
            // 
            this.button_pause.Font = new Font("Bahnschrift", 9F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.button_pause.Location = new Point(1203, 373);
            this.button_pause.Name = "button_pause";
            this.button_pause.Size = new Size(23, 23);
            this.button_pause.TabIndex = 31;
            this.button_pause.Text = "||";
            this.button_pause.UseVisualStyleBackColor = true;
            this.button_pause.Click += this.button_pause_Click;
            // 
            // label_volume
            // 
            this.label_volume.AutoSize = true;
            this.label_volume.Font = new Font("Bahnschrift Condensed", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.label_volume.Location = new Point(1369, 797);
            this.label_volume.Name = "label_volume";
            this.label_volume.Size = new Size(46, 13);
            this.label_volume.TabIndex = 32;
            this.label_volume.Text = "Vol 100.0%";
            // 
            // button_infoHotkeys
            // 
            this.button_infoHotkeys.Font = new Font("Bahnschrift", 8.25F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.button_infoHotkeys.Location = new Point(12, 352);
            this.button_infoHotkeys.Name = "button_infoHotkeys";
            this.button_infoHotkeys.Size = new Size(60, 22);
            this.button_infoHotkeys.TabIndex = 33;
            this.button_infoHotkeys.Text = "Hotkeys?";
            this.button_infoHotkeys.UseVisualStyleBackColor = true;
            this.button_infoHotkeys.Click += this.button_infoHotkeys_Click;
            // 
            // listBox_reserve
            // 
            this.listBox_reserve.FormattingEnabled = true;
            this.listBox_reserve.ItemHeight = 15;
            this.listBox_reserve.Location = new Point(849, 417);
            this.listBox_reserve.Name = "listBox_reserve";
            this.listBox_reserve.Size = new Size(241, 214);
            this.listBox_reserve.TabIndex = 34;
            // 
            // button_move
            // 
            this.button_move.BackColor = Color.FromArgb(  192,   192,   255);
            this.button_move.Location = new Point(1096, 477);
            this.button_move.Name = "button_move";
            this.button_move.Size = new Size(75, 23);
            this.button_move.TabIndex = 35;
            this.button_move.Text = " -Move -";
            this.button_move.UseVisualStyleBackColor = false;
            this.button_move.Click += this.button_move_Click;
            // 
            // listBox_audios
            // 
            this.listBox_audios.FormattingEnabled = true;
            this.listBox_audios.ItemHeight = 15;
            this.listBox_audios.Location = new Point(1177, 402);
            this.listBox_audios.Name = "listBox_audios";
            this.listBox_audios.Size = new Size(235, 229);
            this.listBox_audios.TabIndex = 36;
            // 
            // groupBox_audioScanner
            // 
            this.groupBox_audioScanner.BackColor = SystemColors.ControlLight;
            this.groupBox_audioScanner.Controls.Add(this.label_info_multiplierSymbol);
            this.groupBox_audioScanner.Controls.Add(this.label_info_scanRange);
            this.groupBox_audioScanner.Controls.Add(this.numericUpDown_scanWidth);
            this.groupBox_audioScanner.Controls.Add(this.label_info_scanWidth);
            this.groupBox_audioScanner.Controls.Add(this.numericUpDown_lookingRange);
            this.groupBox_audioScanner.Controls.Add(this.button_keyScan);
            this.groupBox_audioScanner.Controls.Add(this.textBox_scannedKey);
            this.groupBox_audioScanner.Controls.Add(this.button_timingScan);
            this.groupBox_audioScanner.Controls.Add(this.button_bpmScan);
            this.groupBox_audioScanner.Controls.Add(this.textBox_scannedTiming);
            this.groupBox_audioScanner.Controls.Add(this.textBox_scannedBpm);
            this.groupBox_audioScanner.Location = new Point(849, 247);
            this.groupBox_audioScanner.Name = "groupBox_audioScanner";
            this.groupBox_audioScanner.Size = new Size(160, 164);
            this.groupBox_audioScanner.TabIndex = 37;
            this.groupBox_audioScanner.TabStop = false;
            this.groupBox_audioScanner.Text = "Audio Scanner";
            // 
            // label_info_multiplierSymbol
            // 
            this.label_info_multiplierSymbol.AutoSize = true;
            this.label_info_multiplierSymbol.Location = new Point(73, 39);
            this.label_info_multiplierSymbol.Name = "label_info_multiplierSymbol";
            this.label_info_multiplierSymbol.Size = new Size(15, 15);
            this.label_info_multiplierSymbol.TabIndex = 38;
            this.label_info_multiplierSymbol.Text = "x ";
            // 
            // label_info_scanRange
            // 
            this.label_info_scanRange.AutoSize = true;
            this.label_info_scanRange.Location = new Point(6, 19);
            this.label_info_scanRange.Name = "label_info_scanRange";
            this.label_info_scanRange.Size = new Size(40, 15);
            this.label_info_scanRange.TabIndex = 44;
            this.label_info_scanRange.Text = "Range";
            // 
            // numericUpDown_scanWidth
            // 
            this.numericUpDown_scanWidth.Location = new Point(94, 37);
            this.numericUpDown_scanWidth.Maximum = new decimal(new int[] { 131072, 0, 0, 0 });
            this.numericUpDown_scanWidth.Minimum = new decimal(new int[] { 256, 0, 0, 0 });
            this.numericUpDown_scanWidth.Name = "numericUpDown_scanWidth";
            this.numericUpDown_scanWidth.Size = new Size(60, 23);
            this.numericUpDown_scanWidth.TabIndex = 43;
            this.numericUpDown_scanWidth.Value = new decimal(new int[] { 16384, 0, 0, 0 });
            // 
            // label_info_scanWidth
            // 
            this.label_info_scanWidth.AutoSize = true;
            this.label_info_scanWidth.Location = new Point(94, 19);
            this.label_info_scanWidth.Name = "label_info_scanWidth";
            this.label_info_scanWidth.Size = new Size(39, 15);
            this.label_info_scanWidth.TabIndex = 42;
            this.label_info_scanWidth.Text = "Width";
            // 
            // numericUpDown_lookingRange
            // 
            this.numericUpDown_lookingRange.Location = new Point(6, 37);
            this.numericUpDown_lookingRange.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            this.numericUpDown_lookingRange.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numericUpDown_lookingRange.Name = "numericUpDown_lookingRange";
            this.numericUpDown_lookingRange.Size = new Size(60, 23);
            this.numericUpDown_lookingRange.TabIndex = 38;
            this.numericUpDown_lookingRange.Value = new decimal(new int[] { 12, 0, 0, 0 });
            // 
            // button_keyScan
            // 
            this.button_keyScan.BackColor = Color.Lavender;
            this.button_keyScan.Location = new Point(5, 77);
            this.button_keyScan.Name = "button_keyScan";
            this.button_keyScan.Size = new Size(56, 23);
            this.button_keyScan.TabIndex = 40;
            this.button_keyScan.Text = "Key";
            this.button_keyScan.UseVisualStyleBackColor = false;
            this.button_keyScan.Click += this.button_keyScan_Click;
            // 
            // textBox_scannedKey
            // 
            this.textBox_scannedKey.Location = new Point(67, 77);
            this.textBox_scannedKey.MaxLength = 64;
            this.textBox_scannedKey.Name = "textBox_scannedKey";
            this.textBox_scannedKey.PlaceholderText = "Not scanned";
            this.textBox_scannedKey.ReadOnly = true;
            this.textBox_scannedKey.Size = new Size(87, 23);
            this.textBox_scannedKey.TabIndex = 41;
            // 
            // button_timingScan
            // 
            this.button_timingScan.BackColor = Color.Lavender;
            this.button_timingScan.Location = new Point(6, 106);
            this.button_timingScan.Name = "button_timingScan";
            this.button_timingScan.Size = new Size(55, 23);
            this.button_timingScan.TabIndex = 39;
            this.button_timingScan.Text = "Timing";
            this.button_timingScan.UseVisualStyleBackColor = false;
            this.button_timingScan.Click += this.button_timingScan_Click;
            // 
            // button_bpmScan
            // 
            this.button_bpmScan.BackColor = Color.Lavender;
            this.button_bpmScan.Location = new Point(6, 135);
            this.button_bpmScan.Name = "button_bpmScan";
            this.button_bpmScan.Size = new Size(55, 23);
            this.button_bpmScan.TabIndex = 38;
            this.button_bpmScan.Text = "BPM";
            this.button_bpmScan.UseVisualStyleBackColor = false;
            this.button_bpmScan.Click += this.button_bpmScan_Click;
            // 
            // textBox_scannedTiming
            // 
            this.textBox_scannedTiming.Location = new Point(67, 106);
            this.textBox_scannedTiming.MaxLength = 64;
            this.textBox_scannedTiming.Name = "textBox_scannedTiming";
            this.textBox_scannedTiming.PlaceholderText = "Not scanned";
            this.textBox_scannedTiming.ReadOnly = true;
            this.textBox_scannedTiming.Size = new Size(87, 23);
            this.textBox_scannedTiming.TabIndex = 39;
            // 
            // textBox_scannedBpm
            // 
            this.textBox_scannedBpm.Location = new Point(67, 135);
            this.textBox_scannedBpm.MaxLength = 64;
            this.textBox_scannedBpm.Name = "textBox_scannedBpm";
            this.textBox_scannedBpm.PlaceholderText = "Not scanned";
            this.textBox_scannedBpm.ReadOnly = true;
            this.textBox_scannedBpm.Size = new Size(87, 23);
            this.textBox_scannedBpm.TabIndex = 38;
            // 
            // groupBox_basicProcessing
            // 
            this.groupBox_basicProcessing.BackColor = SystemColors.ControlLight;
            this.groupBox_basicProcessing.Controls.Add(this.checkBox_autoParameters);
            this.groupBox_basicProcessing.Controls.Add(this.checkBox_optionalParameters);
            this.groupBox_basicProcessing.Controls.Add(this.panel_basicProcessingParameters);
            this.groupBox_basicProcessing.Controls.Add(this.button_basicProcessingGo);
            this.groupBox_basicProcessing.Controls.Add(this.comboBox_basicProcessing);
            this.groupBox_basicProcessing.Location = new Point(593, 417);
            this.groupBox_basicProcessing.Name = "groupBox_basicProcessing";
            this.groupBox_basicProcessing.Size = new Size(250, 214);
            this.groupBox_basicProcessing.TabIndex = 38;
            this.groupBox_basicProcessing.TabStop = false;
            this.groupBox_basicProcessing.Text = "Basic Audio Processing";
            // 
            // checkBox_autoParameters
            // 
            this.checkBox_autoParameters.AutoSize = true;
            this.checkBox_autoParameters.Font = new Font("Bahnschrift SemiCondensed", 9F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.checkBox_autoParameters.Location = new Point(4, 190);
            this.checkBox_autoParameters.Margin = new Padding(1, 3, 0, 3);
            this.checkBox_autoParameters.Name = "checkBox_autoParameters";
            this.checkBox_autoParameters.Size = new Size(108, 18);
            this.checkBox_autoParameters.TabIndex = 39;
            this.checkBox_autoParameters.Text = "Auto Parameters";
            this.checkBox_autoParameters.UseVisualStyleBackColor = true;
            // 
            // checkBox_optionalParameters
            // 
            this.checkBox_optionalParameters.AutoSize = true;
            this.checkBox_optionalParameters.Checked = true;
            this.checkBox_optionalParameters.CheckState = CheckState.Checked;
            this.checkBox_optionalParameters.Font = new Font("Bahnschrift SemiCondensed", 9F, FontStyle.Regular, GraphicsUnit.Point,  0);
            this.checkBox_optionalParameters.Location = new Point(120, 190);
            this.checkBox_optionalParameters.Margin = new Padding(1, 3, 1, 3);
            this.checkBox_optionalParameters.Name = "checkBox_optionalParameters";
            this.checkBox_optionalParameters.Size = new Size(126, 18);
            this.checkBox_optionalParameters.TabIndex = 39;
            this.checkBox_optionalParameters.Text = "Optional Parameters";
            this.checkBox_optionalParameters.UseVisualStyleBackColor = true;
            // 
            // panel_basicProcessingParameters
            // 
            this.panel_basicProcessingParameters.BackColor = Color.WhiteSmoke;
            this.panel_basicProcessingParameters.Location = new Point(6, 51);
            this.panel_basicProcessingParameters.Name = "panel_basicProcessingParameters";
            this.panel_basicProcessingParameters.Size = new Size(238, 132);
            this.panel_basicProcessingParameters.TabIndex = 39;
            // 
            // button_basicProcessingGo
            // 
            this.button_basicProcessingGo.Location = new Point(202, 22);
            this.button_basicProcessingGo.Name = "button_basicProcessingGo";
            this.button_basicProcessingGo.Size = new Size(42, 23);
            this.button_basicProcessingGo.TabIndex = 39;
            this.button_basicProcessingGo.Text = "Go";
            this.button_basicProcessingGo.UseVisualStyleBackColor = true;
            // 
            // comboBox_basicProcessing
            // 
            this.comboBox_basicProcessing.FormattingEnabled = true;
            this.comboBox_basicProcessing.Location = new Point(6, 22);
            this.comboBox_basicProcessing.Name = "comboBox_basicProcessing";
            this.comboBox_basicProcessing.Size = new Size(190, 23);
            this.comboBox_basicProcessing.TabIndex = 39;
            this.comboBox_basicProcessing.Text = "Select a function ...";
            // 
            // button_loop
            // 
            this.button_loop.Font = new Font("Segoe UI Symbol", 9F, FontStyle.Bold, GraphicsUnit.Point,  0);
            this.button_loop.Location = new Point(1232, 373);
            this.button_loop.Name = "button_loop";
            this.button_loop.Size = new Size(23, 23);
            this.button_loop.TabIndex = 39;
            this.button_loop.Text = "↺";
            this.button_loop.UseVisualStyleBackColor = true;
            this.button_loop.Click += this.button_loop_Click;
            // 
            // groupBox_processingV2
            // 
            this.groupBox_processingV2.BackColor = SystemColors.ControlLight;
            this.groupBox_processingV2.Controls.Add(this.button_advancedProcessingGo);
            this.groupBox_processingV2.Controls.Add(this.comboBox_advancedProcessing);
            this.groupBox_processingV2.Location = new Point(337, 417);
            this.groupBox_processingV2.Name = "groupBox_processingV2";
            this.groupBox_processingV2.Size = new Size(250, 214);
            this.groupBox_processingV2.TabIndex = 41;
            this.groupBox_processingV2.TabStop = false;
            this.groupBox_processingV2.Text = "Full-Auto Advanced Processing";
            // 
            // button_advancedProcessingGo
            // 
            this.button_advancedProcessingGo.Location = new Point(202, 22);
            this.button_advancedProcessingGo.Name = "button_advancedProcessingGo";
            this.button_advancedProcessingGo.Size = new Size(42, 23);
            this.button_advancedProcessingGo.TabIndex = 40;
            this.button_advancedProcessingGo.Text = "Go";
            this.button_advancedProcessingGo.UseVisualStyleBackColor = true;
            // 
            // comboBox_advancedProcessing
            // 
            this.comboBox_advancedProcessing.FormattingEnabled = true;
            this.comboBox_advancedProcessing.Location = new Point(6, 22);
            this.comboBox_advancedProcessing.Name = "comboBox_advancedProcessing";
            this.comboBox_advancedProcessing.Size = new Size(190, 23);
            this.comboBox_advancedProcessing.TabIndex = 41;
            this.comboBox_advancedProcessing.Text = "Select a function ...";
            // 
            // WindowMain
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1424, 821);
            this.Controls.Add(this.groupBox_processingV2);
            this.Controls.Add(this.button_loop);
            this.Controls.Add(this.groupBox_basicProcessing);
            this.Controls.Add(this.groupBox_audioScanner);
            this.Controls.Add(this.listBox_audios);
            this.Controls.Add(this.button_move);
            this.Controls.Add(this.listBox_reserve);
            this.Controls.Add(this.button_infoHotkeys);
            this.Controls.Add(this.label_volume);
            this.Controls.Add(this.button_pause);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox_trackMetrics);
            this.Controls.Add(this.button_drumSet);
            this.Controls.Add(this.hScrollBar_scroll);
            this.Controls.Add(this.groupBox_view);
            this.Controls.Add(this.label_sampleArea);
            this.Controls.Add(this.label_sampleAtCursor);
            this.Controls.Add(this.vScrollBar_volume);
            this.Controls.Add(this.textBox_audioInfo);
            this.Controls.Add(this.button_autoCut);
            this.Controls.Add(this.label_audioName);
            this.Controls.Add(this.listBox_log);
            this.Controls.Add(this.textBox_timestamp);
            this.Controls.Add(this.button_playback);
            this.Controls.Add(this.button_remove);
            this.Controls.Add(this.button_reload);
            this.Controls.Add(this.button_export);
            this.Controls.Add(this.button_load);
            this.Controls.Add(this.pictureBox_wave);
            this.MaximizeBox = false;
            this.MinimumSize = new Size(720, 720);
            this.Name = "WindowMain";
            this.Text = "CSharpSamplesCutter (Forms UI)";
            ((System.ComponentModel.ISupportInitialize) this.pictureBox_wave).EndInit();
            this.groupBox_view.ResumeLayout(false);
            this.groupBox_view.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_skipTracks).EndInit();
            this.panel_enableSamplesPerPixel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_samplesPerPixel).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_timeMarkers).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_caretWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_frameRate).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_hue).EndInit();
            this.groupBox_audioScanner.ResumeLayout(false);
            this.groupBox_audioScanner.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_scanWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize) this.numericUpDown_lookingRange).EndInit();
            this.groupBox_basicProcessing.ResumeLayout(false);
            this.groupBox_basicProcessing.PerformLayout();
            this.groupBox_processingV2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
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
        private TextBox textBox_trackMetrics;
        private GroupBox groupBox1;
        private CheckBox checkBox_timeMarkers;
        private NumericUpDown numericUpDown_timeMarkers;
        private Label label_info_caretWidth;
        private NumericUpDown numericUpDown_caretWidth;
        private CheckBox checkBox_scrollLog;
        private Label label_info_frameRate;
        private NumericUpDown numericUpDown_frameRate;
        private Button button_colorCaret;
        private Button button_strobe;
        private NumericUpDown numericUpDown_hue;
        private CheckBox checkBox_hue;
        private CheckBox checkBox_smoothen;
        private CheckBox checkBox_drawEachChannel;
        private Label label_info_colors;
        private Button button_colorBack;
        private Button button_colorWave;
        private HScrollBar hScrollBar_caretPosition;
        private Label label_info_caretPosition;
        private CheckBox checkBox_solo;
        private Button button_pause;
        private Label label_volume;
        private Button button_infoHotkeys;
        private ListBox listBox_reserve;
        private Button button_move;
        private ListBox listBox_audios;
        private Button button_colorSelection;
        private GroupBox groupBox_audioScanner;
        private TextBox textBox_scannedTiming;
        private TextBox textBox_scannedBpm;
        private Button button_bpmScan;
        private Button button_keyScan;
        private TextBox textBox_scannedKey;
        private Button button_timingScan;
        private NumericUpDown numericUpDown_lookingRange;
        private Label label_info_scanWidth;
        private Label label_info_multiplierSymbol;
        private Label label_info_scanRange;
        private NumericUpDown numericUpDown_scanWidth;
        private GroupBox groupBox_basicProcessing;
        private Panel panel_basicProcessingParameters;
        private Button button_basicProcessingGo;
        private ComboBox comboBox_basicProcessing;
        private CheckBox checkBox_optionalParameters;
        private CheckBox checkBox_autoParameters;
        private Button button_loop;
        private GroupBox groupBox_processingV2;
        private Button button_advancedProcessingGo;
        private ComboBox comboBox_advancedProcessing;
    }
}
