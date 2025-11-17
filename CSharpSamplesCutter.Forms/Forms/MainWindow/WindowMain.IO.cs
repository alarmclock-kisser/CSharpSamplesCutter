using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;
using Dialogs = CSharpSamplesCutter.Forms.Dialogs;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private async void button_load_Click(object sender, EventArgs e)
        {
            int selectedIndex = this.listBox_audios.SelectedIndex;
            int oldCount = this.AudioC.Audios.Count;

            // Alt-click load random resource audio file
            if (ModifierKeys.HasFlag(Keys.Alt))
            {
                var resourceAudios = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources"), "*.*", SearchOption.AllDirectories)
                    .Select(AudioCollection.VerifyAudioFile)
                    .Where(path => path != null)
                    .ToList();

                if (resourceAudios.Count <= 0)
                {
                    LogCollection.Log("No valid resource audio files found.");
                    return;
				}

                var random = new Random();
                var randomIndex = random.Next(0, resourceAudios.Count);
                var randomFilePath = resourceAudios[randomIndex]!;
                var audioObj = await this.AudioC.LoadAsync(randomFilePath);
				if (audioObj == null)
                {
                    LogCollection.Log($"Failed to load audio file: {randomFilePath}");
                    return;
                }

                this.listBox_audios.SelectedIndex = -1;
                this.listBox_audios.SelectedIndex = Math.Clamp(selectedIndex, -1, this.listBox_audios.Items.Count - 1);
                LogCollection.Log($"Loaded random resource audio sample: {audioObj.Name}");

                // Nach Import letzten Track selektieren und UI aktualisieren
                this.SelectTrackAndUpdateUI(audioObj);
                return;
            }

            if (ModifierKeys.HasFlag(Keys.Control) & !ModifierKeys.HasFlag(Keys.Shift))
            {
                using (var loadDialog = new Dialogs.LoadDialog())
                {
                    var result = loadDialog.ShowDialog(this);
                    if (result == DialogResult.OK)
                    {
                        var results = loadDialog.Results;
                        foreach (var item in results)
                        {
                            this.AudioC.Audios.Add(item);
                        }

                        this.listBox_audios.Refresh();
                        this.listBox_audios.SelectedIndex = -1;
                        this.listBox_audios.SelectedIndex = Math.Clamp(selectedIndex, -1, this.listBox_audios.Items.Count - 1);
                        // Nach Import letzten Track selektieren und UI aktualisieren
                        if (results.Count > 0)
                            this.SelectTrackAndUpdateUI(results.Last());
                    }
                }
            }
            else if (ModifierKeys.HasFlag(Keys.Shift) & !ModifierKeys.HasFlag(Keys.Control))
            {
                using var fbd = new FolderBrowserDialog()
                {
                    Description = "Select Directory Containing Audio Files",
                    UseDescriptionForTitle = true,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
                };
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    var dirPath = fbd.SelectedPath;
                    var loadedAudioObjs = await this.AudioC.LoadDirectoryAsync(dirPath);
                    int loadedCount = loadedAudioObjs.Count(a => a != null);
                    this.listBox_audios.SelectedIndex = -1;
                    this.listBox_audios.SelectedIndex = this.listBox_audios.Items.Count - 1;
                    LogCollection.Log($"{loadedCount} audio samples loaded from directory.");
                    // Nach Import letzten Track selektieren und UI aktualisieren
                    var last = loadedAudioObjs.LastOrDefault(a => a != null);
                    if (last != null)
                        this.SelectTrackAndUpdateUI(last);
                }
            }
            else
            {
                string initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

                if (ModifierKeys.HasFlag(Keys.Shift) && ModifierKeys.HasFlag(Keys.Control))
                {
                    initialDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                }

                using var ofd = new OpenFileDialog()
                {
                    Filter = "Audio Files|*.wav;*.mp3;*.flac",
                    Title = "Select Audio File(s)",
                    Multiselect = true,
                    InitialDirectory = initialDir,
                    RestoreDirectory = true
                };
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    int loadedCount = 0;
                    AudioObj? lastLoaded = null;
                    foreach (var filePath in ofd.FileNames)
                    {
                        var verifiedPath = AudioCollection.VerifyAudioFile(filePath);
                        if (verifiedPath != null)
                        {
                            var audioObj = await AudioObj.FromFileAsync(verifiedPath);
                            if (audioObj == null)
                            {
                                LogCollection.Log($"Failed to load audio file: {filePath}");
                                continue;
                            }

                            this.AudioC.Audios.Add(audioObj);
                            loadedCount++;
                            lastLoaded = audioObj;
                        }
                        else
                        {
                            LogCollection.Log($"Unsupported audio format: {filePath}");
                        }
                    }

                    this.listBox_audios.SelectedIndex = -1;
                    this.listBox_audios.SelectedIndex = this.listBox_audios.Items.Count - 1;
                    LogCollection.Log($"{loadedCount} audio samples loaded.");
                    // Nach Import letzten Track selektieren und UI aktualisieren
                    if (lastLoaded != null)
                        this.SelectTrackAndUpdateUI(lastLoaded);
                }
            }
        }

        private async void button_export_Click(object sender, EventArgs e)
        {
            string exportDirectory = Path.GetFullPath(this.AudioC.ExportPath);
            string batchExportPath = exportDirectory;
            if (this.SelectedGuids.Count > 1)
            {
                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    string firstTrackName = (this.SelectedTrack ?? this.AudioC.Audios.FirstOrDefault(a => a.Id == this.SelectedGuids.First()))?.Name ?? "export";
                    string safeTrackName = string.Concat(firstTrackName.Take(16).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                    string batchDirName = $"batch_{safeTrackName}_{this.SelectedGuids.Count:D3}";
                    batchExportPath = Path.Combine(exportDirectory, batchDirName);
                    Directory.CreateDirectory(batchExportPath);
                }

                var exportTasks = this.SelectedGuids.Select(async id =>
                {
                    var track = this.AudioC[id] ?? this.AudioC_res[id];
                    if (track != null)
                    {
                        string? exportedPath = await this.AudioC.Exporter.ExportWavAsync(track, 24, batchExportPath);
                        if (string.IsNullOrEmpty(exportedPath))
                        {
                            LogCollection.Log($"Failed to export audio sample: {track.Name}");
                        }
                        else
                        {
                            LogCollection.Log($"Exported audio sample: {track.Name} to {exportedPath}");
                        }
                    }
                });

                await Task.WhenAll(exportTasks);
            }
            else
            {
                var track = this.SelectedTrack;
                if (track == null)
                {
                    return;
                }

                string? exportFilePath = Path.Combine(exportDirectory, $"{track.Name}_export.wav");
                if (ModifierKeys.HasFlag(Keys.Control))
                {
                    using var sfd = new SaveFileDialog()
                    {
                        InitialDirectory = exportDirectory,
                        Filter = "WAV File|*.wav",
                        Title = "Select Export File Path",
                        FileName = $"{track.Name}_export.wav"
                    };
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        exportFilePath = sfd.FileName;
                        exportDirectory = Path.GetDirectoryName(exportFilePath) ?? exportDirectory;
                    }
                    else
                    {
                        return;
                    }
                }

                exportFilePath = await this.AudioC.Exporter.ExportWavAsync(track, 24, exportDirectory);
                if (string.IsNullOrEmpty(exportFilePath))
                {
                    LogCollection.Log($"Failed to export audio sample: {track.Name}");
                }
            }
        }

        private async void button_reload_Click(object sender, EventArgs e)
        {
            int selectedIndex = this.listBox_audios.SelectedIndex;
            var track = this.SelectedTrack;
            if (track == null)
            {
                return;
            }

            await Task.Run(track.LoadAudioFile);
            LogCollection.Log($"Reloaded audio sample: {track.Name}");

            this.listBox_audios.SelectedIndex = -1;
            this.listBox_audios.SelectedIndex = Math.Clamp(selectedIndex, -1, this.listBox_audios.Items.Count - 1);
        }

        private async void button_remove_Click(object sender, EventArgs e)
        {
            ListBox? active = this.listBox_audios.SelectedItems.Count > 0
                ? this.listBox_audios
                : (this.listBox_reserve.SelectedItems.Count > 0 ? this.listBox_reserve : null);

            if (active == null)
            {
                LogCollection.Log("No audio samples selected to remove.");
                return;
            }

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                if (active == this.listBox_audios)
                {
                    await this.AudioC.ClearAsync();
                    this.RebindAudioListForSkip();
                }
                else
                {
                    await this.AudioC_res.ClearAsync();
                    active.Refresh();
                }
            }

            var selectedAudioObjs = active.SelectedItems.Cast<AudioObj>().ToList();
            if (selectedAudioObjs.Count == 0)
            {
                LogCollection.Log("No audio samples selected to remove.");
                return;
            }

            int rememberedIndex = selectedAudioObjs.Count == 1 ? active.SelectedIndex : -1;

            var tasks = selectedAudioObjs.Select(async track =>
            {
                bool inMain = this.AudioC.Audios.Any(a => a.Id == track.Id);
                bool inReserve = this.AudioC_res.Audios.Any(a => a.Id == track.Id);

                if (inMain)
                {
                    await this.AudioC.RemoveAsync(track.Id);
                    if (this.AudioC.Audios.Any(a => a.Id == track.Id))
                    {
                        LogCollection.Log($"Failed to remove audio sample: {track.Name}");
                    }
                    else
                    {
                        LogCollection.Log($"Removed audio sample: {track.Name}");
                    }
                }
                else if (inReserve)
                {
                    await this.AudioC_res.RemoveAsync(track.Id);
                    if (this.AudioC_res.Audios.Any(a => a.Id == track.Id))
                    {
                        LogCollection.Log($"Failed to remove audio sample: {track.Name}");
                    }
                    else
                    {
                        LogCollection.Log($"Removed audio sample: {track.Name}");
                    }
                }
            });

            await Task.WhenAll(tasks);

            if (active == this.listBox_audios)
            {
                this.RebindAudioListForSkip();
            }
            else
            {
                active.Refresh();
            }

            if (rememberedIndex >= 0 && active.Items.Count > 0)
            {
                active.SelectedIndex = Math.Min(rememberedIndex, active.Items.Count - 1);
            }

            this.UpdateViewingElements();
        }

        private async void button_move_Click(object sender, EventArgs e)
        {
            var guidsToMove = this.SelectedGuids;

            if (guidsToMove == null || guidsToMove.Count == 0)
            {
                LogCollection.Log("No audio samples selected to move.");
                return;
            }

            var moveTasks = guidsToMove.Select(async id =>
            {
                var trackToMove = this.AudioC.Audios.FirstOrDefault(a => a.Id == id) ?? this.AudioC_res.Audios.FirstOrDefault(a => a.Id == id);

                if (trackToMove == null)
                {
                    LogCollection.Log($"Selected track not found for id: {id}");
                    return;
                }

                if (this.AudioC.Audios.Any(a => a.Id == id))
                {
                    this.AudioC_res.Audios.Add(await trackToMove.CloneAsync());
                    await this.AudioC.RemoveAsync(trackToMove.Id);
                    LogCollection.Log($"Moved audio sample to reserve list: {trackToMove.Name}");
                }
                else if (this.AudioC_res.Audios.Any(a => a.Id == id))
                {
                    this.AudioC.Audios.Add(await trackToMove.CloneAsync());
                    await this.AudioC_res.RemoveAsync(trackToMove.Id);
                    LogCollection.Log($"Moved audio sample to main list: {trackToMove.Name}");
                }
                else
                {
                    LogCollection.Log($"Selected track not found in either list: {trackToMove.Name}");
                }

                await Task.Yield();
            });

            await Task.WhenAll(moveTasks);
        }

        private void numericUpDown_skipTracks_ValueChanged(object? sender, EventArgs e)
        {
            this.numericUpDown_skipTracks.Maximum = this.AudioC.Audios.Count;
            if (this.numericUpDown_skipTracks.Value > this.numericUpDown_skipTracks.Maximum)
            {
                this.numericUpDown_skipTracks.Value = this.numericUpDown_skipTracks.Maximum;
            }
            this.RebindAudioListForSkip();
        }
    }
}
