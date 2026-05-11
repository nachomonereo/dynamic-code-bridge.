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
        string cKey = "UniversalPhone_Client"; // Shared key for the client
        string dKey = "UniversalPhone_Data_" + port;
        string pKey = "UniversalPhone_CurrentPort"; // Keep track of which port is bound

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
                
                AppDomain.CurrentDomain.SetData(cKey, c); // <--- DE VUELTA A SU SITIO
                AppDomain.CurrentDomain.SetData(dKey, new List<string>());
                AppDomain.CurrentDomain.SetData(pKey, port);

                Thread t = new Thread(() => {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    while (true) {
                        try {
                            var cur = AppDomain.CurrentDomain.GetData(cKey) as UdpClient;
                            if (cur == null || cur != c) break;
                            if (cur.Available > 0) {
                                byte[] b = cur.Receive(ref ep);
                                string msg = Encoding.UTF8.GetString(b);
                                
                                // Buffer System: Add to list
                                lock(AppDomain.CurrentDomain) {
                                    var buffer = AppDomain.CurrentDomain.GetData(dKey) as List<string>;
                                    if (buffer != null) buffer.Add(msg);
                                }
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
        ref object RawData,
        ref object Decomposed)
    {
        EnsureGlobalListener(Port, reset);
        string dKey = "UniversalPhone_Data_" + Port;
        
        List<string> latestMessages = new List<string>();
        lock(AppDomain.CurrentDomain) {
            var buffer = AppDomain.CurrentDomain.GetData(dKey) as List<string>;
            if (buffer != null && buffer.Count > 0) {
                latestMessages.AddRange(buffer);
                buffer.Clear(); // Empty for next cycle
            }
        }
        
        RawData = latestMessages;

        // Decomposed logic: List of lists (or simple flattened list)
        List<string> allParts = new List<string>();
        foreach(var m in latestMessages) {
            allParts.AddRange(m.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries));
        }
        Decomposed = allParts;
    }
}
