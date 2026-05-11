// IN: Count, Spacing, Height
// OUT: Spheres, Connections

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

// --- LIVE EXECUTION AREA ---

int count = 10;
double spacing = 5.0;
double h = 10.0;

// Safe retrieval
if (Inputs.ContainsKey("Count") && Inputs["Count"] != null) count = Convert.ToInt32(Inputs["Count"]);
if (Inputs.ContainsKey("Spacing") && Inputs["Spacing"] != null) spacing = Convert.ToDouble(Inputs["Spacing"]);
if (Inputs.ContainsKey("Height") && Inputs["Height"] != null) h = Convert.ToDouble(Inputs["Height"]);

List<Sphere> spheres = new List<Sphere>();
List<Line> lines = new List<Line>();

for (int i = 0; i < count; i++) {
    Point3d p = new Point3d(i * spacing, 0, Math.Sin(i * 0.5) * h);
    spheres.Add(new Sphere(p, 1.0));
    if (i > 0) {
        lines.Add(new Line(spheres[i-1].Center, p));
    }
}

// Assign to Output variables (matching // OUT tags)
var Spheres = spheres;
var Connections = lines;

// Final status string
$"C# Bridge: Generated {count} items with Sin-Wave height {h}"