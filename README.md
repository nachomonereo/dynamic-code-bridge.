# 🌮 Dynamic Code Bridge v1.4.0
### Professional Live-Coding Bridge for Rhino 8 & Grasshopper
**Developed by Nacho Monereo | IAAC Robots Lab**

[![Rhino 8](https://img.shields.io/badge/Rhino-8-blue.svg)](https://www.rhino3d.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/Yak-1.4.0-green.svg)](https://yak.rhino3d.com/packages/DynamicCodeBridge)

Dynamic Code Bridge is a high-performance link between external code files (.cs, .py) and Grasshopper. It enables real-time parametric development with instant synchronization, atomic parameter updates, and deep diagnostic telemetry designed for AI-assisted debugging.

---

## 🚀 Key Features

- **⚡ Instant Sync**: Edit your code in VS Code or any IDE, hit Save (Ctrl+S), and Grasshopper updates in milliseconds.
- **🛡️ Atomic Sync (v1.3+)**: Safe parameter registration that prevents UI crashes when adding or removing inputs/outputs on the fly.
- **🐍 CPython Native**: Full support for Rhino 8's new CPython engine, including automatic package management via `# r:` tags.
- **🔬 Deep Diagnostics**: Automatic generation of `bridge_status_[ID].log` files containing full stack traces, input snapshots, and source code context—optimized for **AI Debugging (ChatGPT/Gemini)**.
- **🏛️ Institutional Quality**: Developed at **IAAC Robots Lab** for advanced architectural research and robotic control.

---

## 📖 How to Use

1. **Install**: Search for `DynamicCodeBridge` in the Rhino 8 Package Manager (`_PackageManager`).
2. **Setup**: Drag the C# or Python Bridge component onto the Grasshopper canvas.
3. **Link**: Right-click the component and select "Export Code File" or connect an existing path to the **'P'** input.
4. **Develop**: Open the file in your favorite editor. The file includes a **Master Manual** and a **System Prompt** for your AI Assistant.

---

## 🤖 AI-Assisted Workflow

This bridge is designed to be used with AI assistants. 
1. **Copy the Prompt**: Every exported file contains a pre-configured System Prompt.
2. **Generate Code**: Paste it into your AI of choice (ChatGPT, Gemini, Claude).
3. **Debug with Logs**: If the component turns red, simply provide the generated `.log` file to your AI. It will analyze the error and provide a fix instantly.

---

## 🛠️ Rules for Developers

### Python (CPython)
- Use `# r: library` on the first line to install dependencies.
- Use `Inputs = dict(Inputs)` for safe data access.
- Avoid f-strings in headers for maximum engine compatibility.

### C# (Roslyn)
- Use `// IN: Name` and `// OUT: Name` tags to define pins.
- Use `Convert.ToDouble(Inputs["Name"].ToString())` for type-safe number extraction.

---

## 🎓 About IAAC Robots Lab
The **Institute for Advanced Architecture of Catalonia (IAAC)** is a center for research, education, investigation, and development at the intersection of architecture, robotics, and digital fabrication.

**Developer:** Nacho Monereo
**Laboratory:** IAAC Robots Lab
