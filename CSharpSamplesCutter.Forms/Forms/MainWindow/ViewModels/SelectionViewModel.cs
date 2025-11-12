using CSharpSamplesCutter.Core;

namespace CSharpSamplesCutter.Forms.MainWindow.ViewModels
{
    /// <summary>
    ///  Enthält temporäre Zustände zur Selektion in der Hauptansicht.
    /// </summary>
    internal sealed class SelectionViewModel
    {
        public AudioObj? LastSelectedTrack { get; set; }

        public int? AnchorIndexMain { get; set; }

        public int? AnchorIndexReserve { get; set; }

        public int StepsBack { get; set; }

        public string SelectionMode { get; set; } = "Select";

        public bool IsSelecting { get; set; }
    }
}
