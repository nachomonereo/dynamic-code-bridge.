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
   
   TASK: Generate a script that [DESCRIBE YOUR GOAL HERE]"
   ---------------------------------------------------------------------------
*/

// IN: Radius[0.0..10.0=5.0], Active[boolean], Color[color], Pt[point], Pl[plane], Msg[text]
// OUT: MySphere, Status

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// --- LIVE EXECUTION AREA ---

try {
    double r = (Inputs.ContainsKey("Radius") && Inputs["Radius"] != null) 
        ? Convert.ToDouble(Inputs["Radius"].ToString()) : 5.0;

    var MySphere = new Sphere(Point3d.Origin, Math.Max(0.1, r));

    string Status = $"C# Bridge Ready | Radius: {r:F2}";

} catch (Exception ex) {
    throw new Exception("Diagnostic Error: " + ex.Message, ex);
}