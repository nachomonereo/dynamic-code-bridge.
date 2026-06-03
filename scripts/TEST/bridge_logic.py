#! python3
# venv: iaac_bridge
# r: rhino-geometry, grasshopper-kernel

"""
===========================================================================
DYNAMIC PYTHON BRIDGE - MASTER MANUAL & AI SYSTEM PROMPT v1.5.1
===========================================================================

INSTRUCTIONS:
1. ENVIRONMENT: The # venv and # r headers manage Rhino 8 CPython.
2. AI LOOP: Copy the prompt below to ChatGPT/Gemini to generate code.

DEBUGGING:
This bridge generates a unique log: 'bridge_status_[ID].log'.
If you get an error, provide this log to your AI Assistant.
The AI will read the stack trace and fix the logic for you.

---------------------------------------------------------------------------
[ COPY-PASTE THIS SYSTEM PROMPT TO YOUR AI ASSISTANT ]
---------------------------------------------------------------------------
"You are a Rhino/Grasshopper Python Expert. I am using the 'Dynamic Python 
Bridge' in Rhino 8 (CPython). This bridge syncs this file to GH.

RULES FOR GENERATING CODE:
1. PARAMETERS: Use # IN: Name or # OUT: Name at the top.
   - Supported input types: [slider], [boolean], [color], [point], [plane], [text].
2. LIBRARIES: Import 'Rhino.Geometry as rg'.
3. DATA ACCESS: Use the 'Inputs' dictionary.
4. TYPE CHECKING: Check for GH types (e.g., .Value) or raw Python types.
5. OUTPUTS: Define variables matching your # OUT tags to return data.

TASK: Generate a script that [DESCRIBE YOUR GOAL HERE]"
---------------------------------------------------------------------------
"""

# IN: Radius[0.0..10.0=5.0], Active[boolean], Color[color], Pt[point], Pl[plane], Msg[text]
# OUT: MySphere, Status

import Rhino.Geometry as rg

# 1. Retrieve Input (AI Pattern)
Inputs = dict(Inputs)

def get_num(key, default):
    val = Inputs.get(key)
    if val is None: return default
    return val.Value if hasattr(val, 'Value') else val

try:
    r = float(get_num('Radius', 5.0))
    
    # 2. Logic (AI Pattern)
    MySphere = rg.Sphere(rg.Point3d.Origin, max(0.1, r))
    
    # 3. Assign Outputs
    Status = f"Python Bridge Ready | Radius: {r:.2f}"
    print(Status)

except Exception as e:
    raise e