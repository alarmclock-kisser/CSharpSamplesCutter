
namespace CSharpSamplesCutter.Forms.Dialogs
{
	partial class LoadDialog
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
			this.textBox_path = new TextBox();
			this.button_browse = new Button();
			this.numericUpDown_count = new NumericUpDown();
			this.label_info_count = new Label();
			this.button_process = new Button();
			this.label_info_input = new Label();
			this.button_cancel = new Button();
			this.progressBar_processing = new ProgressBar();
			this.numericUpDown_startTime = new NumericUpDown();
			this.label_info_startTime = new Label();
			this.label_info_endTime = new Label();
			this.numericUpDown_endTime = new NumericUpDown();
			this.label_fileDuration = new Label();
			this.label_info_maxDuration = new Label();
			this.numericUpDown_maxDuration = new NumericUpDown();
			this.label_info_minDuration = new Label();
			this.numericUpDown_minDuration = new NumericUpDown();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_count).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_startTime).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_endTime).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_maxDuration).BeginInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_minDuration).BeginInit();
			this.SuspendLayout();
			// 
			// textBox_path
			// 
			this.textBox_path.Location = new Point(12, 27);
			this.textBox_path.Name = "textBox_path";
			this.textBox_path.PlaceholderText = "File path to audio (.wav, .mp3, .flac) ...";
			this.textBox_path.Size = new Size(454, 23);
			this.textBox_path.TabIndex = 0;
			this.textBox_path.KeyDown += this.textBox_path_Leave;
			this.textBox_path.Leave += this.textBox_path_Leave;
			// 
			// button_browse
			// 
			this.button_browse.Location = new Point(472, 27);
			this.button_browse.Name = "button_browse";
			this.button_browse.Size = new Size(75, 23);
			this.button_browse.TabIndex = 1;
			this.button_browse.Text = "Browse";
			this.button_browse.UseVisualStyleBackColor = true;
			this.button_browse.Click += this.button_browse_Click;
			// 
			// numericUpDown_count
			// 
			this.numericUpDown_count.Location = new Point(12, 99);
			this.numericUpDown_count.Maximum = new decimal(new int[] { 128, 0, 0, 0 });
			this.numericUpDown_count.Name = "numericUpDown_count";
			this.numericUpDown_count.Size = new Size(70, 23);
			this.numericUpDown_count.TabIndex = 2;
			this.numericUpDown_count.ValueChanged += this.numericUpDown_count_ValueChanged;
			// 
			// label_info_count
			// 
			this.label_info_count.AutoSize = true;
			this.label_info_count.Location = new Point(12, 81);
			this.label_info_count.Name = "label_info_count";
			this.label_info_count.Size = new Size(40, 15);
			this.label_info_count.TabIndex = 3;
			this.label_info_count.Text = "Count";
			// 
			// button_process
			// 
			this.button_process.BackColor = SystemColors.Info;
			this.button_process.Location = new Point(472, 99);
			this.button_process.Name = "button_process";
			this.button_process.Size = new Size(75, 23);
			this.button_process.TabIndex = 4;
			this.button_process.Text = "Process";
			this.button_process.UseVisualStyleBackColor = false;
			this.button_process.Click += this.button_process_Click;
			// 
			// label_info_input
			// 
			this.label_info_input.AutoSize = true;
			this.label_info_input.Location = new Point(12, 9);
			this.label_info_input.Name = "label_info_input";
			this.label_info_input.Size = new Size(35, 15);
			this.label_info_input.TabIndex = 5;
			this.label_info_input.Text = "Input";
			// 
			// button_cancel
			// 
			this.button_cancel.BackColor = Color.Gainsboro;
			this.button_cancel.Location = new Point(411, 99);
			this.button_cancel.Name = "button_cancel";
			this.button_cancel.Size = new Size(55, 23);
			this.button_cancel.TabIndex = 6;
			this.button_cancel.Text = "Cancel";
			this.button_cancel.UseVisualStyleBackColor = false;
			this.button_cancel.Click += this.button_cancel_Click;
			// 
			// progressBar_processing
			// 
			this.progressBar_processing.Location = new Point(12, 128);
			this.progressBar_processing.Name = "progressBar_processing";
			this.progressBar_processing.Size = new Size(535, 23);
			this.progressBar_processing.TabIndex = 7;
			// 
			// numericUpDown_startTime
			// 
			this.numericUpDown_startTime.DecimalPlaces = 3;
			this.numericUpDown_startTime.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericUpDown_startTime.Location = new Point(88, 99);
			this.numericUpDown_startTime.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numericUpDown_startTime.Name = "numericUpDown_startTime";
			this.numericUpDown_startTime.Size = new Size(70, 23);
			this.numericUpDown_startTime.TabIndex = 8;
			this.numericUpDown_startTime.ValueChanged += this.numericUpDown_startTime_ValueChanged;
			// 
			// label_info_startTime
			// 
			this.label_info_startTime.AutoSize = true;
			this.label_info_startTime.Location = new Point(88, 81);
			this.label_info_startTime.Name = "label_info_startTime";
			this.label_info_startTime.Size = new Size(31, 15);
			this.label_info_startTime.TabIndex = 9;
			this.label_info_startTime.Text = "Start";
			// 
			// label_info_endTime
			// 
			this.label_info_endTime.AutoSize = true;
			this.label_info_endTime.Location = new Point(164, 81);
			this.label_info_endTime.Name = "label_info_endTime";
			this.label_info_endTime.Size = new Size(27, 15);
			this.label_info_endTime.TabIndex = 11;
			this.label_info_endTime.Text = "End";
			// 
			// numericUpDown_endTime
			// 
			this.numericUpDown_endTime.DecimalPlaces = 3;
			this.numericUpDown_endTime.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericUpDown_endTime.Location = new Point(164, 99);
			this.numericUpDown_endTime.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
			this.numericUpDown_endTime.Name = "numericUpDown_endTime";
			this.numericUpDown_endTime.Size = new Size(70, 23);
			this.numericUpDown_endTime.TabIndex = 10;
			this.numericUpDown_endTime.ValueChanged += this.numericUpDown_endTime_ValueChanged;
			// 
			// label_fileDuration
			// 
			this.label_fileDuration.AutoSize = true;
			this.label_fileDuration.Location = new Point(12, 53);
			this.label_fileDuration.Name = "label_fileDuration";
			this.label_fileDuration.Size = new Size(64, 15);
			this.label_fileDuration.TabIndex = 12;
			this.label_fileDuration.Text = "Duration: -";
			// 
			// label_info_maxDuration
			// 
			this.label_info_maxDuration.AutoSize = true;
			this.label_info_maxDuration.Location = new Point(316, 66);
			this.label_info_maxDuration.Name = "label_info_maxDuration";
			this.label_info_maxDuration.Size = new Size(53, 30);
			this.label_info_maxDuration.TabIndex = 16;
			this.label_info_maxDuration.Text = "Max.\r\nDuration";
			// 
			// numericUpDown_maxDuration
			// 
			this.numericUpDown_maxDuration.DecimalPlaces = 3;
			this.numericUpDown_maxDuration.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
			this.numericUpDown_maxDuration.Location = new Point(316, 99);
			this.numericUpDown_maxDuration.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
			this.numericUpDown_maxDuration.Minimum = new decimal(new int[] { 3, 0, 0, 65536 });
			this.numericUpDown_maxDuration.Name = "numericUpDown_maxDuration";
			this.numericUpDown_maxDuration.Size = new Size(70, 23);
			this.numericUpDown_maxDuration.TabIndex = 15;
			this.numericUpDown_maxDuration.Value = new decimal(new int[] { 175, 0, 0, 131072 });
			// 
			// label_info_minDuration
			// 
			this.label_info_minDuration.AutoSize = true;
			this.label_info_minDuration.Location = new Point(240, 66);
			this.label_info_minDuration.Name = "label_info_minDuration";
			this.label_info_minDuration.Size = new Size(53, 30);
			this.label_info_minDuration.TabIndex = 14;
			this.label_info_minDuration.Text = "Min.\r\nDuration";
			// 
			// numericUpDown_minDuration
			// 
			this.numericUpDown_minDuration.DecimalPlaces = 3;
			this.numericUpDown_minDuration.Increment = new decimal(new int[] { 25, 0, 0, 196608 });
			this.numericUpDown_minDuration.Location = new Point(240, 99);
			this.numericUpDown_minDuration.Maximum = new decimal(new int[] { 1500, 0, 0, 196608 });
			this.numericUpDown_minDuration.Minimum = new decimal(new int[] { 75, 0, 0, 196608 });
			this.numericUpDown_minDuration.Name = "numericUpDown_minDuration";
			this.numericUpDown_minDuration.Size = new Size(70, 23);
			this.numericUpDown_minDuration.TabIndex = 13;
			this.numericUpDown_minDuration.Value = new decimal(new int[] { 25, 0, 0, 131072 });
			// 
			// LoadDialog
			// 
			this.AutoScaleDimensions = new SizeF(7F, 15F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.WhiteSmoke;
			this.ClientSize = new Size(559, 163);
			this.Controls.Add(this.label_info_maxDuration);
			this.Controls.Add(this.numericUpDown_maxDuration);
			this.Controls.Add(this.label_info_minDuration);
			this.Controls.Add(this.numericUpDown_minDuration);
			this.Controls.Add(this.label_fileDuration);
			this.Controls.Add(this.label_info_endTime);
			this.Controls.Add(this.numericUpDown_endTime);
			this.Controls.Add(this.label_info_startTime);
			this.Controls.Add(this.numericUpDown_startTime);
			this.Controls.Add(this.progressBar_processing);
			this.Controls.Add(this.button_cancel);
			this.Controls.Add(this.label_info_input);
			this.Controls.Add(this.button_process);
			this.Controls.Add(this.label_info_count);
			this.Controls.Add(this.numericUpDown_count);
			this.Controls.Add(this.button_browse);
			this.Controls.Add(this.textBox_path);
			this.Name = "LoadDialog";
			this.Text = "LoadDialog";
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_count).EndInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_startTime).EndInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_endTime).EndInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_maxDuration).EndInit();
			((System.ComponentModel.ISupportInitialize) this.numericUpDown_minDuration).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}


		#endregion

		private TextBox textBox_path;
		private Button button_browse;
		private NumericUpDown numericUpDown_count;
		private Label label_info_count;
		private Button button_process;
		private Label label_info_input;
		private Button button_cancel;
		private ProgressBar progressBar_processing;
		private NumericUpDown numericUpDown_startTime;
		private Label label_info_startTime;
		private Label label_info_endTime;
		private NumericUpDown numericUpDown_endTime;
		private Label label_fileDuration;
		private Label label_info_maxDuration;
		private NumericUpDown numericUpDown_maxDuration;
		private Label label_info_minDuration;
		private NumericUpDown numericUpDown_minDuration;
	}
}