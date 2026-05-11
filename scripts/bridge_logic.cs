/* 
   ===========================================================================
   DYNAMIC C# BRIDGE - MASTER MANUAL & AI SYSTEM PROMPT v4.0
   ===========================================================================
   
   INSTRUCTIONS:
   1. LINK: Connect this file path to the 'P' input of the Bridge component.
   2. SYNC: Save this file (Ctrl+S) and Grasshopper updates instantly.
   3. AI LOOP: Copy the prompt below to ChatGPT/Gemini to generate code.
   
   ---------------------------------------------------------------------------
   [ COPY-PASTE THIS SYSTEM PROMPT TO YOUR AI ASSISTANT ]
   ---------------------------------------------------------------------------
   "You are an expert Rhino/Grasshopper C# Developer. I am using the 'Dynamic 
   Code Bridge'. This system uses an external .cs file to control a 
   Grasshopper component via meta-programming.
   
   RULES FOR GENERATING CODE:
   1. PARAMETERS: Start the file with tags: // IN: Name or // OUT: Name.
   2. LIBRARIES: Use 'using Rhino.Geometry;' and 'using Grasshopper.Kernel.Types;'.
   3. DATA ACCESS: Use the 'Inputs["Name"]' dictionary to read values.
   4. TYPE CHECKING: Use 'is GH_Number', 'is GH_Point', etc., to unwrap data.
   5. OUTPUTS: Assign results to variables matching your // OUT tags.
   
   TASK: Generate a script that [DESCRIBE YOUR GOAL HERE]"
   ---------------------------------------------------------------------------
*/

// IN: Radius
// OUT: MySphere

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// --- LIVE EXECUTION AREA ---

double r = 1.0;

// 1. Retrieve & Unwrap Input (AI Pattern)
if (Inputs.ContainsKey("Radius") && Inputs["Radius"] is GH_Number ghn) {
    r = ghn.Value;
} else if (Inputs.ContainsKey("Radius") && Inputs["Radius"] != null) {
    try { r = Convert.ToDouble(Inputs["Radius"]); } catch { }
}

// 2. Logic (AI Pattern)
Sphere sphere = new Sphere(Point3d.Origin, Math.Max(0.1, r));

// 3. Assign to Output (Matches // OUT: MySphere)
var MySphere = sphere;

// Return execution info
$"C# Bridge: Generated Sphere with Radius {r:F2}";
