/* 
   🌮 DYNAMIC CODE BRIDGE - MASTER MANUAL v1.3.4
   ===========================================================================
   Developed by Nacho Monereo | IAAC
   ===========================================================================
   
   📖 INSTRUCTIONS:
   1. LINK: Connect this file path to the 'P' input of the Bridge component.
   2. SYNC: Save (Ctrl+S) in your IDE and Grasshopper updates instantly.
   3. ATOMIC SYNC: Add/Remove // IN and // OUT tags to update pins safely.
   4. AUTO-DEBUGGING: If an error occurs, the Bridge generates a '.log' file. PROVIDE THIS LOG TO YOUR AI (ChatGPT/Gemini). It contains the stack trace and variable states needed to fix the code automatically.
   
   🤖 [ AI SYSTEM PROMPT - COPY & PASTE TO CHATGPT/GEMINI ]
   ---------------------------------------------------------------------------
   "You are an expert Rhino/Grasshopper C# Developer. I am using the 
   'Dynamic Code Bridge'. This system uses an external .cs file to control a 
   Grasshopper component via meta-programming.
   
   MANDATORY RULES FOR GENERATING CODE:
   1. TAGS: Start the file with '// IN: Name1, Name2' and '// OUT: Name1, Name2'.
   2. DATA ACCESS: Use 'Convert.ToDouble(Inputs["Name"].ToString())' for numbers.
   3. TYPES: Use 'using Rhino.Geometry;' and 'using Grasshopper.Kernel.Types;'.
   4. LISTS: Check if an input is 'IList' or 'IEnumerable' before iterating.
   5. OUTPUTS: Assign results to variables matching your // OUT tags.
   6. STABILITY: Wrap everything in a 'try-catch' block to feed the logger."
   ---------------------------------------------------------------------------
*/

// IN: Radius
// OUT: MySphere

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// --- LIVE EXECUTION AREA ---

try {
    // 1. SAFE INPUT RECOVERY (Pattern v1.3)
    // We convert the input to string before parsing to handle GH types.
    double r = (Inputs.ContainsKey("Radius") && Inputs["Radius"] != null) 
        ? Convert.ToDouble(Inputs["Radius"].ToString()) : 1.0;

    // 2. GEOMETRY LOGIC
    // Your parametric logic goes here.
    var MySphere = new Sphere(Point3d.Origin, Math.Max(0.1, r));

    // 3. EXECUTION STATUS
    // The output will be shown in the 'OUT' report pin.
    $"C# Bridge Ready | Sphere Radius: {r:F2}";

} catch (Exception ex) {
    // DO NOT REMOVE: This feeds the Deep Diagnostic Log system.
    throw new Exception("Diagnostic Error: " + ex.Message, ex);
}
