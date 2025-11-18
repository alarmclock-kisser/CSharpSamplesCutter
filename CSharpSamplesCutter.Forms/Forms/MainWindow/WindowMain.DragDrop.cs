using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private void WindowMain_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void WindowMain_DragDrop(object? sender, DragEventArgs e)
        {
            try
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                {
                    return;
                }

                var dropped = (string[]?) e.Data.GetData(DataFormats.FileDrop);
                if (dropped == null || dropped.Length == 0)
                {
                    return;
                }

                Point clientPoint = this.PointToClient(new Point(e.X, e.Y));
                bool overMain = this.listBox_audios.Bounds.Contains(clientPoint);
                bool overReserve = this.listBox_reserve.Bounds.Contains(clientPoint);

                if (overMain)
                {
                    await this.ImportDroppedItemsAsync(dropped, this.AudioC);
                }
                else if (overReserve)
                {
                    await this.ImportDroppedItemsAsync(dropped, this.AudioC_res);
                }
                else
                {
                    await this.ImportDroppedItemsAsync(dropped, this.AudioC);
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log($"Drop: Fehler beim Import: {ex.Message}");
            }
        }

        private void ListBox_MouseDown_Drag(object? sender, MouseEventArgs e)
        {
            if (sender is not ListBox lb)
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (!ModifierKeys.HasFlag(Keys.Control) && lb.SelectedIndices.Count == 1)
            {
                int idx = lb.IndexFromPoint(e.Location);
                if (idx >= 0 && lb.SelectedIndex == idx)
                {
                    this.IsDragInitiated = true;
                    this.DragStartIndex = idx;
                    this.DragSourceListBox = lb;
                }
            }
            else
            {
                this.IsDragInitiated = false;
                this.DragStartIndex = -1;
                this.DragSourceListBox = null;
            }
        }

        private void ListBox_MouseMove_Drag(object? sender, MouseEventArgs e)
        {
            if (!this.IsDragInitiated || this.DragSourceListBox == null)
            {
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            this.IsDragInitiated = false;
            var item = this.DragSourceListBox.SelectedItem;
            if (item != null)
            {
                this.DragSourceListBox!.DoDragDrop(item, DragDropEffects.Move);
            }
        }

        private void ListBox_DragOver(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox lb)
            {
                return;
            }

            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
                return;
            }

            if (!ModifierKeys.HasFlag(Keys.Control) && e.Data?.GetDataPresent(typeof(AudioObj)) == true)
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private async void ListBox_DragDrop(object? sender, DragEventArgs e)
        {
            if (sender is not ListBox targetListBox)
            {
                return;
            }

            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var dropped = (string[]?) e.Data.GetData(DataFormats.FileDrop);
                if (dropped == null || dropped.Length == 0)
                {
                    return;
                }

                var targetCollection = targetListBox == this.listBox_reserve ? this.AudioC_res : this.AudioC;
                await this.ImportDroppedItemsAsync(dropped, targetCollection);
                return;
            }

            if (this.DragSourceListBox == null)
            {
                return;
            }

            if (e.Data?.GetDataPresent(typeof(AudioObj)) == false)
            {
                return;
            }

            if (ModifierKeys.HasFlag(Keys.Control))
            {
                return;
            }

            Point clientPoint = targetListBox.PointToClient(new Point(e.X, e.Y));
            int dropIndex = targetListBox.IndexFromPoint(clientPoint);
            if (dropIndex < 0)
            {
                dropIndex = targetListBox.Items.Count - 1;
            }

            if (targetListBox == this.DragSourceListBox && this.DragStartIndex >= 0 && dropIndex >= 0 && targetListBox.SelectedIndices.Count == 1)
            {
                bool isMain = targetListBox == this.listBox_audios;
                if (isMain)
                {
                    int skip = this.SkipTracks;
                    int actualFrom = skip + this.DragStartIndex;
                    int actualTo = skip + dropIndex;
                    ReorderInCollection(this.AudioC.Audios, actualFrom, actualTo);
                    this.RebindAudioListForSkip();
                    targetListBox.SelectedIndex = Math.Min(dropIndex, targetListBox.Items.Count - 1);
                }
                else
                {
                    ReorderInCollection(this.AudioC_res.Audios, this.DragStartIndex, dropIndex);
                    targetListBox.SelectedIndex = Math.Min(dropIndex, targetListBox.Items.Count - 1);
                }
            }

            this.DragSourceListBox = null;
            this.DragStartIndex = -1;
            this.IsDragInitiated = false;
        }

        private static void ReorderInCollection(IList<AudioObj> list, int from, int to)
        {
            if (from == to)
            {
                return;
            }

            if (from < 0 || to < 0 || from >= list.Count || to >= list.Count)
            {
                return;
            }

            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
        }

        private async Task ImportDroppedItemsAsync(IEnumerable<string> paths, AudioCollection targetCollection)
        {
            if (paths == null)
            {
                return;
            }
            var distinctPaths = paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToArray();
            if (distinctPaths.Length == 0)
            {
                return;
            }

            List<string> fileCandidates = [];
            foreach (var p in distinctPaths)
            {
                try
                {
                    if (File.Exists(p))
                    {
                        fileCandidates.Add(p);
                    }
                    else if (Directory.Exists(p))
                    {
                        var files = Directory.EnumerateFiles(p)
                            .Where(this.IsAudioFileExtension)
                            .ToArray();
                        fileCandidates.AddRange(files);
                    }
                }
                catch (Exception ex)
                {
                    LogCollection.Log($"Drop: Zugriff auf Pfad fehlgeschlagen: {p} ({ex.Message})");
                }
            }

            if (fileCandidates.Count == 0)
            {
                LogCollection.Log("Drop: Keine validen Audio-Dateien gefunden.");
                return;
            }

            var loadTasks = fileCandidates.Select(async file =>
            {
                try
                {
                    var verified = AudioCollection.VerifyAudioFile(file);
                    if (verified == null)
                    {
                        return false;
                    }
                    var audio = await AudioObj.FromFileAsync(verified);
                    if (audio == null)
                    {
                        return false;
                    }
                    if (!targetCollection.Audios.Any(a => string.Equals(a.DisplayName, audio.DisplayName, StringComparison.OrdinalIgnoreCase)))
                    {
                        targetCollection.Audios.Add(audio);
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    LogCollection.Log($"Drop: Fehler beim Laden: {file} ({ex.Message})");
                    return false;
                }
            });

            var results = await Task.WhenAll(loadTasks);
            int added = results.Count(r => r);

            if (added > 0)
            {
                LogCollection.Log($"Drop: {added} Audio-Datei(en) importiert in {(ReferenceEquals(targetCollection, this.AudioC) ? "Main" : "Reserve")}.");
                if (ReferenceEquals(targetCollection, this.AudioC))
                {
                    this.RebindAudioListForSkip();
                }
                else
                {
                    this.listBox_reserve.Refresh();
                }
            }
            else
            {
                LogCollection.Log("Drop: Keine neuen Audio-Dateien importiert (evtl. alle bereits vorhanden).");
            }
        }

        private bool IsAudioFileExtension(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                return ext is ".wav" or ".mp3" or ".flac";
            }
            catch
            {
                return false;
            }
        }
    }
}
