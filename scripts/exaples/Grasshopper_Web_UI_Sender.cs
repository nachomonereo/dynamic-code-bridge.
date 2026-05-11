// --- REQUISITOS DE INPUTS: ---
// Boundary (Rectangle3d), Rects (List Rectangle3d), Colors (List Colour), Texts (List String), IP (String), Send (Boolean)

#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Rhino.Geometry;
#endregion

public class Script_Instance : GH_ScriptInstance
{
  private void RunScript(Rectangle3d Boundary, List<Rectangle3d> Rects, List<Color> Colors, List<string> Texts, string IP, bool Send, ref object JSON_Debug)
  {
    // FIX: Using .IsValid instead of == null
    if (!Send || !Boundary.IsValid || Rects == null || string.IsNullOrEmpty(IP)) {
        JSON_Debug = "System Waiting...";
        return;
    }

    double bW = Boundary.Width;
    double bH = Boundary.Height;
    Point3d min = Boundary.Corner(0);

    StringBuilder sb = new StringBuilder();
    sb.Append("{\"elements\":[");

    for (int i = 0; i < Rects.Count; i++)
    {
        Rectangle3d r = Rects[i];
        if (!r.IsValid) continue; // Skip invalid rectangles

        Point3d rMin = r.Corner(0);
        
        // Percentages calculation
        double x = ((rMin.X - min.X) / bW) * 100.0;
        double y = (1.0 - ((r.Corner(3).Y - min.Y) / bH)) * 100.0;
        double w = (r.Width / bW) * 100.0;
        double h = (r.Height / bH) * 100.0;

        Color c = (i < Colors.Count) ? Colors[i] : Color.Gray;
        string hex = string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        string txt = (i < Texts.Count) ? Texts[i] : "";

        sb.Append("{");
        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, 
            "\"x\":{0:F2},\"y\":{1:F2},\"w\":{2:F2},\"h\":{3:F2},", x, y, w, h);
        sb.AppendFormat("\"color\":\"{0}\",\"text\":\"{1}\"", hex, txt);
        sb.Append("}");

        if (i < Rects.Count - 1) sb.Append(",");
    }

    sb.Append("]}");

    string json = sb.ToString();
    JSON_Debug = json;

    try {
        using (UdpClient client = new UdpClient()) {
            byte[] data = Encoding.UTF8.GetBytes(json);
            client.Send(data, data.Length, IP, 6005);
        }
    } catch (Exception ex) {
        Print("Send Error: " + ex.Message);
    }
  }
}
