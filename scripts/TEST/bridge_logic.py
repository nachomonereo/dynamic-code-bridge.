# ! python3
# r: numpy

# IN: XCount, YCount, Amplitude, Frequency
# OUT: TerrainPoints, Wireframe

import Rhino.Geometry as rg
import numpy as np

# 1. Inputs with defaults safely handled
nx = int(Inputs.get('XCount', 20)) if Inputs.get('XCount') is not None else 20
ny = int(Inputs.get('YCount', 20)) if Inputs.get('YCount') is not None else 20
amp = float(Inputs.get('Amplitude', 5.0)) if Inputs.get('Amplitude') is not None else 5.0
freq = float(Inputs.get('Frequency', 0.2)) if Inputs.get('Frequency') is not None else 0.2

# 2. Logic using Numpy
x = np.linspace(0, 50, nx)
y = np.linspace(0, 50, ny)
X, Y = np.meshgrid(x, y)
Z = amp * np.sin(freq * X) * np.cos(freq * Y)

# 3. Geometry Generation
TerrainPoints = []
Wireframe = []

for i in range(ny):
    row_pts = []
    for j in range(nx):
        p = rg.Point3d(X[i,j], Y[i,j], Z[i,j])
        TerrainPoints.append(p)
        row_pts.append(p)
    if len(row_pts) > 1:
        Wireframe.append(rg.PolylineCurve(row_pts))

# Vertical lines for wireframe
for j in range(nx):
    col_pts = [rg.Point3d(X[i,j], Y[i,j], Z[i,j]) for i in range(ny)]
    if len(col_pts) > 1:
        Wireframe.append(rg.PolylineCurve(col_pts))

print("Python 3 Bridge: Terrain generated successfully.")
"Engine: CPython 3.10"