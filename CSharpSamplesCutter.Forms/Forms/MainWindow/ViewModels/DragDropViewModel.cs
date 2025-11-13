using System.Windows.Forms;

namespace CSharpSamplesCutter.Forms.Forms.MainWindow.ViewModels
{
    /// <summary>
    ///  Verwaltet Status für Drag&Drop-Operationen zwischen den Listen.
    /// </summary>
    internal sealed class DragDropViewModel
    {
        public bool IsDragInitiated { get; set; }

        public int DragStartIndex { get; set; } = -1;

        public ListBox? SourceListBox { get; set; }
    }
}
