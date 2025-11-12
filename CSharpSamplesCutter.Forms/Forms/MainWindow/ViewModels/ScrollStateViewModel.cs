namespace CSharpSamplesCutter.Forms.MainWindow.ViewModels
{
    /// <summary>
    ///  Verwaltet Scroll- und Ansichtsstatus der Wellenformdarstellung.
    /// </summary>
    internal sealed class ScrollStateViewModel
    {
        public long ViewOffsetFrames { get; set; }

        public bool SuppressScrollEvent { get; set; }

        public bool IsUserScroll { get; set; }
    }
}
