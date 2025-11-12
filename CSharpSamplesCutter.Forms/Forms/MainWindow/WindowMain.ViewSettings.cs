using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private void button_colorWave_Click(object? sender, EventArgs e)
        {
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.WaveGraphColor,
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                var chosen = colorDialog.Color;
                this.button_colorWave.BackColor = chosen;
                this.button_colorWave.ForeColor = chosen.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void button_colorBack_Click(object? sender, EventArgs e)
        {
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.BackColor,
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.button_colorBack.BackColor = colorDialog.Color;
                this.button_colorBack.ForeColor = this.BackColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void button_colorBack_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.BackColor = GetNegativeColor(this.BackColor);
                this.button_colorBack.BackColor = GetShadedColor(this.BackColor, 0.95f);
                this.button_colorBack.ForeColor = this.BackColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
            else if (e.Button == MouseButtons.Left)
            {
                this.button_colorBack_Click(sender, EventArgs.Empty);
            }
        }

        private void button_colorCaret_Click(object sender, EventArgs e)
        {
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.CaretColor,
            };
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                this.button_colorCaret.BackColor = colorDialog.Color;
                this.button_colorCaret.ForeColor = this.CaretColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void button_colorSelection_Click(object sender, EventArgs e)
        {
            using ColorDialog colorDialog = new()
            {
                AllowFullOpen = true,
                AnyColor = true,
                FullOpen = true,
                Color = this.SelectionColor
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                this.button_colorSelection.BackColor = colorDialog.Color;
                this.button_colorSelection.BackColor = GetFadedColor(colorDialog.Color, 0.25f);
                this.button_colorSelection.ForeColor = this.SelectionColor.GetBrightness() < 0.6f ? Color.White : Color.Black;
            }
        }

        private void checkBox_hue_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_hue.Checked)
            {
                this.numericUpDown_hue.Enabled = !this.StrobeEffect;
                if (this.numericUpDown_hue.Value <= 0)
                {
                    this.numericUpDown_hue.Value = 1.75m;
                }
                this.StoredHueValue = (float) this.numericUpDown_hue.Value;

                if (this.StrobeEffect)
                {
                    this.HueAdjustment = this.StrobeHueAdjustment;
                    this.numericUpDown_hue.Enabled = false;
                }
                else
                {
                    this.HueAdjustment = this.DefaultHueAdjustment;
                    this.numericUpDown_hue.Enabled = true;
                }
            }
            else
            {
                this.button_strobe.ForeColor = Color.Black;
                this.numericUpDown_hue.Enabled = false;
                this.StoredHueValue = 0.0f;
                this.HueAdjustment = 0.0f;
            }
        }

        private void button_strobe_Click(object sender, EventArgs e)
        {
            bool strobeOn = this.button_strobe.ForeColor != Color.Red;

            if (strobeOn)
            {
                this.button_strobe.ForeColor = Color.Red;
                this.button_strobe.Text = "☠️";
                this.checkBox_hue.Checked = true;
                this.HueAdjustment = this.StrobeHueAdjustment;
                this.numericUpDown_hue.Enabled = false;
            }
            else
            {
                this.button_strobe.ForeColor = Color.Black;
                this.button_strobe.Text = "⚡";
                this.HueAdjustment = this.DefaultHueAdjustment;
                this.numericUpDown_hue.Enabled = true;
            }
        }

        private void numericUpDown_hue_ValueChanged(object sender, EventArgs e)
        {
            this.StoredHueValue = (float) this.numericUpDown_hue.Value;
            if (!this.StrobeEffect && this.HueEnabled)
            {
                this.HueAdjustment = this.DefaultHueAdjustment;
            }
        }

        private void button_selectionMode_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> modes = new()
            {
                { "Select", "⛶" },
                { "Erase", "⛏️" }
            };

            var keys = modes.Keys.ToList();
            int currentIdx = Math.Max(0, keys.FindIndex(k => k.Equals(this.SelectionMode, StringComparison.OrdinalIgnoreCase)));
            int nextIdx = (currentIdx + 1) % keys.Count;
            this.SelectionMode = keys[nextIdx];

            this.label_selectionMode.Text = $"{this.SelectionMode}";
            switch (this.SelectionMode)
            {
                case "Select":
                    this.label_selectionMode.ForeColor = Color.Green;
                    this.button_copy.Enabled = true;
                    break;
                case "Erase":
                    this.label_selectionMode.ForeColor = Color.Red;
                    this.button_copy.Enabled = false;
                    break;
                default:
                    this.label_selectionMode.ForeColor = Color.Black;
                    this.button_copy.Enabled = true;
                    break;
            }

            this.button_selectionMode.Text = modes[this.SelectionMode];
        }

        private Color GetNextHue(float? increment = null, bool updateHueColor = true)
        {
            increment ??= this.HueAdjustment;

            float currentHue = this.HueColor.GetHue();
            float newHue = (currentHue + increment.Value) % 360f;

            if (updateHueColor)
            {
                this.HueColor = ColorFromHSV(newHue, 1.0f, 1.0f);
            }

            return ColorFromHSV(newHue, 1.0f, 1.0f);
        }

        public static Color GetNegativeColor(Color color)
        {
            return Color.FromArgb(color.A, 255 - color.R, 255 - color.G, 255 - color.B);
        }

        public static Color GetShadedColor(Color color, float factor = 0.67f)
        {
            factor = Math.Clamp(factor, 0.0f, 1.0f);
            return Color.FromArgb(
                color.A,
                (int) (color.R * factor),
                (int) (color.G * factor),
                (int) (color.B * factor)
            );
        }

        public static Color GetFadedColor(Color color, float alphaFactor = 0.5f)
        {
            alphaFactor = Math.Clamp(alphaFactor, 0.0f, 1.0f);
            return Color.FromArgb(
                (int) (color.A * alphaFactor),
                color.R,
                color.G,
                color.B
            );
        }

        public static Color ColorFromHSV(float hue, float saturation, float value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            float f = hue / 60 - (float) Math.Floor(hue / 60);
            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));
            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q),
            };
        }
    }
}
