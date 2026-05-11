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
          : base("Dynamic C# Bridge", "CsBridge",
              "A real-time C# link for Rhino 8 with Atomic Sync & Diagnostic Logs.",
              "IAAC", "CodeBridge")
        {
            _lastCode = @"/* 
   🌮 DYNAMIC CODE BRIDGE - MASTER MANUAL v1.4.0
   ===========================================================================
   Developed by Nacho Monereo | IAAC
   ===========================================================================
   
   📖 INSTRUCTIONS:
   1. LINK: Connect this file path to the 'P' input of the Bridge component.
   2. SYNC: Save (Ctrl+S) in your editor and Grasshopper updates instantly.
   3. LIBRARIES: Add '# r: library_name' at the very top to auto-install.
   4. AUTO-DEBUGGING: If an error occurs, the Bridge generates a '.log' file. 
      PROVIDE THIS LOG TO YOUR AI (ChatGPT/Gemini). It contains the stack trace 
      and variable states needed to fix the code automatically.
   
   🤖 [ AI SYSTEM PROMPT - COPY & PASTE TO CHATGPT/GEMINI ]
   ---------------------------------------------------------------------------
   ""You are an expert Rhino/Grasshopper C# Developer. I am using the 
   'Dynamic Code Bridge'. This system uses an external .cs file to control a 
   Grasshopper component via meta-programming.
   
   MANDATORY RULES FOR GENERATING CODE:
   1. TAGS: Start the file with '// IN: Name1, Name2' and '// OUT: Name1, Name2'.
   2. DATA ACCESS: Use 'Convert.ToDouble(Inputs[""Name""].ToString())' for numbers.
   3. TYPES: Use 'using Rhino.Geometry;' and 'using Grasshopper.Kernel.Types;'.
   4. LISTS: Check if an input is 'IList' or 'IEnumerable' before iterating.
   5. OUTPUTS: Assign results to variables matching your // OUT tags.
   6. STABILITY: Wrap everything in a 'try-catch' block to feed the logger.""
   ---------------------------------------------------------------------------
*/

// IN: Radius
// OUT: MySphere

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// --- LIVE EXECUTION AREA ---

try {
    // 1. SAFE INPUT RECOVERY (Pattern v1.3)
    // We convert the input to string before parsing to handle GH types.
    double r = (Inputs.ContainsKey(""Radius"") && Inputs[""Radius""] != null) 
        ? Convert.ToDouble(Inputs[""Radius""].ToString()) : 1.0;

    // 2. GEOMETRY LOGIC
    // Your parametric logic goes here.
    var MySphere = new Sphere(Point3d.Origin, Math.Max(0.1, r));

    // 3. EXECUTION STATUS
    // The output will be shown in the 'OUT' report pin.
    $""C# Bridge Ready | Sphere Radius: {r:F2}"";

} catch (Exception ex) {
    // DO NOT REMOVE: This feeds the Deep Diagnostic Log system.
    throw new Exception(""Diagnostic Error: "" + ex.Message, ex);
}
";
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

                    if (!_isInternalized) ReportStatus("SUCCESS", "Ready", _currentPath, globals.Inputs);
                }
            }
            catch (Exception ex) 
            {
                _statusText = "EXECUTION ERROR";
                var errReport = new List<string> {
                    "ERROR: " + ex.Message,
                    "Line: " + (ex.StackTrace ?? "Unknown"),
                    "Link: " + (_isInternalized ? "STANDALONE" : _currentPath),
                    "Sync: " + DateTime.Now.ToLongTimeString()
                };
                DA.SetDataList(0, errReport);
                if (!_isInternalized) ReportStatus("ERROR", ex.Message, _currentPath, null, ex.StackTrace);
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

        private void ReportStatus(string status, string message, string scriptPath, Dictionary<string, object> inputs = null, string stackTrace = null)
        {
            try {
                if (string.IsNullOrEmpty(scriptPath)) return;
                string logFileName = $"bridge_status_{this.InstanceGuid.ToString().Substring(0, 8)}.log";
                string logPath = Path.Combine(Path.GetDirectoryName(scriptPath), logFileName);
                
                using (StreamWriter sw = new StreamWriter(logPath, false)) {
                    sw.WriteLine("===========================================================================");
                    sw.WriteLine($"DYNAMIC BRIDGE DIAGNOSTIC REPORT | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine("===========================================================================");
                    sw.WriteLine($"STATUS: [{status}]");
                    sw.WriteLine($"MESSAGE: {message}");
                    sw.WriteLine($"FILE: {scriptPath}");
                    sw.WriteLine("---------------------------------------------------------------------------");

                    if (!string.IsNullOrEmpty(stackTrace)) {
                        sw.WriteLine("CRITICAL ERROR DETECTED:");
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
