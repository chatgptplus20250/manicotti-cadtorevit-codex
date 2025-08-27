# Product Requirements Document (PRD) - M-CAD v1.0

This document defines the scope, features, and constraints for the M-CAD plugin.

## 1. System Overview
M-CAD is a Revit plugin designed to automate the conversion of 2D CAD drawings (DWG, DXF, DGN) into 3D, data-rich Revit models. It includes an intelligent analysis and repair engine to handle common inaccuracies in CAD files, providing a reliable and efficient workflow for architects and engineers.

## 2. Supported Inputs

### 2.1. File Formats
- **DWG:** All versions, with a focus on AutoCAD 2000 and newer.
- **DXF:** Both ASCII and binary formats.
- **DGN:** MicroStation v7 and v8 formats.

### 2.2. Drawing Conventions
- **Units:** The system must correctly interpret drawings created in `mm`, `cm`, `m`, `inches`, and `feet`.
- **Coordinate System:** The system must handle drawings with arbitrary UCS and rotation relative to the World Coordinate System.
- **External References (XREFs):** The system must be able to process geometry from loaded XREFs. Unresolved XREFs will be ignored with a warning.
- **Focus:** The primary focus is on 2D plan views. 3D blocks or entities will be noted, but their 3D geometry will not be directly translated.

## 3. Expected Outputs

### 3.1. Revit Elements
The primary output is a set of native Revit elements, including:
- Walls (Straight and Curved)
- Doors & Windows (Hosted in walls)
- Columns (Structural & Architectural)
- Beams / Structural Framing
- Floors / Slabs (including openings)
- Rooms

### 3.2. Data Exports
- **Quantities:** Schedules and quantities for all created elements.
- **File Formats:** Export of quantity data to **Excel (.xlsx)** and **CSV**.
- **Diagnostics Report:** A detailed analysis report exported in **JSON** format.

## 4. System Constraints
- **Host Application:** The plugin must run natively inside Autodesk Revit, with full support for versions **2020 through 2026**.
- **Dependencies:** The plugin should minimize reliance on external binary dependencies that have restrictive licensing. Any required libraries must be legally distributable with the plugin.
- **Performance:** The plugin must be memory-aware and performant, capable of handling large models without causing Revit to become unresponsive or crash.

## 5. Performance & Quality Targets

The following targets define the minimum acceptable performance and quality for v1.0.

### 5.1. Throughput
-   **Analysis:** Process a CAD file with 100,000 curve entities in **under 5 minutes** on a standard 8-core developer machine.
-   **End-to-End Build:** A full conversion (walls, doors, windows, columns, floors) for a typical high-rise floor plan should complete in **under 10 minutes**.

### 5.2. Recognition Accuracy
-   **Walls:** ≥ **95%** of valid, clean wall candidates in a drawing must be correctly converted into Revit walls.
-   **Hosted Elements:** ≥ **98%** of valid door and window blocks must be correctly hosted on their respective wall axes (within a ±5 mm tolerance).

### 5.3. Stability
-   **Crashes:** The plugin must have **zero hard crashes** when processing valid but complex or "dirty" CAD files. It should degrade gracefully by logging warnings and skipping problematic entities.
-   **Memory:** The plugin's additional memory working set should remain **under 1.5 GB** when processing large files. There must be **no memory leaks** across five consecutive runs on the same project.
