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

// IN: Radius[0.0..10.0=5.0], Active[boolean], Color[color], Pt[point], Pl[plane], Msg[text]
// OUT: MySphere, Status

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// --- LIVE EXECUTION AREA ---

// 1. Declare output variables at script scope (vital for Roslyn scripting outputs)
object MySphere = null;
object Status = null;

try {
    // 2. Retrieve & Unwrap Inputs safely
    double radius = 5.0;
    if (Inputs.ContainsKey("Radius") && Inputs["Radius"] != null) {
        radius = Convert.ToDouble(Inputs["Radius"].ToString());
    }

    bool active = true;
    if (Inputs.ContainsKey("Active") && Inputs["Active"] != null) {
        if (Inputs["Active"] is bool b) active = b;
        else if (Inputs["Active"] is GH_Boolean ghb) active = ghb.Value;
        else bool.TryParse(Inputs["Active"].ToString(), out active);
    }

    Point3d center = Point3d.Origin;
    if (Inputs.ContainsKey("Pt") && Inputs["Pt"] != null) {
        if (Inputs["Pt"] is Point3d p) center = p;
        else if (Inputs["Pt"] is GH_Point ghp) center = ghp.Value;
    }

    Plane plane = Plane.WorldXY;
    if (Inputs.ContainsKey("Pl") && Inputs["Pl"] != null) {
        if (Inputs["Pl"] is Plane pl) plane = pl;
        else if (Inputs["Pl"] is GH_Plane ghpl) plane = ghpl.Value;
    }

    string msg = "Hello from C# Bridge!";
    if (Inputs.ContainsKey("Msg") && Inputs["Msg"] != null) {
        msg = Inputs["Msg"].ToString();
    }

    // 3. Geometry logic
    if (active) {
        MySphere = new Sphere(center, Math.Max(0.1, radius));
        Status = $"C# Bridge Active | Sphere created at {center} with Radius: {radius:F2} | Msg: {msg}";
    } else {
        MySphere = null;
        Status = "C# Bridge Inactive";
    }

} catch (Exception ex) {
    throw new Exception("Execution Error: " + ex.Message, ex);
}