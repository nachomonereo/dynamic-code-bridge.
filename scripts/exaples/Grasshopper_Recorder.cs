// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    // ===================================
    // PERSISTENT DATA STATE
    // ===================================
    
    // Stores all recorded Takes. Each Take is a list of Frames. Each Frame is a DataTree capturing metadata at that moment.
    private List<List<DataTree<object>>> _recordedTakes = new List<List<DataTree<object>>>();
    
    // Temporary list for the active recording session
    private List<DataTree<object>> _currentTake = new List<DataTree<object>>();
    
    // Recording state flags
    private bool _isRecording = false;
    private double _lastRecordTime = 0.0; 

    // Helper to check if data has changed significantly (to avoid redundant frames)
    private bool IsSimilar(object dataA, object dataB, double tolerance)
    {
        if (dataA == null || dataB == null) return false;
        if (tolerance <= 0.0001) return false; // If tolerance is near 0, record every frame

        if (dataA is Plane pA && dataB is Plane pB)
            return pA.Origin.DistanceTo(pB.Origin) < tolerance;
        else if (dataA is Point3d ptA && dataB is Point3d ptB)
            return ptA.DistanceTo(ptB) < tolerance;
        else if (dataA is GH_Plane gpA && dataB is GH_Plane gpB)
            return gpA.Value.Origin.DistanceTo(gpB.Value.Origin) < tolerance;
        else if (dataA is GH_Point gptA && dataB is GH_Point gptB)
            return gptA.Value.DistanceTo(gptB.Value) < tolerance;
        else if (dataA is double dA && dataB is double dB)
            return Math.Abs(dA - dB) < tolerance;

        return dataA.Equals(dataB);
    }

    // Deep clone of a DataTree to ensure data persistence across solution updates
    private DataTree<object> CloneTree(DataTree<object> tree)
    {
        DataTree<object> clone = new DataTree<object>();
        if (tree == null) return clone;
        foreach (GH_Path p in tree.Paths)
        {
            clone.AddRange(tree.Branch(p).Select(x => x), p); 
        }
        return clone;
    }

    // IMPORTANT: The "Data" input MUST be set to "Tree Access" in the component settings
    private void RunScript(
        bool run,
        bool recordTrigger,
        DataTree<object> Data,
        double samplingDelayMs,
        double changeTolerance,
        bool resetSystem,
        ref object RecordedData)
    {
        if (!run) return;

        // ---- GRASSHOPPER LOOP PROTECTION ----
        // If lists/trees are injected into inputs, GH may trigger multiple iterations in a single frame.
        // We only want to process the data in the first iteration (Iteration 0).
        if (this.Iteration > 0)
        {
             RecordedData = new DataTree<object>();
             return;
        }

        double currentTime = DateTime.Now.TimeOfDay.TotalMilliseconds;

        // 1. SYSTEM RESET
        if (resetSystem)
        {
            _recordedTakes.Clear();
            _currentTake.Clear();
            _isRecording = false;
            RecordedData = new DataTree<object>();
            return;
        }

        // 2. RECORDING LOGIC
        if (recordTrigger)
        {
            // Start a new take
            if (!_isRecording)
            {
                _isRecording = true;
                _currentTake = new List<DataTree<object>>();
                _lastRecordTime = currentTime;
            }

            // Check if it's time to sample a new frame
            if (_currentTake.Count == 0 || (currentTime - _lastRecordTime) >= samplingDelayMs)
            {
                bool shouldCapture = true;
                
                if (Data != null && Data.Paths.Count > 0)
                {
                    // Check if the current data is different enough from the last frame
                    if (_currentTake.Count > 0)
                    {
                        DataTree<object> lastFrame = _currentTake.Last();
                        if (changeTolerance > 0.0001 && lastFrame.Paths.Count > 0)
                        {
                            GH_Path p0 = Data.Paths[0];
                            GH_Path pLast = lastFrame.Paths[0];
                            
                            // Check the second item in the branch (often the coordinate/plane)
                            if (Data.Branch(p0).Count > 1 && lastFrame.Branch(pLast).Count > 1)
                            {
                                if (IsSimilar(Data.Branch(p0)[1], lastFrame.Branch(pLast)[1], changeTolerance))
                                    shouldCapture = false;
                            }
                        }
                    }
                    
                    if (shouldCapture)
                    {
                        _currentTake.Add(CloneTree(Data)); 
                        _lastRecordTime = currentTime; 
                    }
                }
            }
        }
        else if (_isRecording)
        {
            // Finish active take and store it in persistent memory
            if (_currentTake.Count > 0)
            {
                _recordedTakes.Add(new List<DataTree<object>>(_currentTake));
            }
            _isRecording = false;
            _currentTake = new List<DataTree<object>>(); 
        }

    // 3. OUTPUT GENERATION (Constructing a 3-level tree: {Device ; TakeID ; FrameID})
    DataTree<object> outputTree = new DataTree<object>();
    
    // Nested helper function to process a list of frames into the final tree structure
    Action<List<DataTree<object>>, int> extractTake = (take, takeID) => 
    {
        for (int frameID = 0; frameID < take.Count; frameID++) 
        {
            DataTree<object> frameData = take[frameID];
            foreach (GH_Path p in frameData.Paths)
            {
                // We assume the first index of the input path is the Device ID
                int deviceID = p.Indices.Length > 0 ? p.Indices[0] : 0;
                
                // Construct hierarchical path: { Device ; Take ; Frame }
                GH_Path outPath = new GH_Path(deviceID, takeID, frameID);
                outputTree.AddRange(frameData.Branch(p), outPath);
            }
        }
    };

    // Export all saved takes
    for (int i = 0; i < _recordedTakes.Count; i++)
    {
        extractTake(_recordedTakes[i], i);
    }
    
    // Also export the current take in real-time if recording
    if (_isRecording && _currentTake.Count > 0)
    {
        extractTake(_currentTake, _recordedTakes.Count);
    }
    
    RecordedData = outputTree;
    }
}
