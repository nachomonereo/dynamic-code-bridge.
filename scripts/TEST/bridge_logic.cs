/* 
   ===========================================================================
   DYNAMIC C# BRIDGE - MASTER MANUAL & AI SYSTEM PROMPT v1.5.1
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
   "You are an expert Rhino/Grasshopper C# Developer. I am using the 'Dynamic 
   Code Bridge'. This system uses an external .cs file to control a 
   Grasshopper component via meta-programming.
   
   RULES FOR GENERATING CODE:
   1. PARAMETERS: Start the file with tags: // IN: Name or // OUT: Name.
      - Supported input types: [slider], [boolean], [color], [point], [plane], [text].
   2. LIBRARIES: Use 'using Rhino.Geometry;' and 'using Grasshopper.Kernel.Types;'.
   3. DATA ACCESS: Use the 'Inputs["Name"]' dictionary to read values.
   4. TYPE CHECKING: Use 'is GH_Number', 'is GH_Point', etc., to unwrap data.
   5. OUTPUTS: Assign results to variables matching your // OUT tags.
      IMPORTANT: Declare output variables at the top-level script scope (outside
      any try-catch block) so they can be captured by the script engine.
   
   TASK: Generate a script that [DESCRIBE YOUR GOAL HERE]"
   ---------------------------------------------------------------------------
*/

// IN: Size[1..50=10], Spacing[1.0..20.0=5.0]
// OUT: MatrixPoints, Status

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// INPUT PARAMETERS TYPE REFERENCE:
// - Size: Int (Numeric Range: 1..50, Default: 10)
// - Spacing: Double (Numeric Range: 1.0..20.0, Default: 5.0)

// --- LIVE EXECUTION AREA ---

// 1. Declare output variables at script scope (vital for Roslyn scripting outputs)
object MatrixPoints = null;
object Status = null;

try {
    // 2. Retrieve inputs safely
    int n = 10;
    if (Inputs.ContainsKey("Size") && Inputs["Size"] != null) {
        n = Convert.ToInt32(Inputs["Size"].ToString());
    }
        
    double s = 5.0;
    if (Inputs.ContainsKey("Spacing") && Inputs["Spacing"] != null) {
        s = Convert.ToDouble(Inputs["Spacing"].ToString());
    }

    // 3. Logic: Create a nested list structure (Point Grid)
    List<List<Point3d>> grid = new List<List<Point3d>>();
    for (int i = 0; i < n; i++) {
        List<Point3d> row = new List<Point3d>();
        for (int j = 0; j < n; j++) {
            row.Add(new Point3d(i * s, j * s, 0));
        }
        grid.Add(row);
    }

    // 4. Assign outputs
    MatrixPoints = grid;
    Status = "C# Iteration 10: Matrix " + n + "x" + n + " generated.";

} catch (Exception ex) {
    throw new Exception("C# Matrix Error: " + ex.Message, ex);
}