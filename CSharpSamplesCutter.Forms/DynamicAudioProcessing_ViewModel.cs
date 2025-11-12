using CSharpSamplesCutter.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;

namespace CSharpSamplesCutter.Forms
{
    internal class DynamicAudioProcessing_ViewModel
    {
        private readonly Form MainWindow;
        internal readonly ComboBox ComboBox_functions;
        internal readonly Button Button_go;
        internal readonly Panel Panel_parameters;
        internal readonly CheckBox CheckBox_autoParameters;
        internal readonly CheckBox CheckBox_optionalParameters;

        private AudioObj? SelectedTrack => this.MainWindow is WindowMain win ? win.SelectedTrack : null;
        private IEnumerable<AudioCollection> SelectedCollections => this.MainWindow is WindowMain win ? [win.AudioC, win.AudioC_res] : [];

        internal readonly float InputWidthRatio;
        internal int PanelWidth => this.Panel_parameters.Width;
        internal int PanelHeight => this.Panel_parameters.Height;
        internal int InputHeight { get; set; } = 23;
        internal int Margin { get; set; } = 5;
        internal int InputWidth => (int) (this.PanelWidth * this.InputWidthRatio) - this.Margin * 2;
        internal int InputX => (int) (this.PanelWidth * (1 - this.InputWidthRatio)) + this.Margin;
        internal int LabelWidth => (int) (this.PanelWidth * (1 - this.InputWidthRatio)) - this.Margin * 2;
        internal int LabelX => this.Margin;

        private int DrawingY => this.Margin;

        // Methods list that backs the ComboBox
        public readonly BindingList<MethodInfo> MethodsInfoList = [];

        // Keep controls created for parameters. Key is a unique IEnumerable<Type> instance per parameter (so reference equality works).
        internal Dictionary<IEnumerable<Type>, Control> ParameterControlsMap = [];

        internal DynamicAudioProcessing_ViewModel(Form window, ComboBox comboBox_functions, Button button_go, Panel panel_Parameters, CheckBox checkBox_autoParameters, CheckBox checkBox_optionalParameters, float inputWidthRatio = 0.32f)
        {
            this.MainWindow = window;
            this.ComboBox_functions = comboBox_functions ?? throw new ArgumentNullException(nameof(comboBox_functions));
            this.Button_go = button_go ?? throw new ArgumentNullException(nameof(button_go));
            this.Panel_parameters = panel_Parameters ?? throw new ArgumentNullException(nameof(panel_Parameters));
            this.CheckBox_autoParameters = checkBox_autoParameters ?? throw new ArgumentNullException(nameof(checkBox_autoParameters));
            this.CheckBox_optionalParameters = checkBox_optionalParameters ?? throw new ArgumentNullException(nameof(checkBox_optionalParameters));
            this.InputWidthRatio = inputWidthRatio;

            // Setup ComboBox datasource
            this.ComboBox_functions.DataSource = this.MethodsInfoList;
            this.ComboBox_functions.FormattingEnabled = true; // wichtig damit Format-Event angewendet wird
            this.ComboBox_functions.Format += (s, e) =>
            {
                if (e.ListItem is MethodInfo mi)
                {
                    var name = mi.Name ?? string.Empty;
                    const string asyncSuffix = "Async";
                    if (name.EndsWith(asyncSuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        name = name.Substring(0, name.Length - asyncSuffix.Length);
                    }
                    e.Value = name;
                }
                else
                {
                    e.Value = e.ListItem?.ToString() ?? string.Empty;
                }
            };

            // Events
            this.ComboBox_functions.SelectedIndexChanged += (s, e) =>
            {
                MethodInfo? mi = this.ComboBox_functions.SelectedItem as MethodInfo;
                if (mi != null)
                {
                    this.BuildParametersPanel(mi.Name);
                }
            };

            this.CheckBox_autoParameters.CheckedChanged += (s, e) =>
            {
                this.RegisterCheckBox_RebuildPanelParameters();
            };

            this.CheckBox_optionalParameters.CheckedChanged += (s, e) =>
            {
                this.RegisterCheckBox_RebuildPanelParameters();
            };

            // Async click handler for the Go button
            this.Button_go.Click += async (s, e) =>
            {
                await this.InvokeSelectedMethodAsync().ConfigureAwait(false);
            };

            this.RegisterProcessingMethods();

            this.ComboBox_functions.SelectedIndex = -1;
            this.ComboBox_functions.SelectedIndex = this.ComboBox_functions.Items.Count > 0 ? 0 : -1;
        }

        private void RegisterProcessingMethods()
        {
            // Register functions
            this.RegisterMethodByName<AudioObj>("NormalizeAsync");
            this.RegisterMethodByName<AudioObj>("TrimSilenceAsync");
            this.RegisterMethodByName<AudioObj>("FadeInAsync");
            this.RegisterMethodByName<AudioObj>("FadeOutAsync");
            this.RegisterMethodByName<AudioCollection>("MergeSimilarAudiosAsync");
        }

        public bool RegisterMethodByName<T>(string methodName) where T : class
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                return false;
            }

