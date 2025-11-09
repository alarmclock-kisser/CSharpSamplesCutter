namespace CSharpSamplesCutter.Forms.Dialogs
{
    partial class HotkeysInfoDialog
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
            this.button_confirm = new Button();
            this.textBox_message = new TextBox();
            this.button_next = new Button();
            this.button_previous = new Button();
            this.label_page = new Label();
            this.panel_pages = new Panel();
            this.button_verbose = new Button();
            this.panel_pages.SuspendLayout();
            this.SuspendLayout();
            // 
            // button_confirm
            // 
            this.button_confirm.BackColor = SystemColors.Info;
            this.button_confirm.Location = new Point(733, 415);
            this.button_confirm.Name = "button_confirm";
            this.button_confirm.Size = new Size(55, 23);
            this.button_confirm.TabIndex = 0;
            this.button_confirm.Text = "OK";
            this.button_confirm.UseVisualStyleBackColor = false;
            this.button_confirm.Click += this.button_confirm_Click;
            // 
            // textBox_message
            // 
            this.textBox_message.Location = new Point(12, 12);
            this.textBox_message.MaxLength = 65536;
            this.textBox_message.Multiline = true;
            this.textBox_message.Name = "textBox_message";
            this.textBox_message.ScrollBars = ScrollBars.Vertical;
            this.textBox_message.Size = new Size(776, 397);
            this.textBox_message.TabIndex = 1;
            // 
            // button_next
            // 
            this.button_next.Location = new Point(48, 3);
            this.button_next.Name = "button_next";
            this.button_next.Size = new Size(20, 23);
            this.button_next.TabIndex = 2;
            this.button_next.Text = ">";
            this.button_next.UseVisualStyleBackColor = true;
            this.button_next.Click += this.button_next_Click;
            // 
            // button_previous
            // 
            this.button_previous.Location = new Point(3, 3);
            this.button_previous.Name = "button_previous";
            this.button_previous.Size = new Size(20, 23);
            this.button_previous.TabIndex = 3;
            this.button_previous.Text = "<";
            this.button_previous.UseVisualStyleBackColor = true;
            this.button_previous.Click += this.button_previous_Click;
            // 
            // label_page
            // 
            this.label_page.AutoSize = true;
            this.label_page.Location = new Point(29, 7);
            this.label_page.Name = "label_page";
            this.label_page.Size = new Size(12, 15);
            this.label_page.TabIndex = 4;
            this.label_page.Text = "-";
            // 
            // panel_pages
            // 
            this.panel_pages.BackColor = Color.WhiteSmoke;
            this.panel_pages.Controls.Add(this.button_previous);
            this.panel_pages.Controls.Add(this.label_page);
            this.panel_pages.Controls.Add(this.button_next);
            this.panel_pages.Location = new Point(12, 412);
            this.panel_pages.Margin = new Padding(0);
            this.panel_pages.Name = "panel_pages";
            this.panel_pages.Size = new Size(71, 29);
            this.panel_pages.TabIndex = 5;
            // 
            // button_verbose
            // 
            this.button_verbose.BackColor = Color.FromArgb(  255,   192,   192);
            this.button_verbose.Location = new Point(86, 415);
            this.button_verbose.Name = "button_verbose";
            this.button_verbose.Size = new Size(60, 23);
            this.button_verbose.TabIndex = 6;
            this.button_verbose.Text = "Verbose";
            this.button_verbose.UseVisualStyleBackColor = false;
            this.button_verbose.Click += this.button_verbose_Click;
            // 
            // HotkeysInfoDialog
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = SystemColors.ControlLight;
            this.ClientSize = new Size(800, 450);
            this.Controls.Add(this.button_verbose);
            this.Controls.Add(this.panel_pages);
            this.Controls.Add(this.textBox_message);
            this.Controls.Add(this.button_confirm);
            this.MaximizeBox = false;
            this.Name = "HotkeysInfoDialog";
            this.Text = "Hotkeys & Controls - Information";
            this.panel_pages.ResumeLayout(false);
            this.panel_pages.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private Button button_confirm;
        private TextBox textBox_message;
        private Button button_next;
        private Button button_previous;
        private Label label_page;
        private Panel panel_pages;
        private Button button_verbose;
    }
}