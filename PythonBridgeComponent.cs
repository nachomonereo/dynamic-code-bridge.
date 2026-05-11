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
              "A real-time Python link for Rhino 8 CPython with AI Auto-Debugging.",
              "IAAC", "CodeBridge")
        {
            _lastCode = @"# 🌮 DYNAMIC PYTHON BRIDGE - MASTER MANUAL v4.2
# ===========================================================================
# INSTRUCTIONS:
# 1. LINK: Connect this file path to the 'P' input of the Bridge component.
# 2. SYNC: Save this file (Ctrl+S) and Grasshopper updates instantly.
# 3. LIBRARIES: Use '# r: library_name' to auto-install external dependencies.
#    Example: 
#    # r: numpy
#    # r: pandas
#
# [ COPY-PASTE THIS SYSTEM PROMPT TO YOUR AI ASSISTANT ]
# ---------------------------------------------------------------------------
# ""You are an expert Rhino/Grasshopper Python Developer. I am using the 
# 'Dynamic Code Bridge' for Rhino 8 (CPython).
# 
# RULES FOR GENERATING CODE:
# 1. PARAMETERS: Start with tags: # IN: Name or # OUT: Name.
# 2. DEPENDENCIES: If you need external libraries, add '# r: name' tags at the top.
# 3. DATA ACCESS: Use the 'Inputs' dictionary or direct variable names.
# 4. OUTPUTS: Assign results to variables matching your # OUT tags.
# ""
# ---------------------------------------------------------------------------

# IN: Radius
# OUT: Result

import Rhino.Geometry as rg

# Logic:
print('Python Bridge Active')
Result = rg.Sphere(rg.Point3d.Origin, Inputs.get('Radius', 1.0))
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
                    var doc = OnPingDocument();
                    if (doc != null) doc.ScheduleSolution(5, d => this.ExpireSolution(false));
                    return;
                }

                if (!string.IsNullOrEmpty(_lastCode))
                {
                    var inputs = new Dictionary<string, object>();
                    int dynInStart = (_isInternalized || Params.Input.Count == 0 || Params.Input[0].Name != "File Path") ? 0 : 1;

                    for (int i = dynInStart; i < Params.Input.Count; i++) {
                        object val = null;
                        try {
                            DA.GetData(i, ref val);
                            if (val is Grasshopper.Kernel.Types.IGH_Goo goo) val = goo.ScriptVariable();
                        } catch { }
                        inputs[Params.Input[i].Name] = val;
                    }

                    // PYTHON 3 EXECUTION (RHINO 8 CPYTHON ENGINE)
                    try {
                        var language = RhinoCode.Languages.QueryLatest(new LanguageSpec("mcneel.pythonnet.python"));
                        if (language == null) {
                            DA.SetDataList(0, new List<object> { "[ERROR] Python 3 engine not initialized. Open the Rhino Script Editor once." });
                            return;
                        }

                        var script = language.CreateCode(_lastCode);
                        var ctx = new RunContext();
                        
                        // Inject variables into script scope
                        foreach (var kvp in inputs) {
                            ctx.Inputs[kvp.Key] = kvp.Value;
                        }
                        ctx.Inputs["Inputs"] = inputs;

                        script.Run(ctx);
                        
                        // Consolidated Output (OUT)
                        var report = new List<object>();
                        report.Add("Status: OK (Python 3)");
                        report.Add("Link: " + (_isInternalized ? "STANDALONE" : _currentPath));
                        report.Add("Sync: " + DateTime.Now.ToLongTimeString());

                        string logContent = GetLogContent();
                        if (!string.IsNullOrEmpty(logContent)) report.Add("Log: " + logContent);
                        
                        DA.SetDataList(0, report);

                        // Dynamic Outputs: Retrieve from Python scope (RunContext)
                        for (int i = 1; i < Params.Output.Count; i++) {
                            string outName = Params.Output[i].Name;
                            if (ctx.Outputs.TryGet(outName, out object val)) {
                                if (val is IEnumerable list && !(val is string)) {
                                    DA.SetDataList(i, list);
                                } else {
                                    DA.SetData(i, val);
                                }
                            }
                        }

                        if (!_isInternalized) ReportStatus("SUCCESS", "Ready", _currentPath);
                    } catch (Exception ex) {
                        _statusText = "EXECUTION ERROR";
                        var errorReport = new List<object> {
                            "[PYTHON 3 ERROR] " + ex.Message,
                            "Check your log file for full stack trace."
                        };
                        DA.SetDataList(0, errorReport);
                        if (!_isInternalized) ReportStatus("ERROR", ex.Message, _currentPath);
                    }
                }
            }
            catch (Exception ex) 
            {
                _statusText = "FATAL ERROR";
                ReportStatus("CRITICAL", ex.Message, _currentPath);
            }
        }

        private string GetLogContent()
        {
            try {
                if (string.IsNullOrEmpty(_currentPath)) return "";
                string logFileName = $"bridge_status_{this.InstanceGuid.ToString().Substring(0, 8)}.log";
                string logPath = Path.Combine(Path.GetDirectoryName(_currentPath), logFileName);
                if (File.Exists(logPath)) return File.ReadAllText(logPath);
            } catch { }
            return "";
        }

        private void ReportStatus(string status, string message, string scriptPath)
        {
            try {
                if (string.IsNullOrEmpty(scriptPath)) return;
                string logFileName = $"bridge_status_{this.InstanceGuid.ToString().Substring(0, 8)}.log";
                string logPath = Path.Combine(Path.GetDirectoryName(scriptPath), logFileName);
                File.WriteAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] [{status}] {message}");
            } catch { }
        }

        private bool SyncParameters(string code)
        {
            // STRICT CLEAN START: Only parse tags if linked or internalized
            bool hasFile = !string.IsNullOrEmpty(_currentPath) && File.Exists(_currentPath);
            if (!_isInternalized && !hasFile)
            {
                bool paramsChanged = false;
                for (int i = Params.Input.Count - 1; i >= 1; i--) { Params.UnregisterInputParameter(Params.Input[i]); paramsChanged = true; }
                for (int i = Params.Output.Count - 1; i >= 1; i--) { Params.UnregisterOutputParameter(Params.Output[i]); paramsChanged = true; }
                return paramsChanged;
            }

            var lines = (code ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var dIn = new List<string>();
            var dOut = new List<string>();
            foreach (var line in lines) {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("#")) continue;
                string content = trimmed.TrimStart('#', ' ');
                
                if (content.StartsWith("IN:")) {
                    dIn.AddRange(content.Substring(3).Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                }
                else if (content.StartsWith("OUT:")) {
                    dOut.AddRange(content.Substring(4).Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
                }
            }

            bool changed = false;
            if (_isInternalized) {
                if (Params.Input.Count > 0 && Params.Input[0].Name == "File Path") {
                    Params.UnregisterInputParameter(Params.Input[0]);
                    changed = true;
                }
            } else {
                if (Params.Input.Count == 0 || Params.Input[0].Name != "File Path") {
                    var p = new Grasshopper.Kernel.Parameters.Param_String { Name = "File Path", NickName = "P", Access = GH_ParamAccess.item, Optional = true };
                    Params.RegisterInputParam(p, 0);
                    changed = true;
                }
            }

            int dynInStart = (_isInternalized || Params.Input.Count == 0 || Params.Input[0].Name != "File Path") ? 0 : 1;
            for (int i = Params.Input.Count - 1; i >= dynInStart; i--) {
                if (!dIn.Contains(Params.Input[i].Name)) { Params.UnregisterInputParameter(Params.Input[i]); changed = true; }
            }
            foreach (var name in dIn) {
                if (!string.IsNullOrEmpty(name) && !Params.Input.Any(p => p.Name == name)) {
                    var p = new Grasshopper.Kernel.Parameters.Param_GenericObject { Name = name, NickName = name, Access = GH_ParamAccess.item, Optional = true };
                    Params.RegisterInputParam(p); changed = true;
                }
            }
            for (int i = Params.Output.Count - 1; i >= 1; i--) {
                if (!dOut.Contains(Params.Output[i].Name)) { Params.UnregisterOutputParameter(Params.Output[i]); changed = true; }
            }
            foreach (var name in dOut) {
                if (!string.IsNullOrEmpty(name) && !Params.Output.Any(p => p.Name == name)) {
                    var p = new Grasshopper.Kernel.Parameters.Param_GenericObject { Name = name, NickName = name };
                    Params.RegisterOutputParam(p); changed = true;
                }
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
