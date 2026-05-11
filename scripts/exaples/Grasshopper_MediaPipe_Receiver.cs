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
    private void EnsureGlobalListener(int port, bool reset)
    {
        string cKey = "MP_Client"; // Shared key
        string dKey = "MP_Data_" + port;
        string pKey = "MP_CurrentPort"; // Track active port

        var boundPort = AppDomain.CurrentDomain.GetData(pKey) as int?;
        bool portChanged = boundPort != null && boundPort != port;

        if (reset || portChanged) {
            var old = AppDomain.CurrentDomain.GetData(cKey) as UdpClient;
            if (old != null) { try { old.Close(); old.Dispose(); } catch { } }
            AppDomain.CurrentDomain.SetData(cKey, null);
            AppDomain.CurrentDomain.SetData(pKey, null);
            AppDomain.CurrentDomain.SetData(dKey, "System Reset/Port Changed.");
            Thread.Sleep(100);
            if (reset) return; 
        }

        if (AppDomain.CurrentDomain.GetData(cKey) == null) {
            try {
                UdpClient c = new UdpClient();
                c.ExclusiveAddressUse = false;
                c.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                c.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                
                AppDomain.CurrentDomain.SetData(cKey, c);
                AppDomain.CurrentDomain.SetData(pKey, port);
                AppDomain.CurrentDomain.SetData(dKey, "Connected.");

                Thread t = new Thread(() => {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    while (true) {
                        try {
                            var cur = AppDomain.CurrentDomain.GetData(cKey) as UdpClient;
                            if (cur == null || cur != c) break;
                            if (cur.Available > 0) {
                                byte[] b = cur.Receive(ref ep);
                                AppDomain.CurrentDomain.SetData(dKey, Encoding.UTF8.GetString(b));
                            } else { Thread.Sleep(1); }
                        } catch { break; }
                    }
                });
                t.IsBackground = true; t.Start();
            } catch (Exception ex) { Print("Socket Error: " + ex.Message); }
        }
    }

    private void RunScript(
        int Port,
        bool reset,
        double scale,
        ref object Pts,
        ref object Lns,
        ref object Gestures,
        ref object Handedness,
        ref object FacePlane,
        ref object FaceNodes)
    {
        EnsureGlobalListener(Port, reset);
        string dKey = "MP_Data_" + Port;
        string json = AppDomain.CurrentDomain.GetData(dKey) as string;
        
        double s = (scale == 0) ? 100.0 : scale;
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        DataTree<Point3d> pointsTree = new DataTree<Point3d>();
        DataTree<Line> linesTree = new DataTree<Line>();
        List<string> gesturesList = new List<string>();
        List<string> handednessList = new List<string>();
        List<Point3d> facePtsList = new List<Point3d>();
        Plane headPlane = Plane.Unset;

        if (!string.IsNullOrEmpty(json) && json.StartsWith("{")) {
            // 1. Extract Face Mesh Data
            Match noseM = Regex.Match(json, "\"nose\":\\s*\\{\\s*\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+),\\s*\"z\":\\s*([0-9.-]+)\\s*\\}");
            Match chinM = Regex.Match(json, "\"chin\":\\s*\\{\\s*\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+),\\s*\"z\":\\s*([0-9.-]+)\\s*\\}");
            Match leyeM = Regex.Match(json, "\"left_eye\":\\s*\\{\\s*\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+),\\s*\"z\":\\s*([0-9.-]+)\\s*\\}");
            Match reyeM = Regex.Match(json, "\"right_eye\":\\s*\\{\\s*\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+),\\s*\"z\":\\s*([0-9.-]+)\\s*\\}");
            if (noseM.Success && chinM.Success && leyeM.Success && reyeM.Success) {
                Func<Match, Point3d> map = (m) => new Point3d(double.Parse(m.Groups[1].Value, culture) * s, -double.Parse(m.Groups[2].Value, culture) * s, double.Parse(m.Groups[3].Value, culture) * s);
                Point3d pN = map(noseM); Point3d pC = map(chinM); Point3d pL = map(leyeM); Point3d pR = map(reyeM);
                facePtsList.AddRange(new[] { pN, pC, pL, pR });
                headPlane = new Plane(pN, pL - pR, pN - pC);
            }

            // 2. Extract Hand/Gesture Data
            string[] handBlocks = json.Split(new string[] { "{\"hand\":" }, StringSplitOptions.None);
            for (int i = 1; i < handBlocks.Length; i++) {
                GH_Path path = new GH_Path(i - 1);
                Match gM = Regex.Match(handBlocks[i], "\"gesture\":\\s*\"(.*?)\"");
                Match hM = Regex.Match(handBlocks[i], "\"([^\"]+)\"");
                gesturesList.Add(gM.Success ? gM.Groups[1].Value : "Neutral");
                handednessList.Add(hM.Success ? hM.Groups[1].Value : "Unknown");
                List<Point3d> hPts = new List<Point3d>();
                MatchCollection lmMatches = Regex.Matches(handBlocks[i], "\"x\":\\s*([0-9.-]+),\\s*\"y\":\\s*([0-9.-]+),\\s*\"z\":\\s*([0-9.-]+)");
                foreach (Match m in lmMatches)
                    hPts.Add(new Point3d(double.Parse(m.Groups[1].Value, culture) * s, -double.Parse(m.Groups[2].Value, culture) * s, double.Parse(m.Groups[3].Value, culture) * s));
                pointsTree.AddRange(hPts, path);
                if (hPts.Count >= 21) {
                    int[,] bones = new int[,] { {0,1}, {1,2}, {2,3}, {3,4}, {0,5}, {5,6}, {6,7}, {7,8}, {5,9}, {9,10}, {10,11}, {11,12}, {9,13}, {13,14}, {14,15}, {15,16}, {13,17}, {17,18}, {18,19}, {19,20}, {0,17} };
                    for (int j = 0; j < bones.GetLength(0); j++)
                        linesTree.Add(new Line(hPts[bones[j,0]], hPts[bones[j,1]]), path);
                }
            }
        }
        
        Pts = pointsTree; 
        Lns = linesTree; 
        Gestures = gesturesList; 
        Handedness = handednessList; 
        FacePlane = headPlane; 
        FaceNodes = facePtsList; 

        if (pointsTree.DataCount > 0 || facePtsList.Count > 0) 
            Print("Status: Receiving Camera Data");
        else 
            Print("Status: " + (string.IsNullOrEmpty(json) ? "Waiting for Camera..." : "No targets detected"));
    }
}
