using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private void WindowMain_Load(object? sender, EventArgs e)
        {
            this.label_info_caretPosition.Text = $"Caret Position: {this.CaretPosition:P1}";

            this.listBox_audios.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBox_audios.Items.Clear();
            this.listBox_audios.DrawMode = DrawMode.OwnerDrawFixed;
            this.listBox_audios.DrawItem += this.ListBox_audios_DrawItem;
            this.listBox_audios.ValueMember = "Id";
            this.listBox_audios.DisplayMember = "Name";
            this.RebindAudioListForSkip();

            this.listBox_reserve.Items.Clear();
            this.listBox_reserve.DrawMode = DrawMode.OwnerDrawFixed;
            this.listBox_reserve.DrawItem += this.ListBox_audios_DrawItem;
            this.listBox_reserve.ValueMember = "Id";
            this.listBox_reserve.DisplayMember = "Name";
            this.listBox_reserve.DataSource = this.AudioC_res.Audios;
            this.listBox_reserve.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;

            this.button_autoCut.Enabled = false;
            this.button_playback.Enabled = false;
            this.button_export.Enabled = false;
            this.button_reload.Enabled = false;
            this.button_remove.Enabled = false;
            this.hScrollBar_scroll.Enabled = false;

            this.AudioC.Audios.ListChanged += (s, ev) =>
            {
                this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;
                if (this.numericUpDown_skipTracks.Value > this.numericUpDown_skipTracks.Maximum)
                {
                    this.numericUpDown_skipTracks.Value = this.numericUpDown_skipTracks.Maximum;
                }
                this.RebindAudioListForSkip();
            };
            this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;

            this.listBox_audios.SelectedIndexChanged += (s, ev) => this.ListBox_Audios_SelectedValueChanged(s, ev, this.listBox_reserve);
            this.listBox_reserve.SelectedIndexChanged += (s, ev) => this.ListBox_Audios_SelectedValueChanged(s, ev, this.listBox_audios);

            this.listBox_audios.MouseUp += this.ListBox_Audios_RightClickMenu;
            this.listBox_reserve.MouseUp += this.ListBox_Audios_RightClickMenu;

            this.listBox_audios.DoubleClick += (s, ev) => this.button_move_Click(this.listBox_audios, EventArgs.Empty);
            this.listBox_reserve.DoubleClick += (s, ev) => this.button_move_Click(this.listBox_reserve, EventArgs.Empty);

            this.listBox_audios.SelectedIndex = -1;
            this.listBox_reserve.SelectedIndex = -1;

            this.listBox_log.Items.Clear();
            this.listBox_log.DataSource = LogCollection.Logs;
            LogCollection.Logs.ListChanged += (s, ev) =>
            {
                try
                {
                    this.listBox_log.Invoke(() => { this.listBox_log.TopIndex = this.listBox_log.Items.Count - 1; });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };
            this.listBox_log.DoubleClick += (s, ev) =>
            {
                if (this.listBox_log.SelectedItem != null)
                {
                    Clipboard.SetText(this.listBox_log.SelectedItem.ToString() ?? string.Empty);
                    LogCollection.Log("Log entry copied to clipboard.");
                }
            };

            this.numericUpDown_samplesPerPixel.MouseUp += (s, ev) =>
            {
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    this.numericUpDown_samplesPerPixel.Enabled = false;
                }
            };
            this.panel_enableSamplesPerPixel.MouseUp += (s, ev) =>
            {
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    this.numericUpDown_samplesPerPixel.Enabled = true;
                }
            };

            this.numericUpDown_skipTracks.ValueChanged += this.numericUpDown_skipTracks_ValueChanged;
            this.numericUpDown_skipTracks.MouseDown += (s, ev) =>
            {
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    this.numericUpDown_skipTracks.Value = 0;
                }
            };

            this.button_colorBack.MouseDown -= this.button_colorBack_Click;
            this.button_colorBack.MouseDown += this.button_colorBack_MouseDown;
            this.button_colorWave.Click -= this.button_colorWave_Click;
            this.button_colorWave.Click += this.button_colorWave_Click;

            this.button_colorSelection.BackColor = GetFadedColor(this.button_colorWave.BackColor, 0.33f);

            this.KeyDown += this.Form_CtrlZ_Pressed;
            this.KeyDown += this.Form_CtrlY_Pressed;
            this.KeyDown += this.Form_CtrlC_Pressed;
            this.KeyDown += this.Form_Del_Pressed;
            this.KeyDown += this.Form_Back_Pressed;
            this.KeyDown += this.Form_Space_Pressed;
            this.Register_PictureBox_Events(this.pictureBox_wave);
            this.Register_NumericUpDown_ToBePowOf2(this.numericUpDown_scanWidth);
            this.listBox_audios.AllowDrop = true;
            this.listBox_reserve.AllowDrop = true;
            this.listBox_audios.MouseDown += this.ListBox_MouseDown_Drag;
            this.listBox_reserve.MouseDown += this.ListBox_MouseDown_Drag;
            this.listBox_audios.MouseMove += this.ListBox_MouseMove_Drag;
            this.listBox_reserve.MouseMove += this.ListBox_MouseMove_Drag;
            this.listBox_audios.DragOver += this.ListBox_DragOver;
            this.listBox_reserve.DragOver += this.ListBox_DragOver;
            this.listBox_audios.DragDrop += this.ListBox_DragDrop;
            this.listBox_reserve.DragDrop += this.ListBox_DragDrop;
            this.AllowDrop = true;
            this.DragEnter += this.WindowMain_DragEnter;
            this.DragDrop += this.WindowMain_DragDrop;
            this.comboBox_basicProcessing.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void PreviousSteps_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                try
                {
                    this.BeginInvoke(new Action(this.UpdateUndoLabel));
                }
                catch
                {
                }
            }
            else
            {
                this.UpdateUndoLabel();
            }
        }

        private void RebindAudioListForSkip()
        {
            Guid selectedId = this.listBox_audios.SelectedValue is Guid id ? id : Guid.Empty;
            int skip = Math.Clamp(this.SkipTracks, 0, this.AudioC.Audios.Count);
            var slice = this.AudioC.Audios.Skip(skip).ToList();
            this.listBox_audios.DataSource = null;
            this.listBox_audios.DataSource = slice;
            this.listBox_audios.DisplayMember = "Name";
            this.listBox_audios.ValueMember = "Id";
            if (selectedId != Guid.Empty && slice.Any(a => a.Id == selectedId))
            {
                this.listBox_audios.SelectedValue = selectedId;
            }
            else
            {
                this.listBox_audios.SelectedIndex = slice.Count > 0 ? 0 : -1;
            }
        }


    }
}
