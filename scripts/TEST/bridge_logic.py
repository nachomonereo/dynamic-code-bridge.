#! python3

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

# IN: SetA, SetB, Tolerance[0.001..0.1=0.01], SelfIntersect[boolean]
# OUT: Intersections, Status

import Rhino.Geometry as rg

# INPUT PARAMETERS TYPE REFERENCE:
# - SetA: List of rg.Curve (First set of curves to intersect)
# - SetB: List of rg.Curve (Second set of curves to intersect)
# - Tolerance: Double (Numeric Range: 0.001..0.1, Default: 0.01)
# - SelfIntersect: Boolean (Type: bool, Default: False)

# 1. Retrieve Input Safely
Inputs = dict(Inputs)

def to_list(val):
    if val is None: return []
    if isinstance(val, (str, rg.GeometryBase, rg.Point3d, rg.Vector3d)):
        return [val]
    if hasattr(val, "__iter__"):
        return list(val)
    return [val]

def get_num(key, default):
    val = Inputs.get(key)
    if val is None: return default
    return val.Value if hasattr(val, 'Value') else val

# Initialize outputs at module level
Intersections = None
Status = None

try:
    list_a = to_list(Inputs.get('SetA'))
    list_b = to_list(Inputs.get('SetB'))
    tol = float(get_num('Tolerance', 0.01))
    self_int = bool(get_num('SelfIntersect', False))

    pts = []
    
    # Curve-Curve intersections between SetA and SetB
    for crv_a in list_a:
        if not isinstance(crv_a, rg.Curve): continue
        for crv_b in list_b:
            if not isinstance(crv_b, rg.Curve): continue
            
            events = rg.Intersect.Intersection.CurveCurve(crv_a, crv_b, tol, tol)
            if events:
                for ev in events:
                    pts.append(ev.PointA)

    # Optional self-intersections within SetA curves
    if self_int and len(list_a) > 1:
        for i in range(len(list_a)):
            crv_1 = list_a[i]
            if not isinstance(crv_1, rg.Curve): continue
            for j in range(i + 1, len(list_a)):
                crv_2 = list_a[j]
                if not isinstance(crv_2, rg.Curve): continue
                events = rg.Intersect.Intersection.CurveCurve(crv_1, crv_2, tol, tol)
                if events:
                    for ev in events:
                        pts.append(ev.PointA)

    Intersections = pts
    Status = f"Found {len(pts)} intersection points. (SelfIntersect={self_int})"
    print(Status)

except Exception as e:
    raise e