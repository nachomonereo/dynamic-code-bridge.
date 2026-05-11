// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    // ============================================================
    // THE GLOBAL SOCKET REGISTRY (Rhino-wide shared memory)
    // ============================================================
    private void EnsureGlobalListener(int port, bool reset)
    {
        string cKey = "GlobalClient_" + port;
        string dKey = "GlobalData_" + port;
        string sKey = "GlobalStop_" + port;

        // Force Reset if requested
        if (reset) {
            var old = AppDomain.CurrentDomain.GetData(cKey) as UdpClient;
            if (old != null) { try { old.Close(); old.Dispose(); } catch { } }
            AppDomain.CurrentDomain.SetData(cKey, null);
            AppDomain.CurrentDomain.SetData(sKey, true);
            AppDomain.CurrentDomain.SetData(dKey, "System Reset");
            Thread.Sleep(100);
        }

        // Start Listener if inactive
        if (AppDomain.CurrentDomain.GetData(cKey) == null)
        {
            try {
                UdpClient c = new UdpClient();
                c.ExclusiveAddressUse = false;
                c.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                c.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                
                AppDomain.CurrentDomain.SetData(cKey, c);
                AppDomain.CurrentDomain.SetData(sKey, false);
                AppDomain.CurrentDomain.SetData(dKey, "Waiting for UDP...");

                Thread t = new Thread(() => {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    while (!(bool)AppDomain.CurrentDomain.GetData(sKey)) {
                        try {
                            if (c.Available > 0) {
                                byte[] b = c.Receive(ref ep);
                                string json = Encoding.UTF8.GetString(b);
                                AppDomain.CurrentDomain.SetData(dKey, json);
                            } else { Thread.Sleep(1); }
                        } catch { break; }
                    }
                });
                t.IsBackground = true; t.Start();
            } catch (Exception e) { Print("Socket Error: " + e.Message); }
        }
    }

    private string GetLatestJson(int port) { 
        return AppDomain.CurrentDomain.GetData("GlobalData_" + port) as string; 
    }

    // ============================================================
    // PERSISTENCE (Calibration Store)
    // ============================================================
    private Plane _currentMasterPlane = Plane.Unset;
    private Plane _currentRhinoGoal = Plane.WorldXY;
    private bool _loaded = false;

    private string GetPath() {
        string f = @"C:\Users\nacho\.gemini\antigravity\scratch\mediapipe_grasshopper";
        if (this.GrasshopperDocument != null && !string.IsNullOrEmpty(this.GrasshopperDocument.FilePath))
            f = Path.GetDirectoryName(this.GrasshopperDocument.FilePath);
        return Path.Combine(f, "Tracker_Calibration.txt");
    }

    private void Save(Plane m, Plane g) {
        try {
            string[] s = new string[18]; var cult = System.Globalization.CultureInfo.InvariantCulture;
            s[0]=m.Origin.X.ToString(cult); s[1]=m.Origin.Y.ToString(cult); s[2]=m.Origin.Z.ToString(cult); s[3]=m.XAxis.X.ToString(cult); s[4]=m.XAxis.Y.ToString(cult); s[5]=m.XAxis.Z.ToString(cult); s[6]=m.YAxis.X.ToString(cult); s[7]=m.YAxis.Y.ToString(cult); s[8]=m.YAxis.Z.ToString(cult);
            s[9]=g.Origin.X.ToString(cult); s[10]=g.Origin.Y.ToString(cult); s[11]=g.Origin.Z.ToString(cult); s[12]=g.XAxis.X.ToString(cult); s[13]=g.XAxis.Y.ToString(cult); s[14]=g.XAxis.Z.ToString(cult); s[15]=g.YAxis.X.ToString(cult); s[16]=g.YAxis.Y.ToString(cult); s[17]=g.YAxis.Z.ToString(cult);
            File.WriteAllLines(GetPath(), s);
        } catch { }
    }

    private void Load() {
        try {
            string p = GetPath();
            if (File.Exists(p)) {
                string[] l = File.ReadAllLines(p); var c = System.Globalization.CultureInfo.InvariantCulture;
                if (l.Length >= 9) _currentMasterPlane = new Plane(new Point3d(double.Parse(l[0],c), double.Parse(l[1],c), double.Parse(l[2],c)), new Vector3d(double.Parse(l[3],c), double.Parse(l[4],c), double.Parse(l[5],c)), new Vector3d(double.Parse(l[6],c), double.Parse(l[7],c), double.Parse(l[8],c)));
                if (l.Length >= 18) _currentRhinoGoal = new Plane(new Point3d(double.Parse(l[9],c), double.Parse(l[10],c), double.Parse(l[11],c)), new Vector3d(double.Parse(l[12],c), double.Parse(l[13],c), double.Parse(l[14],c)), new Vector3d(double.Parse(l[15],c), double.Parse(l[16],c), double.Parse(l[17],c)));
            }
        } catch { }
    }

    // ============================================================
    // MAIN SCRIPT EXECUTION
    // ============================================================
    private void RunScript(
        bool reset,
        bool calibrate,
        double scale,
        object Target_Plane,
        object Station_Plane,
        ref object IDs,
        ref object Cal_Planes,
        ref object Meta_Data,
        ref object Master_Calib_Plane)
    {
        if (!_loaded) { Load(); _loaded = true; }
        EnsureGlobalListener(6001, reset);
        
        string lastJson = GetLatestJson(6001);
        if (scale == 0) scale = 1.0;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        // Determination of Goal vs Target
        Plane activeGoal = _currentRhinoGoal;
        if (Target_Plane != null) {
            activeGoal = (Target_Plane is Plane p) ? p : (Target_Plane is GH_Plane ghp) ? ghp.Value : Plane.WorldXY;
        }

        // Determination of Master Anchor
        bool injected = false;
        Plane anchor = _currentMasterPlane;
        if (Station_Plane != null) {
            Plane sp = (Station_Plane is Plane p1) ? p1 : (Station_Plane is GH_Plane ghp1) ? ghp1.Value : Plane.Unset;
            if (sp != Plane.Unset) { anchor = sp; injected = true; }
        }

        // Global Mapping Matrix
        Transform xform = Transform.Identity;
        if (anchor != Plane.Unset) {
            // Mapping Anchor (Physical) -> Goal (Rhino)
            xform = Transform.PlaneToPlane(anchor, activeGoal);
        }

        // Processing Lists
        List<int> trkIDs = new List<int>();
        List<Plane> trkPlanes = new List<Plane>();
        DataTree<object> trkMeta = new DataTree<object>();

        if (!string.IsNullOrEmpty(lastJson) && lastJson.StartsWith("{")) {
            string[] blocks = lastJson.Split(new string[] { "\"device_id\":" }, StringSplitOptions.None);

            // CALIBRATION LOGIC
            if (calibrate && !injected) {
                Plane masterP = Plane.Unset;
                for (int i = 1; i < blocks.Length; i++) {
                    MatchCollection vm = Regex.Matches(blocks[i], "\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+),\\s*\"z\":\\s*([0-9.-]+)");
                    if (vm.Count >= 3) {
                        Point3d o = new Point3d(double.Parse(vm[0].Groups[1].Value, culture) * scale, -double.Parse(vm[0].Groups[3].Value, culture) * scale, double.Parse(vm[0].Groups[2].Value, culture) * scale);
                        Vector3d x = new Vector3d(double.Parse(vm[1].Groups[1].Value, culture), -double.Parse(vm[1].Groups[3].Value, culture), double.Parse(vm[1].Groups[2].Value, culture));
                        Vector3d y = new Vector3d(-double.Parse(vm[2].Groups[1].Value, culture), double.Parse(vm[2].Groups[3].Value, culture), -double.Parse(vm[2].Groups[2].Value, culture));
                        masterP = new Plane(o, x, y); break;
                    }
                }
                if (masterP != Plane.Unset) {
                    _currentMasterPlane = masterP; _currentRhinoGoal = activeGoal;
                    Save(_currentMasterPlane, _currentRhinoGoal); Print("Calibration Saved Globally.");
                    anchor = _currentMasterPlane;
                    xform = Transform.PlaneToPlane(anchor, activeGoal);
                }
            }

            // DATA EXTRACTION (Strict Metadata Structure)
            for (int i = 1; i < blocks.Length; i++) {
                string block = blocks[i];
                Match idM = Regex.Match(block, @"\s*([0-9]+)\s*,");
                int id = idM.Success ? int.Parse(idM.Groups[1].Value) : -1;
                trkIDs.Add(id);

                Match tM = Regex.Match(block, "\"type\":\\s*\"(.*?)\"");
                string typeStr = tM.Success ? tM.Groups[1].Value : "Tracker";

                MatchCollection vm2 = Regex.Matches(block, "\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+),\\s*\"z\":\\s*([0-9.-]+)");
                if (vm2.Count >= 3) {
                    Point3d o = new Point3d(double.Parse(vm2[0].Groups[1].Value, culture) * scale, -double.Parse(vm2[0].Groups[3].Value, culture) * scale, double.Parse(vm2[0].Groups[2].Value, culture) * scale);
                    Vector3d x = new Vector3d(double.Parse(vm2[1].Groups[1].Value, culture), -double.Parse(vm2[1].Groups[3].Value, culture), double.Parse(vm2[1].Groups[2].Value, culture));
                    Vector3d y = new Vector3d(-double.Parse(vm2[2].Groups[1].Value, culture), double.Parse(vm2[2].Groups[3].Value, culture), -double.Parse(vm2[2].Groups[2].Value, culture));
                    
                    Plane pT = new Plane(o, x, y);
                    pT.Transform(xform);
                    trkPlanes.Add(pT);

                    // --- STRICT META DATA STORAGE ---
                    GH_Path path = new GH_Path(id);
                    trkMeta.Add(typeStr, path);
                    trkMeta.Add(pT, path);

                    if (typeStr.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0) {
                        Match trM = Regex.Match(block, "\"trigger\":\\s*(true|false)");
                        Match grM = Regex.Match(block, "\"grip\":\\s*(true|false)");
                        Match meM = Regex.Match(block, "\"menu\":\\s*(true|false)");
                        Match pdM = Regex.Match(block, "\"trackpad_click\":\\s*(true|false)");
                        Point3d pdP = Point3d.Origin;
                        Match pxM = Regex.Match(block, "\"trackpad_pos\":\\s*\\{\\s*\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+)\\s*\\}");
                        if (pxM.Success) pdP = new Point3d(double.Parse(pxM.Groups[1].Value, culture), double.Parse(pxM.Groups[2].Value, culture), 0);

                        trkMeta.Add(trM.Success && trM.Groups[1].Value == "true", path);
                        trkMeta.Add(grM.Success && grM.Groups[1].Value == "true", path);
                        trkMeta.Add(meM.Success && meM.Groups[1].Value == "true", path);
                        trkMeta.Add(pdM.Success && pdM.Groups[1].Value == "true", path);
                        trkMeta.Add(pdP, path);
                    }
                }
            }
        }

        IDs = trkIDs; Cal_Planes = trkPlanes; Meta_Data = trkMeta;
        Master_Calib_Plane = (anchor != Plane.Unset) ? anchor : null;
        if (trkIDs.Count == 0) Print("UDP Server Status: " + (lastJson ?? "OFFLINE"));
    }
}
