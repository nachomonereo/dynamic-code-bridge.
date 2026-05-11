# Dynamic Code Bridge (v1.0.0)
Developed by **Nacho Monereo IAAC**

A professional live-coding bridge for Rhino/Grasshopper that connects external C# (`.cs`) and Python (`.py`) files directly to the Grasshopper canvas with zero latency and AI-assisted debugging.

## Key Features
- **Instant Sync**: Save in VS Code, update in Grasshopper instantly.
- **AI-Ready**: Embedded system prompts for ChatGPT/Gemini/Claude to generate compliant code.
- **Auto-Debugging**: Generates instance-specific logs for autonomous AI error correction.
- **Rhino 8 Compatible**: Full support for CPython (Rhino 8) and Roslyn (C#).
- **Atomic Start**: Components initialize in a clean state, only showing the `P` and `OUT` ports.

## Installation
1. Download the `DynamicCodeBridge.gha` and its `.dll` dependencies.
2. Place them in your Grasshopper Libraries folder (or use the `.yak` package).
3. Search for "Dynamic C# Bridge" or "Dynamic Python Bridge" in Grasshopper.

## How to use
1. Drag the component to the canvas.
2. Right-click and select **"Export Code to File..."**.
3. Edit the file in VS Code and watch the magic happen.

---
© 2026 Nacho Monereo IAAC
