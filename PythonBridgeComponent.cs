using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using GH_IO.Serialization;
using Rhino.Geometry;
using System.Drawing.Imaging;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

namespace DynamicCodeBridge
{
    public class DynamicPythonBridgeComponent : GH_Component, IGH_VariableParameterComponent
    {
        private FileSystemWatcher _watcher;
        private string _lastCode = "";
        private string _currentPath = "";
        private bool _isInternalized = false; // DEFAULT: Clean Start (Linked mode)
        private string _statusText = "DISCONNECTED";

        public DynamicPythonBridgeComponent()
          : base("Dynamic Python Bridge", "PyBridge",
              "A real-time Python link for Rhino 8 CPython with Diagnostic Logs.",
              "IAAC", "CodeBridge")
        {
            _lastCode = @"# r: numpy
# !python3
# ===========================================================================
# 🌮 DYNAMIC CODE BRIDGE - MASTER MANUAL v1.3.4
# Developed by Nacho Monereo | IAAC
# ===========================================================================
#
# 📖 INSTRUCTIONS:
# 1. LINK: Connect this file path to the 'P' input of the Bridge component.
# 2. SYNC: Save (Ctrl+S) in your editor and Grasshopper updates instantly.
# 3. LIBRARIES: Add '# r: library_name' at the very top to auto-install.
# 4. AUTO-DEBUGGING: If an error occurs, the Bridge generates a '.log' file. 
#    PROVIDE THIS LOG TO YOUR AI (ChatGPT/Gemini). It contains the stack trace 
#    and variable states needed to fix the code automatically.
#
# 🤖 [ AI SYSTEM PROMPT - COPY & PASTE TO CHATGPT/GEMINI ]
# ---------------------------------------------------------------------------
# ""You are an expert Rhino/Grasshopper Python Developer. I am using the 
# 'Dynamic Code Bridge' for Rhino 8 (CPython). 
# 
# MANDATORY RULES FOR GENERATING CODE:
# 1. HEADERS: Use '# r: library' on Line 1 for dependencies.
# 2. TAGS: Use '# IN: Name1, Name2' and '# OUT: Name1, Name2' to sync pins.
# 3. COMPATIBILITY: Always start with 'Inputs = dict(Inputs)'.
# 4. DATA ACCESS: Use 'val.Value if hasattr(val, ""Value"") else val' for numbers.
# 5. LISTS: Always validate if an input is a list before iterating.
# 6. OUTPUTS: Assign results to variables matching your # OUT tags.""
# ---------------------------------------------------------------------------

# IN: Radius
# OUT: MySphere

import Rhino.Geometry as rg

# 1. COMPATIBILITY LAYER
# We convert the .NET dictionary to a native Python dict.
Inputs = dict(Inputs)

def get_num(key, default):
    val = Inputs.get(key)
    if val is None: return default
    # Extract the .Value from Grasshopper types (GH_Number, GH_Integer)
    return val.Value if hasattr(val, 'Value') else val

try:
    # 2. INPUT RECOVERY
    # Pattern: get_num('PinName', defaultValue)
    r = float(get_num('Radius', 1.0))

    # 3. GEOMETRY LOGIC
    # Your parametric logic goes here.
    MySphere = rg.Sphere(rg.Point3d.Origin, max(0.1, r))

    # 4. STATUS REPORT
    print('Python Bridge Ready | Sphere Radius: {0:.2f}'.format(r))
    'Status: OK'

except Exception as e:
    # Diagnostic Log will capture the full StackTrace and Input Snapshot.
    raise e
";
        }

        public override void CreateAttributes()
        {
            m_attributes = new DynamicPythonBridgeAttributes(this);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "P", "Path to the .py file", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "OUT", "Component Output", GH_ParamAccess.list);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetString("LastCode", _lastCode);
            writer.SetBoolean("IsInternalized", _isInternalized);
            writer.SetString("CurrentPath", _currentPath);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            if (reader.ItemExists("LastCode")) _lastCode = reader.GetString("LastCode");
            if (reader.ItemExists("IsInternalized")) _isInternalized = reader.GetBoolean("IsInternalized");
            if (reader.ItemExists("CurrentPath")) _currentPath = reader.GetString("CurrentPath");
            
