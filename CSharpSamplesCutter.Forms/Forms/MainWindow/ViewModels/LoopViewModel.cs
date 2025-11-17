using System;

namespace CSharpSamplesCutter.Forms.MainWindow.ViewModels
{
    public class LoopViewModel
    {
        private bool loopEnabled = false;
        private int loopFractionIndex = 0;

        // ✅ ALLE Loop-Brüche: 1/1, 1/2, 1/4, 1/8, 1/16, 1/32, 1/64
        public readonly int[] LoopFractionDenominators = { 1, 2, 4, 8, 16, 32, 64 };

        public bool LoopEnabled
        {
            get => this.loopEnabled;
            set => this.loopEnabled = value;
        }

        public int LoopFractionIndex
        {
            get => this.loopFractionIndex;
            set => this.loopFractionIndex = Math.Clamp(value, 0, this.LoopFractionDenominators.Length - 1);
        }

        public int CurrentLoopFractionDenominator =>
            this.LoopEnabled ? this.LoopFractionDenominators[this.LoopFractionIndex] : 0;

        public string GetLoopFractionString() =>
            this.LoopEnabled ? $"1/{this.CurrentLoopFractionDenominator}" : "↻";

        public void CycleLoopFraction(bool previous = false)
        {
            if (previous)
            {
                // Cycle one back
                this.LoopFractionIndex = (this.LoopFractionIndex - 1 + this.LoopFractionDenominators.Length) % this.LoopFractionDenominators.Length;
			}
            else
            {
                this.LoopFractionIndex = (this.LoopFractionIndex + 1) % this.LoopFractionDenominators.Length;
            }
        }

        public void ResetLoop()
        {
            this.loopEnabled = false;
            this.loopFractionIndex = 0;
        }
    }
}
