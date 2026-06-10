# DynamicCodeBridge v1.7.0
### Professional Live-Coding Bridge for Rhino 8 & Grasshopper
**Developed by Nacho Monereo | IAAC Robots Lab**

DynamicCodeBridge is a high-performance, real-time sync system linking external code files (`.cs`, `.py`) directly to Grasshopper. It enables you to write script logic inside professional external IDEs and instantly run, visualize, and debug it on the Grasshopper canvas.

---

## Key Features

* **Instant Live-Sync**: Edit your code in Visual Studio, VS Code, Cursor, or Rider. Saving the file (`Ctrl+S`) re-compiles and re-calculates the Grasshopper definition in milliseconds.
* **Professional IDE Workflows**: Use full IntelliSense, autocomplete, formatting (Black, Prettier, C# Formatters), packages, and Git version control on your Grasshopper scripts.
* **Script Internalization**: Embed external scripts directly inside the Grasshopper component with a single click to make definitions fully standalone and portable.
* **Automatic Rich Inputs**: Annotate your code parameters with tags to dynamically generate and wire Grasshopper controls (Sliders, Toggles, Swatches, Points, Planes, and Strings).
* **Deep Diagnostics**: Instantly generates `bridge_status_[shortId].log` containing compilation error traces, execution status, input/output snapshots, and runtime telemetry.
* **Export ID Binding**: Exported script files are named `bridge_logic_[shortId].cs` / `.py` using the unique 8-character component GUID to keep files cleanly separated.
* **AI-Ready System Prompts**: Exported scripts contain custom AI system prompts in their headers. Copy-paste them directly to ChatGPT, Gemini, or Claude to get instantly aligned code generations.

---

## Professional IDE Integration & Real-Time Sync

Traditionally, editing scripts inside Grasshopper is constrained by simple, built-in text boxes lacking advanced developer tools. DynamicCodeBridge frees you from this limitation:

### 1. External Development
Right-click a C# or Python Bridge component and select **"Export Code File..."**. Open the generated file or its parent folder in your IDE (VS Code, Visual Studio, Cursor, etc.).
* **IntelliSense & Autocomplete**: Write geometry code with full member suggestions, documentation, and parameter info for RhinoCommon, Grasshopper SDK, and system libraries.
* **Diagnostics**: Find syntax issues immediately through IDE red squiggles before execution.

### 2. Live-Reloading Sync
When you modify code in your editor and save (`Ctrl+S`):
1. The plugin's file system watcher detects the file update.
2. The Roslyn (C#) compiler or CPython (Python) engine parses the updated script on the fly.
3. Automatically maps any changed parameter tags to update Grasshopper pins.
4. Instantly triggers a solution recalculation to visualize the results in the Rhino viewport.

> [!TIP]
> This live-sync takes **milliseconds** to execute, creating a seamless feedback loop between typing code in Visual Studio and seeing geometry change in Rhino 8.

---

## Script Internalization (Standalone Mode)

When sharing a `.gh` definition with clients, colleagues, or students, requiring them to manage external `.cs` or `.py` files can be cumbersome. DynamicCodeBridge solves this with **Standalone Mode**:

### How to Internalize Your Code:
1. Right-click the Bridge component on the Grasshopper canvas.
2. Check the option **"Internalize Code (Standalone)"** in the context menu.
3. **What happens**:
   - The first input pin (`File Path`) is removed or hidden.
   - The external file dependency is cut.
   - The latest compiled script code is embedded directly into the component's internal persistent state.
   - The status panel shows: `Link: STANDALONE`.

Now, your Grasshopper file is **fully portable** and self-contained! Anyone with the `DynamicCodeBridge` plugin installed can open, run, and compute the definition without needing any external script files on their machine.

### How to Re-link/Edit:
* If you need to make more changes using an external editor, simply right-click the component and uncheck **"Internalize Code (Standalone)"**. 
* The `File Path` input pin will reappear, allowing you to link a file and edit it in Visual Studio again.

---

## How to Use

1. **Install**: Search for `DynamicCodeBridge` in the Rhino 8 Package Manager (`_PackageManager`) and click Install.
2. **Setup**: Drag the C# or Python Bridge component onto the Grasshopper canvas.
3. **Link**: Right-click the component and select **"Export Code File..."** to create a template, or connect a file path string to the **'P'** input.
4. **Auto-Generate Inputs**: Declare inputs at the top using comment tags (e.g., `// IN: active[toggle], size[0..10=5]`). Grasshopper will automatically construct and wire the sliders/toggles on the canvas.
5. **Clean Input Metadata**: Sliders and custom ranges are specified in the parameter tags. The pin name remains clean (e.g. `size`), while constraints (e.g. `0..10 (Default: 5)`) are automatically added to the parameter description tooltips.

---

## AI-Assisted Workflow

1. **Copy the Prompt**: Copy the pre-configured system prompt at the top of your exported script.
2. **Paste & Prompt**: Paste the prompt into ChatGPT, Claude, or Gemini, then describe the geometry or logic you want.
3. **Debug with Logs**: If the component turns red, copy the contents of `bridge_status_[shortId].log` and paste it to the AI. It will analyze the variables and stack trace to return fixed code instantly.

---

## Rules for Developers

### Python (CPython)
- Declare dependencies at the very top using `# r: library_name`.
- Declare inputs using `# IN: Name[type]` and outputs using `# OUT: Name1, Name2`.
- Use `Inputs = dict(Inputs)` at the beginning for safe dictionary conversions.

### C# (Roslyn)
- Declare inputs using `// IN: Name[type]` and outputs using `// OUT: Name1, Name2`.
- Use type-safe retrieval: `Convert.ToDouble(Inputs["Name"])` or pattern match using Grasshopper types (e.g., `is GH_Number`).

---

## About IAAC Robots Lab
The **Institute for Advanced Architecture of Catalonia (IAAC)** is a center for research, education, and development at the intersection of architecture, robotics, and digital fabrication.

**Developer:** Nacho Monereo
**Laboratory:** IAAC Robots Lab

