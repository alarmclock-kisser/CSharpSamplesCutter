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
    public partial class HotkeysInfoDialog : Form
    {
        public int CurrentPage { get; private set; } = 0;
        public bool VerboseMode { get; private set; } = true;

        public HotkeysInfoDialog(bool verbose = false)
        {
            this.InitializeComponent();
            this.VerboseMode = verbose;

            this.StartPosition = FormStartPosition.Manual;
            this.Location = WindowsScreenHelper.GetCenterStartingPoint(this);

            this.Load += this.HotkeysInfoDialog_Load;
        }

        private void HotkeysInfoDialog_Load(object? sender, EventArgs e)
        {
            this.label_page.TextChanged += this.Label_page_TextChanged;

            this.label_page.Text = "0";
        }

        private void Label_page_TextChanged(object? sender, EventArgs e)
        {
            this.CurrentPage = int.TryParse(this.label_page.Text, out int result) ? result : 0;
            this.textBox_message.Text = this.VerboseMode ? GetVerbosePageContent().ElementAtOrDefault(this.CurrentPage) ?? "No content available for this page." : GetSimplePageContent().ElementAtOrDefault(this.CurrentPage) ?? "No content available for this page.";

            this.button_previous.Enabled = this.CurrentPage > 0;
            this.button_next.Enabled = this.VerboseMode ? this.CurrentPage < PageCountVerbose - 1 : this.CurrentPage < PageCountSimple - 1;
        }


        private void button_previous_Click(object sender, EventArgs e)
        {
            this.label_page.Text = Math.Max(0, this.CurrentPage - 1).ToString();
        }

        private void button_next_Click(object sender, EventArgs e)
        {
            this.label_page.Text = Math.Min(PageCountVerbose - 1, this.CurrentPage + 1).ToString();
        }





        private static string[] GetVerbosePageContent()
        {
            string nl = Environment.NewLine;

            string[] content =
            [
				// --- PAGE 0 ---
				"General Hotkeys and Commands:" + nl + nl +
        " - Ctrl + Z : Undo last change on the selected track (uses AudioC.UndoAsync)." + nl +
        " - Ctrl + Y : Redo last undone change on the selected track (uses AudioC.RedoAsync)." + nl +
        " - Ctrl + C : Copy selection (only when SelectionMode == \"Select\")." + nl +
        " - Delete:" + nl +
        "     • Ctrl + Delete : Remove selected audio items (or all if multiple selected)." + nl +
        "     • Delete alone  : If SelectionMode == \"Select\" and an area is selected, deletes that area." + nl +
        " - Backspace:" + nl +
        "     • Ctrl + Backspace : Stops all tracks if any are playing, otherwise starts playback." + nl +
        "     • Backspace alone  : Jumps to start position and continues playback (Select mode) or erases before/after caret (Erase mode)." + nl +
        " - Ctrl + Load : Opens the Load + Auto-Cut dialog (custom LoadDialog)." + nl +
        " - Ctrl + Export : Choose export path for the current track." + nl +
        " - Shift + Export : Export all or selected samples." + nl +
        " - Ctrl + Playback : Stops all tracks when Ctrl is held while pressing Play." + nl +
        " - Esc : Cancels active selections or dialogs when applicable." + nl + nl +
        "Tip: Many destructive actions create snapshots (CreateSnapshotAsync) so Undo/Redo remains available.",

        // --- PAGE 1 ---
        "Mouse & Waveform Interaction:" + nl + nl +
        "Mouse Wheel:" + nl +
        " - Ctrl + Wheel : Zoom in/out centered around cursor (if over waveform) or screen center otherwise." + nl +
        " - Wheel alone  : Scroll horizontally through waveform." + nl + nl +
        "Mouse Input:" + nl +
        " - Mouse Down:" + nl +
        "     • Left click : Begin selection (Select or Erase mode)." + nl +
        "     • Right click: Cancel selection immediately." + nl +
        " - Mouse Up:" + nl +
        "     • In Select mode: finalize selection. If selection too short, sets caret position instead." + nl +
        "     • In Erase mode : snapshot and erase selection asynchronously." + nl +
        " - Mouse Move : Updates current selection area while dragging." + nl +
        " - Mouse Enter : Ensures PictureBox has focus for wheel/keyboard events." + nl +
        " - Single click (no selection): Sets new start/caret position." + nl + nl +
        "Note: Scroll position (viewOffsetFrames) is saved to track.ScrollOffset for each waveform when switching tracks.",

        // --- PAGE 2 ---
        "Selection Modes:" + nl + nl +
        "Selection mode toggles via button_selectionMode." + nl +
        "Visual cue: label color + icon updates between modes." + nl + nl +

        "Select Mode:" + nl +
        " - Click and drag to select an area in waveform." + nl +
        " - Right-click to cancel selection." + nl +
        " - Del deletes the selected area." + nl +
        " - Backspace jumps to selection start and resumes playback." + nl +
        " - Playback restores cursor to selection start after stopping." + nl + nl +

        "Erase Mode:" + nl +
        " - Click and drag to mark region for removal." + nl +
        " - On mouse up: creates snapshot and erases region." + nl +
        " - Shift + Backspace toggles between CutOffBefore / CutOffAfter." + nl +
        " - Uses async erase routines to prevent UI blocking.",

        // --- PAGE 3 ---
        "List, Track & UI Behavior:" + nl + nl +
        "ListBox (listBox_audios):" + nl +
        " - Ctrl + Click enables multi-selection (SelectionMode.MultiExtended)." + nl +
        " - Releasing Ctrl reverts to single selection (last item kept)." + nl +
        " - SelectedGuids affects Play, Export, Remove operations." + nl + nl +
        "Solo Checkbox:" + nl +
        " - When toggled on, stops all other tracks immediately." + nl + nl +
        "Scrollbars:" + nl +
        " - hScrollBar_scroll updates waveform offset (viewOffsetFrames)." + nl +
        " - numericUpDown_samplesPerPixel changes zoom (Ctrl + MouseUp disables it temporarily)." + nl +
        " - Right-click on panel with Ctrl re-enables numeric control." + nl + nl +
        "Time Markers:" + nl +
        " - checkBox_timeMarkers enables numericUpDown_timeMarkers." + nl +
        " - Default interval derived from BPM or saved value." + nl +
        "Volume Control:" + nl +
        " - vScrollBar_volume displays percentage while dragging." + nl +
        " - Updates track volume on EndScroll event." + nl + nl +
        "Logs:" + nl +
        " - listBox_log double-click copies entry text to clipboard.",

        // --- PAGE 4 ---
        "Miscellaneous Notes:" + nl + nl +
        " - Zoom anchors are computed so cursor position remains fixed during zoom." + nl +
        " - Each track stores its scroll offset independently (ScrollOffset)." + nl +
        " - Playback and UI events are designed for async execution (preventing blocking)." + nl +
        " - Most handlers support both single and multi-selection workflows transparently." + nl +
        " - Internal state machine ensures mode safety (Select/Erase/Solo) and async safety." + nl + nl +
        "Hint:" + nl +
        "This info was generated from full WindowMain.cs logic. Consider exporting these pages as HTML help or Markdown file for better readability." + nl
            ];

            return content;
        }

        private static string[] GetSimplePageContent()
        {
            string nl = Environment.NewLine;

            string[] content =
            [
				// --- PAGE 0 ---
				"General Hotkeys:" + nl + nl +
        "Ctrl + Z   → Undo" + nl +
        "Ctrl + Y   → Redo" + nl +
        "Ctrl + C   → Copy Selection" + nl +
        "Ctrl + Load   → Open Load / Auto-Cut" + nl +
        "Ctrl + Export → Choose Export Path" + nl +
        "Shift + Export → Export All / Selected Samples" + nl +
        "Ctrl + Playback → Stop All Tracks" + nl +
        "Ctrl + Delete → Remove All Samples" + nl +
        "Delete → Remove Selection (Select-Mode)" + nl +
        "Ctrl + Backspace → Stop All or Start Playback" + nl +
        "Backspace → Jump to Start / Resume Playback" + nl +
        "Esc → Cancel Selection or Dialog" + nl,

        // --- PAGE 1 ---
        "Mouse & Waveform:" + nl + nl +
        "Ctrl + Wheel → Zoom In/Out (cursor centered)" + nl +
        "Wheel → Scroll Left/Right" + nl +
        "Left Click + Drag → Select Area" + nl +
        "Right Click → Cancel Selection" + nl +
        "Mouse Up (Erase) → Cut / Remove Region" + nl +
        "Mouse Move → Update Selection Area" + nl +
        "Single Click (no selection) → Set Start Position" + nl +
        "Mouse Enter → Focus Waveform for Input" + nl,

        // --- PAGE 2 ---
        "Selection Modes:" + nl + nl +
        "Select-Mode:" + nl +
        "  - Drag → Select Area" + nl +
        "  - Right Click → Cancel Selection" + nl +
        "  - Del → Remove Selection" + nl +
        "  - Backspace → Reset to Start / Resume Playback" + nl + nl +
        "Erase-Mode:" + nl +
        "  - Drag → Mark Region to Remove" + nl +
        "  - Mouse Up → Erase Marked Area" + nl +
        "  - Shift + Backspace → Cut Before / After Cursor" + nl,

        // --- PAGE 3 ---
        "List / UI Controls:" + nl + nl +
        "ListBox (Tracks):" + nl +
        "  - Ctrl + Click → Multi-Select" + nl +
        "  - Release Ctrl → Return to Single Selection" + nl +
        "Solo Checkbox → Mute All Other Tracks" + nl +
        "hScrollBar → Scroll View" + nl +
        "Ctrl + MouseUp on Zoom → Lock Zoom Level" + nl +
        "Right Click + Ctrl on Panel → Unlock Zoom Control" + nl +
        "checkBox_timeMarkers → Enable Time Markers" + nl +
        "vScrollBar_volume → Adjust Track Volume" + nl +
        "listBox_log Double Click → Copy Log Entry" + nl,

        // --- PAGE 4 ---
        "Notes & Behavior:" + nl + nl +
        "Zoom Anchors Keep Cursor Fixed" + nl +
        "Scroll Offset Saved per Track" + nl +
        "Async Playback & Erase Operations" + nl +
        "Supports Multi-Track Selections" + nl +
        "Internal State Safe Between Modes" + nl +
        "Snapshots Enable Undo / Redo" + nl
            ];

            return content;
        }


        private void button_confirm_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button_verbose_Click(object sender, EventArgs e)
        {
            this.VerboseMode = !this.VerboseMode;
            int maxPage = this.VerboseMode ? PageCountVerbose - 1 : PageCountSimple - 1;
            if (this.CurrentPage > maxPage)
            {
                this.label_page.Text = maxPage.ToString();
            }
            else
            {
                // Anzeige sofort aktualisieren
                this.Label_page_TextChanged(this.label_page, EventArgs.Empty);
            }
        }

        private static int PageCountVerbose => GetVerbosePageContent().Length;
        private static int PageCountSimple => GetSimplePageContent().Length;
    }
}
