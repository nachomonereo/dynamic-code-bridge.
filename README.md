# 🌮 Dynamic Code Bridge (v1.1.0)
Developed by **Nacho Monereo** | Credits to: **IAAC (Institute for Advanced Architecture of Catalonia)**

A high-performance, real-time live-coding suite for Rhino 8 and Grasshopper. This plugin breaks the barrier between external IDEs and the Grasshopper canvas, enabling a professional software development workflow within a visual programming environment.

---

## 🚀 Key Features

### 🔹 Python 3 Bridge (NEW in v1.1.0)
- **Rhino 8 CPython Engine**: Migrated to the modern `Rhino.Runtime.Code` API for native Python 3 support.
- **External Libraries**: Support for `# r: library_name` tags. Rhino 8 automatically downloads and manages dependencies like `numpy`, `pandas`, or `requests` via pip.
- **Global Scope**: Access to `RhinoDoc.ActiveDoc` and custom `Inputs` dictionary as global variables.

### 🔹 C# Dynamic Bridge
- **Powered by Roslyn**: High-speed compilation of external `.cs` files.
- **Native Types**: Seamless handling of `Rhino.Geometry` and `Grasshopper.Kernel.Types`.
- **Atomic Start**: Components initialize in a clean state, only showing the `P` and `OUT` ports.

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
3. **External Libraries (Python)**: Add `# r: numpy` at the top of your script to use external packages.
4. **Link**: Right-click the component and select **"Export Code to File..."** to generate your Master Template.
5. **Live Code**: Open the generated file in VS Code. Start coding, and watch Grasshopper react in real-time.

---

## 📂 Project Structure
- `BridgeComponent.cs`: Core C# live-coding engine.
- `PythonBridgeComponent.cs`: Modern Rhino 8 CPython integration.
- `scripts/`: Example scripts and master templates.

---
© 2026 Nacho Monereo | IAAC
