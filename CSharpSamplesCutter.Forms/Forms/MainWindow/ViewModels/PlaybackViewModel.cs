using System.Collections.Concurrent;

namespace CSharpSamplesCutter.Forms.MainWindow.ViewModels
{
    /// <summary>
    ///  Modelliert Playback-bezogene UI-Zustände und Tokens.
    /// </summary>
    internal sealed class PlaybackViewModel
    {
        public bool LoopEnabled { get; set; }

        public ConcurrentDictionary<Guid, CancellationToken> CancellationTokens { get; } = new();

        public DateTime LastSpaceToggleUtc { get; set; } = DateTime.MinValue;

        public bool SpaceKeyDebounceActive { get; set; }
    }
}
