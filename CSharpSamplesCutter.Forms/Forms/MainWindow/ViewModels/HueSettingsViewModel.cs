using System.Drawing;

namespace CSharpSamplesCutter.Forms.Forms.MainWindow.ViewModels
{
    /// <summary>
    ///  Hält Einstellungen zur Hue- und Strobe-Steuerung der Wellenform.
    /// </summary>
    internal sealed class HueSettingsViewModel
    {
        public float StoredHueValue { get; set; }

        public float HueAdjustment { get; set; }

        public Color HueColor { get; set; } = Color.BlueViolet;
    }
}
