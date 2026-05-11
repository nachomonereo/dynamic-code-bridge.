# r: numpy
# !python3

# IN: SetA, SetB
# OUT: Intersections, Status

import Rhino.Geometry as rg

# 1. COMPATIBILITY LAYER
Inputs = dict(Inputs)

try:
    # 2. DATA RECOVERY
    # Validamos que lo que entra sean listas de curvas
    list_a = Inputs.get('SetA')
    list_b = Inputs.get('SetB')
    
    if not isinstance(list_a, list): list_a = [list_a] if list_a else []
    if not isinstance(list_b, list): list_b = [list_b] if list_b else []

    pts = []
    
    # 3. LOGIC: Doble bucle de intersección
    for crv_a in list_a:
        if not isinstance(crv_a, rg.Curve): continue
        for crv_b in list_b:
            if not isinstance(crv_b, rg.Curve): continue
            
            # Buscamos puntos de cruce
            events = rg.Intersect.Intersection.CurveCurve(crv_a, crv_b, 0.01, 0.01)
            if events:
                for ev in events:
                    pts.Add(ev.PointA)

    Intersections = pts
    Status = "Found {0} intersection points.".format(len(pts))

except Exception as e:
    # Diagnostic Log will capture this
    raise e

print(Status)
"Status: Ready"