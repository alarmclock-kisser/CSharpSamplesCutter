using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSharpSamplesCutter.Core;
using CSharpSamplesCutter.Core.Processors_V2;

namespace CSharpSamplesCutter.Forms
{
    public partial class WindowMain
    {
        private void InitializeAdvancedProcessing()
        {
            // ComboBox mit allen Processors_V2 Methoden befüllen
            this.PopulateAdvancedProcessingComboBox();

            // Event-Handler registrieren
            this.comboBox_advancedProcessing.SelectedIndexChanged += this.ComboBox_advancedProcessing_SelectedIndexChanged;
            this.button_advancedProcessingGo.Click += this.Button_advancedProcessingGo_Click;
        }

        private void PopulateAdvancedProcessingComboBox()
        {
            this.comboBox_advancedProcessing.Items.Clear();

            // Alle statischen Klassen aus Processors_V2 namespace durchsuchen
            var processorTypes = Assembly.GetAssembly(typeof(AutoDrumLooper))!
                .GetTypes()
                .Where(t => t.Namespace == "CSharpSamplesCutter.Core.Processors_V2" && 
                            t.IsClass && 
                            t.IsAbstract && 
                            t.IsSealed) // static class
                .ToList();

            foreach (var processorType in processorTypes)
            {
                // Alle public static Methoden der Klasse
                var methods = processorType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => !m.IsSpecialName && m.DeclaringType == processorType)
                    .ToList();

                foreach (var method in methods)
                {
                    // Nur Methodenname anzeigen, Async am Ende entfernen
                    var methodName = method.Name;
                    if (methodName.EndsWith("Async", StringComparison.Ordinal))
                        methodName = methodName.Substring(0, methodName.Length - 5);
                    // CamelCase/PascalCase in Worte mit Großbuchstaben am Anfang umwandeln
                    var displayName = System.Text.RegularExpressions.Regex.Replace(
                        methodName,
                        "([a-z])([A-Z])",
                        "$1 $2"
                    );
                    displayName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(displayName);
                    this.comboBox_advancedProcessing.Items.Add(new AdvancedProcessingItem
                    {
                        DisplayName = displayName,
                        ProcessorType = processorType,
                        Method = method
                    });
                }
            }

            this.comboBox_advancedProcessing.DisplayMember = nameof(AdvancedProcessingItem.DisplayName);

            if (this.comboBox_advancedProcessing.Items.Count > 0)
            {
                this.comboBox_advancedProcessing.SelectedIndex = 0;
            }
        }

