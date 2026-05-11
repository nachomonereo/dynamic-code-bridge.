# r: numpy
# !python3
# ===========================================================================
# 🌮 DYNAMIC CODE BRIDGE - MASTER MANUAL v1.4.0
# Developed by Nacho Monereo | IAAC
# ===========================================================================
#
# 📖 INSTRUCTIONS:
# 1. LINK: Connect this file path to the 'P' input of the Bridge component.
# 2. SYNC: Save (Ctrl+S) in your editor and Grasshopper updates instantly.
# 3. LIBRARIES: Add '# r: library_name' at the very top to auto-install.
# 4. AUTO-DEBUGGING: If an error occurs, the Bridge generates a '.log' file. PROVIDE THIS LOG TO YOUR AI (ChatGPT/Gemini). It contains the stack trace and variable states needed to fix the code automatically.
#
# 🤖 [ AI SYSTEM PROMPT - COPY & PASTE TO CHATGPT/GEMINI ]
# ---------------------------------------------------------------------------
# "You are an expert Rhino/Grasshopper Python Developer. I am using the 
# 'Dynamic Code Bridge' for Rhino 8 (CPython). 
# 
# MANDATORY RULES FOR GENERATING CODE:
# 1. HEADERS: Use '# r: library' on Line 1 for dependencies.
# 2. TAGS: Use '# IN: Name1, Name2' and '# OUT: Name1, Name2' to sync pins.
# 3. COMPATIBILITY: Always start with 'Inputs = dict(Inputs)'.
# 4. DATA ACCESS: Use 'val.Value if hasattr(val, ""Value"") else val' for numbers.
# 5. LISTS: Always validate if an input is a list before iterating.
# 6. OUTPUTS: Assign results to variables matching your # OUT tags."
# ---------------------------------------------------------------------------

# IN: Radius
# OUT: MySphere

import Rhino.Geometry as rg

# 1. COMPATIBILITY LAYER
# We convert the .NET dictionary to a native Python dict.
Inputs = dict(Inputs)

def get_num(key, default):
    val = Inputs.get(key)
    if val is None: return default
    # Extract the .Value from Grasshopper types (GH_Number, GH_Integer)
    return val.Value if hasattr(val, 'Value') else val

try:
    # 2. INPUT RECOVERY
    # Pattern: get_num('PinName', defaultValue)
    r = float(get_num('Radius', 1.0))

    # 3. GEOMETRY LOGIC
    # Your parametric logic goes here.
    MySphere = rg.Sphere(rg.Point3d.Origin, max(0.1, r))

    # 4. STATUS REPORT
    print('Python Bridge Ready | Sphere Radius: {0:.2f}'.format(r))
    'Status: OK'

except Exception as e:
    # Diagnostic Log will capture the full StackTrace and Input Snapshot.
    raise e
