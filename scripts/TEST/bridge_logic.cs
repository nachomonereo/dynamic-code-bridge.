// IN: Size, Spacing
// OUT: MatrixPoints, Status

using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

try {
    // 1. Inputs
    int n = (Inputs.ContainsKey("Size") && Inputs["Size"] != null) 
        ? Convert.ToInt32(Inputs["Size"].ToString()) : 10;
        
    double s = (Inputs.ContainsKey("Spacing") && Inputs["Spacing"] != null)
        ? Convert.ToDouble(Inputs["Spacing"].ToString()) : 5.0;

    // 2. Logic: Create a nested list structure
    List<List<Point3d>> grid = new List<List<Point3d>>();
    
    for (int i = 0; i < n; i++) {
        List<Point3d> row = new List<Point3d>();
        for (int j = 0; j < n; j++) {
            row.Add(new Point3d(i * s, j * s, 0));
        }
        grid.Add(row);
    }

    // 3. Outputs
    var MatrixPoints = grid;
    var Status = "C# Iteration 10: Matrix " + n + "x" + n + " generated.";

} catch (Exception ex) {
    throw new Exception("C# Matrix Error: " + ex.Message, ex);
}

"Iteration 10: Stable"