            if (!_isInternalized && !string.IsNullOrEmpty(_currentPath)) SetupWatcher(_currentPath);
            return base.Read(reader);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Internalize Code (Standalone)", (s, e) => {
                _isInternalized = !_isInternalized;
                var doc = OnPingDocument();
                if (doc != null) doc.ScheduleSolution(5, d => this.ExpireSolution(false));
            }, true, _isInternalized);

            Menu_AppendItem(menu, "Link to File Path...", (s, e) => {
                OpenFileDialog ofd = new OpenFileDialog { Filter = "Python Script|*.py", Title = "Link Bridge Code" };
                if (ofd.ShowDialog() == DialogResult.OK) {
                    _currentPath = ofd.FileName;
                    _isInternalized = false;
                    _lastCode = File.ReadAllText(_currentPath);
                    SetupWatcher(_currentPath);
                    
                    if (SyncParameters(_lastCode)) {
                        Params.OnParametersChanged();
                    }
                    this.OnAttributesChanged();

                    var doc = OnPingDocument();
                    if (doc != null) doc.ScheduleSolution(5, d => this.ExpireSolution(true));
                }
            });

            Menu_AppendSeparator(menu);

            Menu_AppendItem(menu, "Export Code to File...", (s, e) => {
                SaveFileDialog sfd = new SaveFileDialog { Filter = "Python Script|*.py", Title = "Export & Link Master Template", FileName = "bridge_logic.py" };
                if (sfd.ShowDialog() == DialogResult.OK) {
                    try {
                        File.WriteAllText(sfd.FileName, _lastCode);
                        _currentPath = sfd.FileName;
                        _isInternalized = false;
                        SetupWatcher(_currentPath);
                        
                        // Force a refresh to show pins after link
                        this.OnAttributesChanged();
                        this.ExpireSolution(true);

                        MessageBox.Show("Bridge Linked!\nYou can now edit this file in VS Code.\nPath: " + sfd.FileName, "Dynamic Python Bridge");
                    } catch (Exception ex) {
                        MessageBox.Show("Export failed: " + ex.Message);
                    }
                }
            });
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            try 
            {
                if (!_isInternalized)
                {
                    string pathInput = string.Empty;
                    if (Params.Input.Count > 0 && Params.Input[0].Name == "File Path") {
                        DA.GetData(0, ref pathInput);
                    }

                    // FIX: Priority to cable input. If cable is empty, use internalized/last path.
                    string activePath = !string.IsNullOrEmpty(pathInput) ? pathInput : _currentPath;

                    if (string.IsNullOrEmpty(activePath)) {
                        _statusText = "DISCONNECTED";
                        DA.SetData(0, "Waiting for link...");
                        
                        // ABSOLUTE PROTECTION: Force clean UI every time we are disconnected
                        bool changed = false;
                        for (int i = Params.Input.Count - 1; i >= 1; i--) { Params.UnregisterInputParameter(Params.Input[i]); changed = true; }
                        for (int i = Params.Output.Count - 1; i >= 1; i--) { Params.UnregisterOutputParameter(Params.Output[i]); changed = true; }
                        if (changed) {
                            this.OnAttributesChanged();
                            var doc = OnPingDocument();
                            if (doc != null) doc.ScheduleSolution(5, d => this.ExpireSolution(false));
                        }
                        return;
                    }

                    if (_currentPath != activePath) {
                        if (!string.IsNullOrEmpty(activePath) && File.Exists(activePath)) {
                            _currentPath = activePath;
                            _lastCode = File.ReadAllText(_currentPath);
                            SetupWatcher(_currentPath);
                            _statusText = "LINKED: " + Path.GetFileName(_currentPath);
                            
                            if (SyncParameters(_lastCode)) {
                                Params.OnParametersChanged();
                                this.OnAttributesChanged();
                            }
                            
                            var doc = OnPingDocument();
                            if (doc != null) doc.ScheduleSolution(5, d => this.ExpireSolution(true));
                            return;
                        } else {
                            _statusText = "FILE MISSING";
                            DA.SetData(0, "File missing");
                            return;
                        }
                    }

                    if (File.Exists(_currentPath)) {
                        _statusText = "LINKED: " + Path.GetFileName(_currentPath);
                    } else {
                        _statusText = "FILE MISSING";
                        DA.SetData(0, "File missing");
                        return;
                    }
                }
                else
                {
                    _statusText = "PORTABLE PYTHON ACTIVE";
                }

                if (SyncParameters(_lastCode)) {
                    this.OnAttributesChanged();
                    var d = OnPingDocument();
                    if (d != null) d.ScheduleSolution(5, x => this.ExpireSolution(false));
                    return;
                }

                var inputs = new Dictionary<string, object>();
                int start = (_isInternalized || Params.Input.Count == 0 || Params.Input[0].Name != "File Path") ? 0 : 1;
                for (int i = start; i < Params.Input.Count; i++) {
                    object val = null; DA.GetData(i, ref val);
                    inputs[Params.Input[i].Name] = val;
                }

                // PYTHON 3 EXECUTION (RHINO 8 CPYTHON ENGINE)
                try {
                    var language = RhinoCode.Languages.QueryLatest(new LanguageSpec("mcneel.pythonnet.python"));
                    if (language == null) {
                        DA.SetDataList(0, new List<object> { "[ERROR] Python 3 engine not initialized." });
                        return;
                    }

                    var script = language.CreateCode(_lastCode);
                    var ctx = new RunContext();
                    foreach (var kvp in inputs) ctx.Inputs[kvp.Key] = kvp.Value;
                    ctx.Inputs["Inputs"] = inputs;
                    
                    // Inject __file__ for path discovery
                    if (!string.IsNullOrEmpty(_currentPath)) {
                        ctx.Inputs["__file__"] = _currentPath;
                    }

                    script.Run(ctx);
                    
                    var report = new List<object> {
                        "Status: OK (Python 3)",
                        "Link: " + (_isInternalized ? "STANDALONE" : _currentPath),
                        "Sync: " + DateTime.Now.ToLongTimeString()
                    };

                    string logContent = GetLogContent();
                    if (!string.IsNullOrEmpty(logContent)) report.Add("Log: " + logContent);
                    DA.SetDataList(0, report);

                    for (int i = 1; i < Params.Output.Count; i++) {
                        string name = Params.Output[i].Name;
                        if (ctx.Outputs.TryGet(name, out object val)) {
                            if (val is IEnumerable l && !(val is string)) DA.SetDataList(i, l);
                            else DA.SetData(i, val);
                        }
                    }

                    if (!_isInternalized) ReportStatus("SUCCESS", "Ready", _currentPath, inputs);
                } catch (Exception ex) {
                    _statusText = "EXECUTION ERROR";
                    DA.SetDataList(0, new List<object> { "[PYTHON 3 ERROR] " + ex.Message, "Check log for details." });
                    if (!_isInternalized) ReportStatus("ERROR", ex.Message, _currentPath, null, ex.StackTrace);
                }
            } catch (Exception ex) {
                _statusText = "FATAL ERROR";
                if (!_isInternalized) ReportStatus("CRITICAL", ex.Message, _currentPath, null, ex.StackTrace);
            }
        }

        private string GetLogContent()
        {
            try {
                if (string.IsNullOrEmpty(_currentPath)) return "";
                string logFile = $"bridge_status_{this.InstanceGuid.ToString().Substring(0, 8)}.log";
                string logPath = Path.Combine(Path.GetDirectoryName(_currentPath), logFile);
                if (File.Exists(logPath)) return File.ReadAllText(logPath);
            } catch { }
            return "";
        }

        private void ReportStatus(string status, string message, string scriptPath, Dictionary<string, object> inputs = null, string stackTrace = null)
        {
            try {
                if (string.IsNullOrEmpty(scriptPath)) return;
                string logFile = $"bridge_status_{this.InstanceGuid.ToString().Substring(0, 8)}.log";
                string logPath = Path.Combine(Path.GetDirectoryName(scriptPath), logFile);
                
                using (StreamWriter sw = new StreamWriter(logPath, false)) {
                    sw.WriteLine("===========================================================================");
                    sw.WriteLine($"DYNAMIC PYTHON BRIDGE DIAGNOSTIC | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine("===========================================================================");
                    sw.WriteLine($"STATUS: [{status}]");
                    sw.WriteLine($"MESSAGE: {message}");
                    sw.WriteLine($"FILE: {scriptPath}");
                    sw.WriteLine("---------------------------------------------------------------------------");

                    if (!string.IsNullOrEmpty(stackTrace)) {
                        sw.WriteLine("CRITICAL PYTHON ERROR:");
                        sw.WriteLine(stackTrace);
                        sw.WriteLine("---------------------------------------------------------------------------");
                    }

                    if (inputs != null) {
                        sw.WriteLine("INPUTS STATE SNAPSHOT:");
                        foreach(var kvp in inputs) {
                            string valStr = kvp.Value?.ToString() ?? "NULL";
                            if (valStr.Length > 100) valStr = valStr.Substring(0, 97) + "...";
                            sw.WriteLine($"- {kvp.Key}: {valStr} ({kvp.Value?.GetType().Name ?? "N/A"})");
                        }
                        sw.WriteLine("---------------------------------------------------------------------------");
                    }

                    sw.WriteLine("EXECUTED SOURCE CODE:");
                    string[] codeLines = _lastCode.Split('\n');
                    for (int i = 0; i < codeLines.Length; i++) {
                        sw.WriteLine($"{(i + 1),3}: {codeLines[i].TrimEnd()}");
                    }
                    sw.WriteLine("===========================================================================");
                }
            } catch { }
        }

        private bool SyncParameters(string code)
        {
            var lines = (code ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var newIn = new List<string>();
            var newOut = new List<string>();

            foreach (var line in lines) {
                string t = line.Trim();
                if (t.StartsWith("// IN:") || t.StartsWith("# IN:")) 
                    newIn.AddRange(t.Split(':')[1].Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                if (t.StartsWith("// OUT:") || t.StartsWith("# OUT:")) 
                    newOut.AddRange(t.Split(':')[1].Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
            }

            bool changed = false;
            int startIdx = (_isInternalized || Params.Input.Count == 0 || Params.Input[0].Name != "File Path") ? 0 : 1;

            // 1. Check Inputs
            var currentIn = Params.Input.Skip(startIdx).Select(p => p.Name).ToList();
            if (!newIn.SequenceEqual(currentIn)) {
                for (int i = Params.Input.Count - 1; i >= startIdx; i--) Params.UnregisterInputParameter(Params.Input[i]);
                foreach (var name in newIn) {
                    var p = new Grasshopper.Kernel.Parameters.Param_GenericObject { Name = name, NickName = name, Access = GH_ParamAccess.item, Optional = true };
                    Params.RegisterInputParam(p);
                }
                changed = true;
            }

            // 2. Check Outputs
            var currentOut = Params.Output.Skip(1).Select(p => p.Name).ToList();
            if (!newOut.SequenceEqual(currentOut)) {
                for (int i = Params.Output.Count - 1; i >= 1; i--) Params.UnregisterOutputParameter(Params.Output[i]);
                foreach (var name in newOut) {
                    var p = new Grasshopper.Kernel.Parameters.Param_GenericObject { Name = name, NickName = name };
                    Params.RegisterOutputParam(p);
                }
                changed = true;
            }

            return changed;
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => null;
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;
        public void VariableParameterMaintenance() { }

        private void ReloadCode() {
            if (string.IsNullOrEmpty(_currentPath) || !File.Exists(_currentPath)) return;
            for (int i = 0; i < 5; i++) {
                try {
                    _lastCode = File.ReadAllText(_currentPath);
                    break;
                } catch { System.Threading.Thread.Sleep(50); }
            }
        }

        private void SetupWatcher(string path) {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            _watcher?.Dispose();
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path));
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
            _watcher.Changed += (s, e) => Rhino.RhinoApp.InvokeOnUiThread((Action)(() => {
                System.Threading.Thread.Sleep(50); 
                ReloadCode();
                var doc = OnPingDocument();
                if (doc != null) doc.ScheduleSolution(5, d => this.ExpireSolution(true));
            }));
            _watcher.EnableRaisingEvents = true;
        }

        protected override Bitmap Icon {
            get {
                try {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    using (Stream stream = assembly.GetManifestResourceStream("DynamicCodeBridge.logo-p.png")) {
                        if (stream != null) {
                            Bitmap source = new Bitmap(stream);
                            Bitmap target = new Bitmap(24, 24);
                            using (Graphics g = Graphics.FromImage(target)) {
                                g.Clear(Color.Transparent);
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                float factor = Math.Min(24f / source.Width, 24f / source.Height);
                                int w = (int)(source.Width * factor);
                                int h = (int)(source.Height * factor);
                                g.DrawImage(source, (24 - w) / 2, (24 - h) / 2, w, h);
                            }
                            return target;
                        }
                    }
                } catch { }
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("5e2b3f33-1c2b-5d3e-076b-9e8c4f2b5d0c"); }
        }

        public string StatusText => _statusText;
    }

    public class DynamicPythonBridgeAttributes : GH_ComponentAttributes
    {
        public DynamicPythonBridgeAttributes(IGH_Component component) : base(component) { }
        protected override void Layout() { base.Layout(); RectangleF rec = Bounds; rec.Height += 12; Bounds = rec; }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel) {
            base.Render(canvas, graphics, channel);
            if (channel == GH_CanvasChannel.Objects) {
                RectangleF bar = Bounds; bar.Y = bar.Bottom - 12; bar.Height = 12;
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(40, 60, 100)), bar);
                DynamicPythonBridgeComponent owner = (DynamicPythonBridgeComponent)Owner;
                string label = owner.StatusText;
                Font font = new Font(GH_FontServer.Standard.FontFamily, 6);
                StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                graphics.DrawString(label, font, Brushes.White, bar, format);
            }
        }
    }
}