            try
            {
                Type targetType = typeof(T);

                // public instance and static methods
                var infos = targetType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                var matches = infos.Where(m => m.Name.IndexOf(methodName, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (matches.Length == 0)
                {
                    return false;
                }

                foreach (var mi in matches)
                {
                    // Avoid duplicates
                    if (!this.MethodsInfoList.Any(existing => MethodEquals(existing, mi)))
                    {
                        this.MethodsInfoList.Add(mi);
                    }
                }

                // If there is at least one method, select first
                if (this.MethodsInfoList.Count > 0 && this.ComboBox_functions.SelectedItem == null)
                {
                    this.ComboBox_functions.SelectedItem = this.MethodsInfoList[0];
                    this.BuildParametersPanel(this.MethodsInfoList[0].Name);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
                return false;
            }

            static bool MethodEquals(MethodInfo a, MethodInfo b)
            {
                if (a == null || b == null)
                {
                    return false;
                }

                if (!object.ReferenceEquals(a.DeclaringType, b.DeclaringType))
                {
                    return false;
                }

                if (a.Name != b.Name)
                {
                    return false;
                }

                var pa = a.GetParameters();
                var pb = b.GetParameters();
                if (pa.Length != pb.Length)
                {
                    return false;
                }

                for (int i = 0; i < pa.Length; i++)
                {
                    if (pa[i].ParameterType != pb[i].ParameterType)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private Dictionary<IEnumerable<Type>, Control> CreateParameterControlsMap(string methodName)
        {
            MethodInfo? methodInfo = this.MethodsInfoList.FirstOrDefault(m => m.Name == methodName);
            if (methodInfo == null)
            {
                LogCollection.Log($"Method '{methodName}' not found.");
                return [];
            }

            Dictionary<IEnumerable<Type>, Control> parameterControlsMap = [];
            ParameterInfo[] parameters = methodInfo.GetParameters();

            // If the checkbox hides optional parameters, then we remove optional ones from the list
            if (!this.CheckBox_optionalParameters.Checked)
            {
                // If checkbox is unchecked -> do NOT show optional parameters
                parameters = parameters.Where(p => !p.IsOptional).ToArray();
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                // Skip AudioObj parameters - those should be provided from SelectedTrack automatically
                if (IsAudioObjParameter(parameter.ParameterType))
                {
                    continue;
                }

                Control? control = this.CreateControlForParameterType(parameter.ParameterType, i);
                if (control != null)
                {
                    // Position will be set later by BuildParametersPanel; Name and Tag store useful info
                    control.Name = $"param_{i}";
                    control.Tag = parameter; // store ParameterInfo directly

                    // Use a unique IEnumerable<Type> instance for the key: a single-element array.
                    var key = new Type[] { parameter.ParameterType };
                    parameterControlsMap.Add(key, control);
                }
            }

            return parameterControlsMap;
        }

        private static bool IsAudioObjParameter(Type t)
        {
            if (t == null)
            {
                return false;
            }

            Type audioType = typeof(AudioObj);
            if (t == audioType)
            {
                return true;
            }
            // consider nullable and derived types
            var underlying = Nullable.GetUnderlyingType(t) ?? t;
            return audioType.IsAssignableFrom(underlying);
        }

        private static bool IsAudioCollectionParameter(Type t)
        {
            if (t == null)
            {
                return false;
            }

            Type audioCollectionType = typeof(AudioCollection);
            if (t == audioCollectionType)
            {
                return true;
            }
            // consider nullable and derived types
            var underlying = Nullable.GetUnderlyingType(t) ?? t;
            return audioCollectionType.IsAssignableFrom(underlying);
        }

        private Control? CreateControlForParameterType(Type parameterType, int position)
        {
            // Do not create controls for AudioObj parameters; those come from SelectedTrack
            if (IsAudioObjParameter(parameterType))
            {
                return null;
            }

            // NumericUpDown for integer-like types
            if (parameterType == typeof(int) || parameterType == typeof(long) || parameterType == typeof(int?) || parameterType == typeof(long?))
            {
                var nud = new NumericUpDown()
                {
                    Minimum = decimal.MinValue,
                    Maximum = decimal.MaxValue,
                    Value = 0,
                    Increment = 1,
                    DecimalPlaces = 0,
                    Height = this.InputHeight
                };
                return nud;
            }
            // float/double
            else if (parameterType == typeof(float) || parameterType == typeof(double) || parameterType == typeof(float?) || parameterType == typeof(double?))
            {
                var nud = new NumericUpDown()
                {
                    Minimum = decimal.MinValue / 1000,
                    Maximum = decimal.MaxValue / 1000,
                    Value = 0,
                    Increment = 0.05m,
                    DecimalPlaces = 5,
                    Height = this.InputHeight
                };
                return nud;
            }
            else if (parameterType == typeof(string))
            {
                var tb = new TextBox()
                {
                    Height = this.InputHeight
                };
                return tb;
            }
            else if (parameterType == typeof(bool))
            {
                var cb = new CheckBox()
                {
                    Height = this.InputHeight
                };
                return cb;
            }
            else if (parameterType.IsEnum)
            {
                var combo = new ComboBox()
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Height = this.InputHeight
                };
                combo.Items.AddRange(Enum.GetNames(parameterType));
                if (combo.Items.Count > 0)
                {
                    combo.SelectedIndex = 0;
                }

                return combo;
            }
            else
            {
                // Fallback: use TextBox so user can type something. Later we will try to convert.
                var tb = new TextBox()
                {
                    Height = this.InputHeight
                };
                return tb;
            }
        }

        private void BuildParametersPanel(string methodName)
        {
            try
            {
                // Clear old
                this.Panel_parameters.Controls.Clear();
                this.ParameterControlsMap.Clear();

                MethodInfo? methodInfo = this.MethodsInfoList.FirstOrDefault(m => m.Name == methodName);
                if (methodInfo == null)
                {
                    return;
                }

                ParameterInfo[] parameters = methodInfo.GetParameters();

                if (!this.CheckBox_autoParameters.Checked)
                {
                    // hide parameters that are nullable and will be set in method if null
                    parameters = parameters.Where(p => !(Nullable.GetUnderlyingType(p.ParameterType) != null)).ToArray();
                }
                if (!this.CheckBox_optionalParameters.Checked)
                {
                    // hide optional parameters when checkbox is not checked
                    parameters = parameters.Where(p => !p.IsOptional && !(Nullable.GetUnderlyingType(p.ParameterType) != null)).ToArray();
                }

                parameters = parameters.Where(p => !((p.Name ?? string.Empty).Contains("worker", StringComparison.OrdinalIgnoreCase))).ToArray();

                int y = this.DrawingY;
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo p = parameters[i];

                    // Label - show friendly primitive name (AudioObj parameters won't be shown as controls but we still label them)
                    var labelText = $"{p.Name} ({GetPrimitiveTypeNameFromParameterType(p.ParameterType)})";
                    var lbl = new Label()
                    {
                        Text = labelText,
                        Location = new Point(this.LabelX, y),
                        Size = new Size(this.LabelWidth, this.InputHeight),
                        AutoEllipsis = true,
                        ForeColor = p.IsOptional ? Color.Gray : SystemColors.ControlText
                    };

                    // If parameter is AudioObj, show info label and don't create input control
                    if (IsAudioObjParameter(p.ParameterType))
                    {
                        lbl.Text += " — will be taken from SelectedTrack";
                        this.Panel_parameters.Controls.Add(lbl);
                        y += this.InputHeight + this.Margin;
                        continue;
                    }

                    this.Panel_parameters.Controls.Add(lbl);

                    // Control
                    Control? ctrl = this.CreateControlForParameterType(p.ParameterType, i);
                    if (ctrl == null)
                    {
                        continue;
                    }

                    ctrl.Location = new Point(this.InputX, y);
                    ctrl.Size = new Size(this.InputWidth, this.InputHeight);

                    // if parameter has a default value, try to set the control to it
                    if (p.HasDefaultValue && p.DefaultValue != DBNull.Value && p.DefaultValue != null)
                    {
                        TrySetControlValue(ctrl, p.DefaultValue);
                    }

                    // store parameter info in Tag for later retrieval
                    ctrl.Tag = p;

                    this.Panel_parameters.Controls.Add(ctrl);

                    // store in map; key is a new array so its reference is unique
                    var key = new Type[] { p.ParameterType };
                    this.ParameterControlsMap[key] = ctrl;

                    y += this.InputHeight + this.Margin;
                }

                // Optionally add a vertical scrollbar when content exceeds panel
                if (y > this.Panel_parameters.Height)
                {
                    this.Panel_parameters.VerticalScroll.Enabled = true;
                    int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
                    this.Panel_parameters.Controls.OfType<Control>().ToList().ForEach(c =>
                    {
                        if (c.Right > this.Panel_parameters.Width - scrollBarWidth)
                        {
                            c.Width -= scrollBarWidth;
                        }
                    });
                }
                else
                {
                    this.Panel_parameters.VerticalScroll.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }

            static void TrySetControlValue(Control ctrl, object defaultValue)
            {
                try
                {
                    if (ctrl is NumericUpDown nud)
                    {
                        if (decimal.TryParse(Convert.ToString(defaultValue), out var dec))
                        {
                            if (dec < nud.Minimum)
                            {
                                dec = nud.Minimum;
                            }

                            if (dec > nud.Maximum)
                            {
                                dec = nud.Maximum;
                            }

                            nud.Value = dec;
                        }
                    }
                    else if (ctrl is CheckBox cb)
                    {
                        if (bool.TryParse(Convert.ToString(defaultValue), out var b))
                        {
                            cb.Checked = b;
                        }
                    }
                    else if (ctrl is TextBox tb)
                    {
                        tb.Text = defaultValue?.ToString() ?? string.Empty;
                    }
                    else if (ctrl is ComboBox combo)
                    {
                        var s = Convert.ToString(defaultValue);
                        if (!string.IsNullOrEmpty(s) && combo.Items.Contains(s))
                        {
                            combo.SelectedItem = s;
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void RegisterCheckBox_RebuildPanelParameters()
        {
            MethodInfo? mi = this.ComboBox_functions.SelectedItem as MethodInfo;
            if (mi != null)
            {
                this.BuildParametersPanel(mi.Name);
            }
        }

        private async Task InvokeSelectedMethodAsync()
        {
            this.Button_go.Enabled = false;
            MethodInfo? mi = null;
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                mi = this.ComboBox_functions.SelectedItem as MethodInfo;
                if (mi == null)
                {
                    LogCollection.Log("No method selected.");
                    return;
                }

                // Build parameter list from method signature; for each parameter try to find a control (by matching ParameterInfo)
                var methodParams = mi.GetParameters();
                var paramValues = new object?[methodParams.Length];

                for (int pi = 0; pi < methodParams.Length; pi++)
                {
                    var p = methodParams[pi];

                    // Find control for this parameter (if any). We stored ParameterInfo in control.Tag when building the panel.
                    var ctrl = this.Panel_parameters.Controls
                        .OfType<Control>()
                        .FirstOrDefault(c => c.Tag is ParameterInfo tag && tag.Position == p.Position && tag.Name == p.Name);

                    if (ctrl != null)
                    {
                        paramValues[pi] = TryGetValueFromControl(ctrl, p.ParameterType);
                        continue;
                    }

                    // No control found:
                    if (IsAudioObjParameter(p.ParameterType))
                    {
                        // Try to inject SelectedTrack
                        if (this.SelectedTrack != null)
                        {
                            paramValues[pi] = this.SelectedTrack;
                        }
                        else
                        {
                            LogCollection.Log($"Method '{mi.Name}' requires an AudioObj parameter '{p.Name}', but no SelectedTrack is available.");
                            return;
                        }

                        continue;
                    }
                    else if (IsAudioCollectionParameter(p.ParameterType))
                    {
                        // Try to inject SelectedCollections
                        var collections = this.SelectedCollections.ToArray();
                        if (collections.Length > 0)
                        {
                            // If parameter type is exactly AudioCollection, but we have multiple selected collections, we cannot provide that.
                            if (p.ParameterType == typeof(AudioCollection) && collections.Length > 1)
                            {
                                LogCollection.Log($"Method '{mi.Name}' requires an AudioCollection parameter '{p.Name}', but multiple collections are selected.");
                                return;
                            }
                            // If parameter type is AudioCollection[], provide all selected collections as array
                            if (p.ParameterType == typeof(AudioCollection[]))
                            {
                                paramValues[pi] = collections;
                            }
                            else
                            {
                                // Otherwise provide collection wherein the selected track is, or first collection
                                var selectedTrack = this.SelectedTrack;
                                if (selectedTrack != null)
                                {
                                    var collectionWithTrack = collections.FirstOrDefault(c => c.Audios.Contains(selectedTrack));
                                    if (collectionWithTrack != null)
                                    {
                                        paramValues[pi] = collectionWithTrack;
                                    }
                                    else
                                    {
                                        paramValues[pi] = collections[0];
                                    }
                                }
                                else
                                {
                                    paramValues[pi] = collections[0];
                                }
                            }
                        }
                        else
                        {
                            LogCollection.Log($"Method '{mi.Name}' requires an AudioCollection parameter '{p.Name}', but no collections are selected.");
                            return;
                        }
                        continue;
                    }

                    // If parameter has default value, use it
                    if (p.HasDefaultValue && p.DefaultValue != DBNull.Value)
                    {
                        paramValues[pi] = p.DefaultValue;
                        continue;
                    }

                    // No control, not AudioObj, no default -> null (or could log)
                    paramValues[pi] = null;
                }

                object? instance = null;
                if (!mi.IsStatic)
                {
                    // Resolve instance for common declaring types
                    var declType = mi.DeclaringType;
                    if (declType == null)
                    {
                        LogCollection.Log("Declaring type is null for the selected method.");
                        return;
                    }

                    if (typeof(AudioObj).IsAssignableFrom(declType))
                    {
                        if (this.SelectedTrack != null)
                        {
                            instance = this.SelectedTrack;
                        }
                        else
                        {
                            LogCollection.Log($"Cannot invoke '{mi.Name}' because no SelectedTrack is available.");
                            return;
                        }
                    }
                    else if (typeof(AudioCollection).IsAssignableFrom(declType))
                    {
                        var collections = this.SelectedCollections.ToArray();
                        if (collections.Length == 0)
                        {
                            LogCollection.Log($"Cannot invoke '{mi.Name}' because no AudioCollection is available.");
                            return;
                        }

                        var selectedTrack = this.SelectedTrack;
                        var collectionWithTrack = selectedTrack != null ? collections.FirstOrDefault(c => c.Audios.Contains(selectedTrack)) : null;
                        instance = (object) (collectionWithTrack ?? collections[0]);
                    }
                    else
                    {
                        // Fallback: try to create instance if possible
                        try
                        {
                            instance = Activator.CreateInstance(declType);
                        }
                        catch (Exception ex)
                        {
                            LogCollection.Log($"Cannot create instance of '{declType.FullName}'. {ex.Message}");
                            return;
                        }
                    }
                }
                else
                {
                    // static method: ensure any AudioObj/AudioCollection parameter missing was already filled above
                }

                // Invoke
                object? invokeResult = null;
                var returnType = mi.ReturnType;

                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    // Async method. We need to invoke and await the task.
                    var task = (Task?) mi.Invoke(instance, paramValues);
                    if (task == null)
                    {
                        LogCollection.Log("Invoked async method returned null Task.");
                        return;
                    }

                    await task.ConfigureAwait(false);

                    // If Task<T>, get result via reflection
                    if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        var resultProperty = task.GetType().GetProperty("Result");
                        if (resultProperty != null)
                        {
                            invokeResult = resultProperty.GetValue(task);
                        }
                    }
                }
                else
                {
                    // Synchronous method: run in background to avoid blocking UI
                    invokeResult = await Task.Run(() => mi.Invoke(instance, paramValues)).ConfigureAwait(false);
                }

                // Log or handle result
                if (invokeResult != null)
                {
                    LogCollection.Log($"Method returned: {invokeResult}");
                }
                else
                {
                    LogCollection.Log("Method invocation completed (no result or null).");
                }
            }
            catch (TargetInvocationException tie)
            {
                // unwrap
                LogCollection.Log(tie.InnerException ?? tie);
            }
            catch (Exception ex)
            {
                LogCollection.Log(ex);
            }
            finally
            {
                sw.Stop();
                this.Button_go.Invoke((Action) (() =>
                {
                    this.Button_go.Enabled = true;
                    LogCollection.Log($"Executed basic method '{(mi?.Name ?? "N/A")}' ({sw.Elapsed.TotalMilliseconds} ms. elapsed)");
                    var win = this.MainWindow as WindowMain;
                    if (win != null)
                    {
                        // win.UpdateSelectedCollectionListBox();
                    }
                }));
            }

            static object? TryGetValueFromControl(Control c, Type targetType)
            {
                try
                {
                    if (c is NumericUpDown nud)
                    {
                        if (targetType == typeof(int))
                        {
                            return Convert.ToInt32(nud.Value);
                        }

                        if (targetType == typeof(long))
                        {
                            return Convert.ToInt64(nud.Value);
                        }

                        if (targetType == typeof(float))
                        {
                            return Convert.ToSingle(nud.Value);
                        }

                        if (targetType == typeof(double))
                        {
                            return Convert.ToDouble(nud.Value);
                        }
                        // fallback
                        return nud.Value;
                    }
                    else if (c is CheckBox cb)
                    {
                        return cb.Checked;
                    }
                    else if (c is TextBox tb)
                    {
                        if (targetType == typeof(string) || targetType == typeof(object))
                        {
                            return tb.Text;
                        }
                        // try convert to primitive types
                        if (targetType == typeof(int) && int.TryParse(tb.Text, out var i))
                        {
                            return i;
                        }

                        if (targetType == typeof(long) && long.TryParse(tb.Text, out var l))
                        {
                            return l;
                        }

                        if (targetType == typeof(float) && float.TryParse(tb.Text, out var f))
                        {
                            return f;
                        }

                        if (targetType == typeof(double) && double.TryParse(tb.Text, out var d))
                        {
                            return d;
                        }

                        if (targetType == typeof(bool) && bool.TryParse(tb.Text, out var b))
                        {
                            return b;
                        }
                        // fallback: return text
                        return tb.Text;
                    }
                    else if (c is ComboBox combo)
                    {
                        var sel = combo.SelectedItem?.ToString();
                        if (targetType.IsEnum && sel != null)
                        {
                            try { return Enum.Parse(targetType, sel); } catch { }
                        }
                        return sel;
                    }
                }
                catch (Exception ex)
                {
                    LogCollection.Log(ex);
                }

                return null;
            }
        }

        public static string GetPrimitiveTypeNameFromParameterType(Type parameterType)
        {
            if (parameterType == null)
            {
                return "void";
            }

            // Wenn Nullable<T>, dann zugrunde liegenden Typ extrahieren
            if (Nullable.GetUnderlyingType(parameterType) is Type underlyingType)
            {
                parameterType = underlyingType;
            }

            // Dictionary bekannter Aliase
            var primitiveAliases = new Dictionary<Type, string>
            {
                { typeof(bool), "bool" },
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(char), "char" },
                { typeof(decimal), "decimal" },
                { typeof(double), "double" },
                { typeof(float), "float" },
                { typeof(int), "int" },
                { typeof(uint), "uint" },
                { typeof(long), "long" },
                { typeof(ulong), "ulong" },
                { typeof(short), "short" },
                { typeof(ushort), "ushort" },
                { typeof(object), "object" },
                { typeof(string), "string" }
            };

            // Prüfen, ob der Typ ein Alias hat
            if (primitiveAliases.TryGetValue(parameterType, out string? alias))
            {
                return alias;
            }

            // Sonst generischer Name ohne Namespace
            return parameterType.Name;
        }

    }
}
