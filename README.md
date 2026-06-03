# 🌮 DynamicCodeBridge v1.5.1
### Professional Live-Coding Bridge for Rhino 8 & Grasshopper
**Developed by Nacho Monereo | IAAC Robots Lab**

[![Rhino 8](https://img.shields.io/badge/Rhino-8-blue.svg)](https://www.rhino3d.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Version](https://img.shields.io/badge/Yak-1.5.1-green.svg)](https://yak.rhino3d.com/packages/DynamicCodeBridge)

DynamicCodeBridge is a high-performance, real-time link between external code files (`.cs`, `.py`) and Grasshopper. It enables instant parametric synchronization, automatic rich inputs creation, and deep diagnostic telemetry specifically designed for AI-assisted workflows (ChatGPT, Gemini, Claude).

---

## 🚀 Key Features

- **⚡ Instant Sync**: Edit your code in VS Code, Cursor, or any text editor. Saving the file (Ctrl+S) updates the Grasshopper canvas in milliseconds.
- **🛠️ Automatic Rich Inputs**: Annotate your code parameters with tags to dynamically generate and wire Grasshopper controls:
  - Sliders (`[slider]` or `[min..max=val]`)
  - Boolean Toggles (`[boolean]` or `[toggle]`)
  - Color Swatches (`[color]` or `[swatch]`)
  - Point Parameters (`[point]` or `[pt]`)
  - Plane Parameters (`[plane]` or `[pl]`)
  - Text Parameters (`[text]` or `[string]`)
- **🆔 Export ID Binding**: Exported script files are named `bridge_logic_[shortId].cs` / `.py` using the unique 8-character component GUID to keep files cleanly separated.
- **🤖 Pre-Configured AI System Prompts**: Exported scripts contain custom AI system prompts in the header. Simply copy-paste it to your AI of choice to establish context immediately.
- **🔬 Deep Diagnostics**: Real-time generation of `bridge_status_[shortId].log` containing error traces, execution status, input/output value snapshots, and runtime telemetry.
- **🐍 CPython Native**: Full support for Rhino 8's new CPython engine, including automatic package management via `# r:` tags.

---

## 📖 How to Use

1. **Install**: Search for `DynamicCodeBridge` in the Rhino 8 Package Manager (`_PackageManager`).
2. **Setup**: Drag the C# or Python Bridge component onto the Grasshopper canvas.
3. **Link**: Right-click the component and select **"Export Code File..."** to create a master template, or connect a path to the **'P'** input.
4. **Develop**: Open the linked file in your editor. The file includes instructions, system prompts, and default parameters.
5. **Auto-Generate Inputs**: Declare inputs at the top using comments (e.g. `// IN: active[toggle], size[0..10=5]`). Grasshopper will automatically construct and wire them.

---

## 🤖 AI-Assisted Workflow

1. **Copy the Prompt**: Copy the pre-configured system prompt at the top of your exported file.
2. **Paste & Request**: Paste the prompt into ChatGPT or Gemini. Tell it what geometry or logic you want to generate.
3. **Debug with Logs**: If the component turns red, supply the `bridge_status_[shortId].log` file directly to the AI. It will analyze the variables and stack trace and return the fixed code instantly.

---

## 🛠️ Rules for Developers

### Python (CPython)
- Declare dependencies at the very top using `# r: library_name`.
- Declare inputs using `# IN: Name[type]` and outputs using `# OUT: Name1, Name2`.
- Use `Inputs = dict(Inputs)` at the beginning for safe dictionary conversions.

### C# (Roslyn)
- Declare inputs using `// IN: Name[type]` and outputs using `// OUT: Name1, Name2`.
- Use type-safe retrieval: `Convert.ToDouble(Inputs["Name"].ToString())` or pattern match using Grasshopper types (e.g., `is GH_Number`).

---

## 🎓 About IAAC Robots Lab
The **Institute for Advanced Architecture of Catalonia (IAAC)** is a center for research, education, investigation, and development at the intersection of architecture, robotics, and digital fabrication.

**Developer:** Nacho Monereo
**Laboratory:** IAAC Robots Lab
