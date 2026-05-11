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
  // ============================================================
  // INSTANCE MEMORY (Safe & Isolated)
  // ============================================================
  // Using private fields instead of static to prevent 
  // memory locks and cross-component interference.
  private DataTree<object> _storedTree = new DataTree<object>();
  private bool _hasSnapshot = false;

  private void RunScript(
    DataTree<object> Data,
    bool Update,
    ref object Result)
  {
    // 1. INPUT VALIDATION
    if (Data == null) return;

    // 2. CAPTURE LOGIC (Only on Trigger)
    if (Update)
    {
      // Create a fresh copy to prevent reference flickering
      DataTree<object> newSnapshot = new DataTree<object>();
      
      foreach (GH_Path path in Data.Paths)
      {
        var branch = Data.Branch(path);
        if (branch != null)
        {
          newSnapshot.AddRange(branch, path);
        }
      }
      
      _storedTree = newSnapshot;
      _hasSnapshot = true;
      Component.Message = "STABLE";
    }

    // 3. OUTPUT LOGIC
    if (_hasSnapshot)
    {
      Result = _storedTree;
    }
    else
    {
      Result = null;
      Component.Message = "EMPTY";
    }
  }
}
