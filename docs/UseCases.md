# User Journeys & Use Cases

This document defines the primary user flows for interacting with the M-CAD plugin.

## 1. Quick Convert (Beginner Wizard)

**Persona:** An architect or engineer who is not a Revit power user. They want to convert a standard CAD drawing into a basic Revit model as quickly as possible with minimal configuration.

**Journey:**
1.  **Start:** User clicks the "Build Model from CAD" button in the Revit ribbon.
2.  **Select File:** A file dialog appears. The user selects their DWG file.
3.  **Preview:** The plugin automatically analyzes the file using default settings. The Preview Window appears, showing the colored preview graphics and the diagnostics report.
4.  **Build:** The user reviews the preview, sees that it looks correct, and clicks the "Build Model" button.
5.  **Result:** The plugin creates the 3D Revit model in a few minutes.
6.  **(Optional) Export:** The user can then use a separate command to export a basic Bill of Quantities to Excel.

## 2. Advanced Convert (Power User)

**Persona:** A BIM manager or senior technician who needs fine-grained control over the conversion process. They work with non-standard layer conventions or need to adjust tolerances for a specific project.

**Journey:**
1.  **Configure:** Before conversion, the user opens the M-CAD "Settings" panel.
2.  **Customize Tolerances:** The user adjusts the geometry cleaning tolerances (e.g., `gapToleranceMm`).
3.  **Customize Layer Map:** The user edits the `layer-map.json` file (or a project-specific copy) to match the unique layer names in their DWG file. They might add new wildcard rules.
4.  **Select Importer:** The user chooses a specific CAD importer (e.g., switch to the DXF importer for a specific file).
5.  **Start & Select:** The user runs the "Build Model from CAD" command and selects their file.
6.  **Preview & Iterate:** The Preview Window appears. The user carefully examines the diagnostics report. If they see issues, they may cancel, adjust the settings further, and re-run the command.
7.  **Build:** Once satisfied with the preview and diagnostics, the user clicks "Build Model".
8.  **Result:** The plugin creates a highly accurate Revit model that respects their custom configuration.

## 3. Batch Convert (BIM Operations)

**Persona:** A user responsible for converting a large number of drawings for a major project, such as all the floor plans for a high-rise tower.

**Journey:**
1.  **Setup:** The user prepares a folder containing all the DWG files to be converted. They ensure a master `layer-map.json` and `Tolerances.json` are configured correctly for the project.
2.  **Start Batch:** The user clicks a "Batch Process Folder" button in the Revit ribbon.
3.  **Select Folder:** A folder browser dialog appears. The user selects the folder containing the DWGs.
4.  **Run:** The plugin begins processing the files one by one, without showing the interactive Preview window for each. The main Revit window shows overall progress (e.g., "Processing file 5 of 20...").
5.  **Result:** After the batch process is complete, the user receives a consolidated report summarizing the results for all files. Each converted model might be saved as a separate Revit project or as linked models in a central container project. A detailed JSON report is saved for each processed file.
