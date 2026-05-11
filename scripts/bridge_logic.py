#! python3
# venv: iaac_bridge
# r: rhino-geometry, grasshopper-kernel

"""
===========================================================================
DYNAMIC PYTHON BRIDGE - MASTER MANUAL & AI SYSTEM PROMPT v4.0
===========================================================================

INSTRUCTIONS:
1. ENVIRONMENT: The # venv and # r headers manage Rhino 8 CPython.
2. AI LOOP: Copy the prompt below to ChatGPT/Gemini to generate code.

---------------------------------------------------------------------------
[ COPY-PASTE THIS SYSTEM PROMPT TO YOUR AI ASSISTANT ]
---------------------------------------------------------------------------
"You are a Rhino/Grasshopper Python Expert. I am using the 'Dynamic Python 
Bridge' in Rhino 8 (CPython). This bridge syncs this file to GH.

RULES FOR GENERATING CODE:
1. PARAMETERS: Use # IN: Name or # OUT: Name at the top.
2. LIBRARIES: Import 'Rhino.Geometry as rg'.
3. DATA ACCESS: Use the 'Inputs' dictionary.
4. TYPE CHECKING: Check for GH types (e.g., .Value) or raw Python types.
5. OUTPUTS: Define variables matching your # OUT tags to return data.

TASK: Generate a script that [DESCRIBE YOUR GOAL HERE]"
---------------------------------------------------------------------------
"""

# IN: Radius
# OUT: MySphere

import Rhino.Geometry as rg

# 1. Retrieve Input (AI Pattern)
r = 1.0
if 'Radius' in Inputs and Inputs['Radius'] is not None:
    try:
        # Robust unwrapping of GH_Number or float
        r = float(Inputs['Radius'].Value) if hasattr(Inputs['Radius'], 'Value') else float(Inputs['Radius'])
    except:
        pass

# 2. Logic (AI Pattern)
MySphere = rg.Sphere(rg.Point3d.Origin, max(0.1, r))

# 3. Print execution info
print(f"Python Bridge: Sphere created with R={r:.2f}")
'Python Bridge Ready'