        private void ComboBox_advancedProcessing_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Optional: Hier könnten Parameter-Infos angezeigt werden
        }

        private async void Button_advancedProcessingGo_Click(object? sender, EventArgs e)
        {
            if (this.comboBox_advancedProcessing.SelectedItem is not AdvancedProcessingItem item)
            {
                MessageBox.Show("Bitte wählen Sie eine Verarbeitungsmethode aus.", "Erweiterte Verarbeitung", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                this.button_advancedProcessingGo.Enabled = false;
                this.Cursor = Cursors.WaitCursor;

                // Bestimme Quell- und Ziel-Collection
                bool sourceIsMainList = this.SelectedCollectionListBox == this.listBox_audios;
                var sourceCollection = sourceIsMainList ? this.AudioC : this.AudioC_res;
                var targetCollection = sourceIsMainList ? this.AudioC_res : this.AudioC;

                // Bereite Input-Parameter vor
                var parameters = item.Method.GetParameters();
                object?[] args = await this.PrepareMethodArgumentsAsync(parameters, sourceCollection);

                if (args == null)
                {
                    MessageBox.Show("Die gewählte Methode konnte nicht mit den aktuellen Samples ausgeführt werden.", "Erweiterte Verarbeitung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Methode aufrufen
                object? result = item.Method.Invoke(null, args);

                // Async-Ergebnis awaiten falls Task
                if (result is Task task)
                {
                    await task; // ConfigureAwait(false) entfernt

                    // Extrahiere Result von Task<T>
                    var taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        var resultProperty = taskType.GetProperty("Result");
                        result = resultProperty?.GetValue(task);
                    }
                    else
                    {
                        result = null;
                    }
                }

                // Ergebnis(se) zur Ziel-Collection hinzufügen
                await this.AddResultsToTargetCollectionAsync(result, targetCollection, sourceIsMainList);

                LogCollection.Log($"Erweiterte Verarbeitung abgeschlossen: {item.DisplayName}");
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Fehler bei der Verarbeitung:\n\n{innerMsg}", "Erweiterte Verarbeitung", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogCollection.Log($"Fehler bei erweiterter Verarbeitung: {innerMsg}");
            }
            finally
            {
                this.Cursor = Cursors.Default;
                this.button_advancedProcessingGo.Enabled = true;

                // Fokus auf die aktive ListBox setzen, damit Tastatursteuerung wieder funktioniert
                if (this.SelectedCollectionListBox is ListBox lb)
                    lb.Focus();
            }
        }

        private async Task<object?[]?> PrepareMethodArgumentsAsync(ParameterInfo[] parameters, AudioCollection sourceCollection)
        {
            if (parameters.Length == 0)
            {
                return Array.Empty<object?>();
            }

            var args = new List<object?>();

            foreach (var param in parameters)
            {
                var paramType = param.ParameterType;

                // IEnumerable<AudioObj> oder AudioObj[]
                if (paramType == typeof(IEnumerable<AudioObj>) || 
                    paramType == typeof(AudioObj[]) ||
                    paramType == typeof(List<AudioObj>) ||
                    paramType == typeof(ICollection<AudioObj>))
                {
                    // Mehrere Samples: ausgewählte GUIDs oder alle aus aktiver ListBox
                    var selectedAudios = this.SelectedGuids
                        .Select(id => this.AudioC.Audios.FirstOrDefault(a => a.Id == id) ?? this.AudioC_res.Audios.FirstOrDefault(a => a.Id == id))
                        .Where(a => a != null)
                        .Cast<AudioObj>()
                        .ToList();

                    if (selectedAudios.Count == 0)
                    {
                        // Fallback: alle aus Source-Collection
                        selectedAudios = sourceCollection.Audios.ToList();
                    }

                    if (selectedAudios.Count == 0)
                    {
                        return null; // Keine Samples verfügbar
                    }

                    // Cast zu entsprechendem Typ
                    if (paramType == typeof(AudioObj[]))
                    {
                        args.Add(selectedAudios.ToArray());
                    }
                    else if (paramType == typeof(List<AudioObj>))
                    {
                        args.Add(selectedAudios);
                    }
                    else
                    {
                        args.Add(selectedAudios.AsEnumerable());
                    }
                }
                // Einzelnes AudioObj
                else if (paramType == typeof(AudioObj))
                {
                    var track = this.SelectedTrack;
                    if (track == null)
                    {
                        return null; // Kein Sample ausgewählt
                    }
                    args.Add(track);
                }
                // Optionale Parameter mit Defaults
                else if (param.HasDefaultValue)
                {
                    args.Add(param.DefaultValue);
                }
                // Nullable-Typen mit null
                else if (Nullable.GetUnderlyingType(paramType) != null)
                {
                    args.Add(null);
                }
                else
                {
                    // Unbekannter Parameter-Typ, versuche Default
                    args.Add(paramType.IsValueType ? Activator.CreateInstance(paramType) : null);
                }
            }

            return args.ToArray();
        }

        private async Task AddResultsToTargetCollectionAsync(object? result, AudioCollection targetCollection, bool sourceIsMainList)
        {
            if (result == null)
            {
                return;
            }

            var targetListBox = sourceIsMainList ? this.listBox_reserve : this.listBox_audios;

            await this.Invoke(async () =>
            {
                // Einzelnes AudioObj
                if (result is AudioObj single)
                {
                    targetCollection.Audios.Add(single);
                    targetListBox.SelectedIndex = -1;
                    targetListBox.SelectedIndex = targetListBox.Items.Count - 1;
                }
                // IEnumerable<AudioObj>
                else if (result is IEnumerable<AudioObj> multiple)
                {
                    var list = multiple.ToList();
                    if (list.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            targetCollection.Audios.Add(item);
                        }
                        targetListBox.SelectedIndex = -1;
                        targetListBox.SelectedIndex = targetListBox.Items.Count - 1;
                    }
                }

                await Task.CompletedTask;
            });
        }

        private class AdvancedProcessingItem
        {
            public string DisplayName { get; set; } = string.Empty;
            public Type ProcessorType { get; set; } = default!;
            public MethodInfo Method { get; set; } = default!;
        }
    }
}
