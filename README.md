# 🌮 Dynamic Code Bridge (v1.0.1)
Developed by **Nacho Monereo** | Credits to: **IAAC (Institute for Advanced Architecture of Catalonia)**

A high-performance, real-time live-coding suite for Rhino 8 and Grasshopper. This plugin breaks the barrier between external IDEs and the Grasshopper canvas, enabling a professional software development workflow within a visual programming environment.

---

## 🚀 Key Features

### 🔹 C# Dynamic Bridge
- **Powered by Roslyn**: High-speed compilation of external `.cs` files.
- **Native Types**: Seamless handling of `Rhino.Geometry` and `Grasshopper.Kernel.Types`.
- **Global Scope**: Access to `RhinoDoc.ActiveDoc` and custom `Inputs` dictionary.

### 🔹 Python Dynamic Bridge
- **Rhino 8 CPython**: Fully compatible with the new Python 3 engine in Rhino 8.
- **Library Support**: Easy integration with `numpy`, `pandas`, and other CPython libraries.
- **Native Dictionary**: Injects a native Python dictionary for ultra-fast data access.

### 🔹 Meta-Programming Engine
- **Dynamic Pins**: Define your inputs and outputs directly in code comments:
  - C#: `// IN: Radius, Height` | `// OUT: Geometry`
  - Python: `# IN: Radius, Height` | `# OUT: Geometry`
- **Zero-Latency Sync**: Components update their interface the moment you save in your favorite editor (VS Code, Cursor, etc.).

### 🔹 AI-Assisted Auto-Debugging 🤖
- **Automated Logs**: Generates unique `bridge_status_[GUID].log` files for every instance.
- **Closed-Loop Error Correction**: Simply feed the log file to an AI (ChatGPT, Claude, Gemini), and it will understand the stack trace and provide fixed code.

---

## 🛠 Installation & Usage

1. **Install via Yak**: Open Rhino 8 and run `_PackageManager`. Search for **"Dynamic Code Bridge"**.
2. **Setup**: Drag the C# or Python Bridge component to the canvas.
3. **Link**: Right-click the component and select **"Export Code to File..."** to generate your Master Template.
4. **Live Code**: Open the generated file in VS Code. Start coding, and watch Grasshopper react in real-time.

---

## 📂 Project Structure
- `BridgeComponent.cs`: Core C# live-coding engine.
- `PythonBridgeComponent.cs`: Rhino 8 CPython integration.
- `scripts/`: Example scripts and master templates.

---
© 2026 Nacho Monereo | IAAC
