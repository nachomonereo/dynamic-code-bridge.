# r: numpy
# !python3

# IN: SetA, SetB
# OUT: Intersections, Status

import Rhino.Geometry as rg

# 1. COMPATIBILITY LAYER
Inputs = dict(Inputs)

try:
    # 2. DATA RECOVERY
    def to_list(val):
        if val is None: return []
        if isinstance(val, (str, rg.GeometryBase, rg.Point3d, rg.Vector3d)):
            return [val]
        if hasattr(val, "__iter__"):
            return list(val)
        return [val]

    list_a = to_list(Inputs.get('SetA'))
    list_b = to_list(Inputs.get('SetB'))

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
                    pts.append(ev.PointA)

    Intersections = pts
    Status = "Found {0} intersection points.".format(len(pts))

except Exception as e:
    # Diagnostic Log will capture this
    raise e

print(Status)
"Status: Ready"