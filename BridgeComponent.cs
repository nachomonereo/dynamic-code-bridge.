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
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace DynamicCodeBridge
{
    public class DynamicBridgeComponent : GH_Component, IGH_VariableParameterComponent
    {
        private FileSystemWatcher _watcher;
        private string _lastCode = "";
        private string _currentPath = "";
        private bool _isInternalized = false; // DEFAULT: Clean Start (Linked mode)
        private string _statusText = "DISCONNECTED";
        
        private Script _cachedScript;
        private string _compiledCode = "";

        public class ScriptGlobals
        {
            public Dictionary<string, object> Inputs = new Dictionary<string, object>();
            public string scriptPath;
        }

        public DynamicBridgeComponent()
          : base("Dynamic C# Bridge", "CS Bridge",
              "A professional live-coding bridge for C# with AI Auto-Debugging.",
              "Math", "Script")
        {
            _lastCode = @"/* 
   ===========================================================================
   DYNAMIC C# BRIDGE - MASTER MANUAL & AI SYSTEM PROMPT v4.0
   ===========================================================================
   
   INSTRUCTIONS:
   1. LINK: Connect this file path to the 'P' input of the Bridge component.
   2. SYNC: Save this file (Ctrl+S) and Grasshopper updates instantly.
   3. AI LOOP: Copy the prompt below to ChatGPT/Gemini to generate code.
   
   DEBUGGING:
   This bridge generates a unique log: 'bridge_status_[ID].log'.
   If you get an error, provide this log to your AI Assistant.
   The AI will read the stack trace and fix the logic for you.
   
   ---------------------------------------------------------------------------
   [ COPY-PASTE THIS SYSTEM PROMPT TO YOUR AI ASSISTANT ]
   ---------------------------------------------------------------------------
   ""You are an expert Rhino/Grasshopper C# Developer. I am using the 'Dynamic 
   Code Bridge'. This system uses an external .cs file to control a 
   Grasshopper component via meta-programming.
   
   RULES FOR GENERATING CODE:
   1. PARAMETERS: Start the file with tags: // IN: Name or // OUT: Name.
   2. LIBRARIES: Use 'using Rhino.Geometry;' and 'using Grasshopper.Kernel.Types;'.
   3. DATA ACCESS: Use the 'Inputs[""Name""]' dictionary to read values.
   4. TYPE CHECKING: Use 'is GH_Number', 'is GH_Point', etc., to unwrap data.
   5. OUTPUTS: Assign results to variables matching your // OUT tags.
   
   TASK: Generate a script that [DESCRIBE YOUR GOAL HERE]""
   ---------------------------------------------------------------------------
*/

// IN: Radius
// OUT: MySphere

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// --- LIVE EXECUTION AREA ---

double r = 1.0;

// 1. Retrieve & Unwrap Input (AI Pattern)
if (Inputs.ContainsKey(""Radius"") && Inputs[""Radius""] is GH_Number ghn) {
    r = ghn.Value;
} else if (Inputs.ContainsKey(""Radius"") && Inputs[""Radius""] != null) {
    try { r = Convert.ToDouble(Inputs[""Radius""]); } catch { }
}

// 2. Logic (AI Pattern)
Sphere sphere = new Sphere(Point3d.Origin, Math.Max(0.1, r));

// 3. Assign to Output (Matches // OUT: MySphere)
var MySphere = sphere;

// Return execution info
$""C# Bridge: Generated Sphere with Radius {r:F2}""";
        }

        public override void CreateAttributes()
        {
            m_attributes = new DynamicBridgeAttributes(this);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "P", "Path to the .cs file", GH_ParamAccess.item);
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
                OpenFileDialog ofd = new OpenFileDialog { Filter = "C# Script|*.cs", Title = "Link Bridge Code" };
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
                SaveFileDialog sfd = new SaveFileDialog { Filter = "C# Script|*.cs", Title = "Export & Link Master Template", FileName = "bridge_logic.cs" };
                if (sfd.ShowDialog() == DialogResult.OK) {
                    try {
                        File.WriteAllText(sfd.FileName, _lastCode);
                        _currentPath = sfd.FileName;
                        _isInternalized = false;
                        SetupWatcher(_currentPath);
                        this.OnAttributesChanged();
                        this.ExpireSolution(true);
                        MessageBox.Show("Bridge Linked!\nYou can now edit this file in VS Code.\nPath: " + sfd.FileName, "Dynamic C# Bridge");
                    } catch (Exception ex) {
                        MessageBox.Show("Export failed: " + ex.Message);
                    }
                }
            });

            Menu_AppendSeparator(menu);

            Menu_AppendItem(menu, "Open Status Log (Notepad)", (s, e) => {
                try {
                    string logFileName = $"bridge_status_{this.InstanceGuid.ToString().Substring(0, 8)}.log";
                    string logPath = Path.Combine(Path.GetDirectoryName(_currentPath), logFileName);
                    if (File.Exists(logPath)) System.Diagnostics.Process.Start("notepad.exe", logPath);
                    else MessageBox.Show("Log file not found yet. Execute the script first.");
                } catch (Exception ex) { MessageBox.Show("Error opening log: " + ex.Message); }
            }, !string.IsNullOrEmpty(_currentPath));

            Menu_AppendItem(menu, "Force Recompile (Reset Engine)", (s, e) => {
                _cachedScript = null;
                _compiledCode = "";
                this.ExpireSolution(true);
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
                            
                            // SILENT SYNC & REFRESH
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
                    _statusText = "PORTABLE SCRIPT ACTIVE";
                }

                if (SyncParameters(_lastCode)) {
                    this.OnAttributesChanged();
                    // FIX: Avoid 'Expired during solution' by scheduling
                    var doc = OnPingDocument();
                    if (doc != null) {
                        doc.ScheduleSolution(5, d => this.ExpireSolution(false));
                    }
                    return;
                }

                if (!string.IsNullOrEmpty(_lastCode))
                {
                    var globals = new ScriptGlobals { scriptPath = _currentPath };
                    int dynInStart = (_isInternalized || Params.Input.Count == 0 || Params.Input[0].Name != "File Path") ? 0 : 1;

                    for (int i = dynInStart; i < Params.Input.Count; i++) {
                        string name = Params.Input[i].Name;
                        object val = null;
                        try {
                            DA.GetData(i, ref val);
                        } catch { }
                        globals.Inputs[name] = val;
                    }

                    if (_cachedScript == null || _compiledCode != _lastCode)
                    {
                        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                            .Where(a => !a.FullName.Contains("Rhino3dm")) 
                            .Select(a => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(a.Location))
                            .Cast<Microsoft.CodeAnalysis.MetadataReference>()
                            .ToList();

                        var options = ScriptOptions.Default
                            .WithReferences(assemblies)
                            .WithImports("System", "System.Collections", "System.Collections.Generic", "System.Linq", "System.Text", "Rhino", "Rhino.Geometry", "Grasshopper", "Grasshopper.Kernel");

                        _cachedScript = CSharpScript.Create(_lastCode, options, typeof(ScriptGlobals));
                        _cachedScript.Compile();
                        _compiledCode = _lastCode;
                    }

                    var task = _cachedScript.RunAsync(globals);
                    task.Wait();
                    
                    var state = task.Result;
                    
                    // Consolidated Output (OUT) - All info in one place
                    var report = new List<object>();
                    string shortId = this.InstanceGuid.ToString().Substring(0, 8);
                    
                    if (state.ReturnValue != null) report.Add(state.ReturnValue);
                    report.Add("Status: OK");
                    report.Add("Bridge ID: " + shortId);
                    report.Add("Link: " + (_isInternalized ? "STANDALONE" : _currentPath));
                    report.Add("Sync: " + DateTime.Now.ToLongTimeString());
                    
                    string logContent = GetLogContent();
                    if (!string.IsNullOrEmpty(logContent)) report.Add("Log: " + logContent);
                    
                    report.Add("Code: " + _lastCode);
                    DA.SetDataList(0, report);

                    // Dynamic Outputs get their variable values
                    for (int i = 1; i < Params.Output.Count; i++) {
                        var param = Params.Output[i];
                        try {
                            var variable = state.Variables.FirstOrDefault(v => v.Name == param.Name);
                            if (variable != null) {
                                object val = variable.Value;
                                if (val is IEnumerable && !(val is string)) DA.SetDataList(i, (IEnumerable)val);
                                else DA.SetData(i, val);
                            }
                        } catch { }
                    }

                    if (!_isInternalized) ReportStatus("SUCCESS", "Ready", _currentPath);
                }
            }
            catch (Exception ex) 
            {
                _statusText = "EXECUTION ERROR";
                var errReport = new List<string> {
                    "ERROR: " + ex.Message,
                    "Link: " + (_isInternalized ? "STANDALONE" : _currentPath),
                    "Sync: " + DateTime.Now.ToLongTimeString(),
                    "Log: " + GetLogContent(),
                    "Code: " + _lastCode
                };
                DA.SetDataList(0, errReport);
                if (!_isInternalized) ReportStatus("ERROR", ex.Message, _currentPath);
                _cachedScript = null; 
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
                if (!trimmed.StartsWith("//")) continue;
                string content = trimmed.TrimStart('/', ' ');
                
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
                    using (Stream stream = assembly.GetManifestResourceStream("DynamicCodeBridge.logo-c.png")) {
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
            get { return new Guid("6f1a4e22-8d7b-4c3e-965a-8d7b3e1a4c9b"); }
        }

        public string StatusText => _statusText;
    }

    public class DynamicBridgeAttributes : GH_ComponentAttributes
    {
        public DynamicBridgeAttributes(IGH_Component component) : base(component) { }
        protected override void Layout() { base.Layout(); RectangleF rec = Bounds; rec.Height += 12; Bounds = rec; }
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel) {
            base.Render(canvas, graphics, channel);
            if (channel == GH_CanvasChannel.Objects) {
                RectangleF bar = Bounds; bar.Y = bar.Bottom - 12; bar.Height = 12;
                graphics.FillRectangle(Brushes.Black, bar);
                DynamicBridgeComponent owner = (DynamicBridgeComponent)Owner;
                string label = owner.StatusText;
                Font font = new Font(GH_FontServer.Standard.FontFamily, 6);
                StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                graphics.DrawString(label, font, Brushes.White, bar, format);
            }
        }
    }
}
