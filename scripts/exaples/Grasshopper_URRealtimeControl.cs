// Grasshopper Script Instance
#region Usings
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

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
    // PERSISTENT NETWORK STATE
    // ============================================================
    private TcpClient _client = null;
    private NetworkStream _stream = null;
    private string _lastIP = "";
    
    // HIGH-PRECISION BACKGROUND TIMER (Metronome)
    // Runs at 25Hz to ensure smooth, jitter-free robotic movement
    private System.Timers.Timer _metronome = null;
    
    // THREAD-SAFE SHARED VARIABLES
    private readonly object _lockObj = new object();
    private List<double> _goalTarget = null;
    private List<double> _currentTarget = null;
    private bool _isCartesian = true;

    // Disconnect and cleanup resources
    private void Disconnect()
    {
        if (_metronome != null) { _metronome.Stop(); _metronome.Dispose(); _metronome = null; }
        try { if (_stream != null) { _stream.Close(); _stream = null; } } catch {}
        try { if (_client != null) { _client.Close(); _client = null; } } catch {}
    }

    // Ensure connection to the Universal Robot Controller (Port 30003)
    private void EnsureConnection(string IP)
    {
        if (_client != null && _client.Connected && IP == _lastIP) return;

        Disconnect();
        try
        {
            _client = new TcpClient();
            _client.NoDelay = true; // Disable Nagle's algorithm for low latency
            _client.Connect(IP, 30003); 
            _stream = _client.GetStream();
            _lastIP = IP;
            
            // Re-initialize high-speed metronome (40ms = 25Hz)
            _metronome = new System.Timers.Timer(40);
            _metronome.Elapsed += OnMetronomeTick;
            _metronome.AutoReset = true;
            _metronome.Start();
        }
        catch (Exception e)
        {
            Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Connection Failed: " + e.Message);
        }
    }

    // SYSTEM METRONOME: Decouples Grasshopper UI from Robot Communication
    private void OnMetronomeTick(object sender, System.Timers.ElapsedEventArgs e)
    {
        List<double> commandPayload = null;
        bool sendAsPose = true;

        lock(_lockObj)
        {
            if (_goalTarget == null || _goalTarget.Count != 6) return;
            if (_currentTarget == null || _currentTarget.Count != 6) {
                _currentTarget = new List<double>(_goalTarget);
            }
            
            // Easing/Smoothing Logic (10% interpolation per 40ms)
            // This prevents mechanical jerks and smooths out tracking noise
            for(int i = 0; i < 6; i++) {
                _currentTarget[i] += (_goalTarget[i] - _currentTarget[i]) * 0.10; 
            }
            
            commandPayload = new List<double>(_currentTarget);
            sendAsPose = _isCartesian;
        }

        if (_client != null && _client.Connected && _stream != null)
        {
            try {
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                string v0 = commandPayload[0].ToString(culture);
                string v1 = commandPayload[1].ToString(culture);
                string v2 = commandPayload[2].ToString(culture);
                string v3 = commandPayload[3].ToString(culture);
                string v4 = commandPayload[4].ToString(culture);
                string v5 = commandPayload[5].ToString(culture);

                string coords = $"[{v0},{v1},{v2},{v3},{v4},{v5}]";
                string script = "";

                // SMART SMOOTHING (ANTI-NOISE) PARAMS:
                // gain=100 -> Low proportional gain for soft robotic response
                // lookahead_time=0.2 -> Buffer for future trajectory smoothing
                // t=0.08 -> Generous time window for the servoj command
                if (sendAsPose) {
                    script = $"servoj(get_inverse_kin(p{coords}), t=0.08, lookahead_time=0.2, gain=100)\n";
                } else {
                    script = $"servoj({coords}, t=0.08, lookahead_time=0.2, gain=100)\n";
                }

                byte[] data = Encoding.ASCII.GetBytes(script);
                _stream.Write(data, 0, data.Length);
            } catch {
                // Background error handling
            }
        }
    }

    private void RunScript(
        bool active,
        string robotIP,
        List<double> jointOrPoseTarget,
        bool isCartesian,
        ref object Status)
    {
        if (!active)
        {
            Disconnect();
            _lastIP = "";
            lock(_lockObj) {
                _goalTarget = null;
                _currentTarget = null;
            }
            Status = "Control Disconnected.";
            return;
        }

        // Establish connection if needed
        EnsureConnection(robotIP);

        if (_client != null && _client.Connected && _stream != null)
        {
            if (jointOrPoseTarget != null && jointOrPoseTarget.Count == 6)
            {
                lock(_lockObj) {
                    _goalTarget = new List<double>(jointOrPoseTarget);
                    _isCartesian = isCartesian;
                }
                Status = "UR Real-Time Thread Active: Sending Anti-Noise Commands at 25Hz.";
            }
            else
            {
                Status = "Standby: Target must be a list of 6 numbers (Joints or P[x,y,z,rx,ry,rz]).";
            }
        }
        else
        {
            Status = "Connecting to Robot Controller...";
        }
    }
}
