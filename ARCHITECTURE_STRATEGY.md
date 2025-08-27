# M-CAD Plugin: Architectural Strategy

This document outlines the key architectural strategies for ensuring the plugin is maintainable, scalable, and compatible across multiple versions of Revit.

## 1. Multi-Version Revit API Support (2020-2026)

To handle API differences between Revit versions, the project will adopt a multi-project solution structure with a shared core and version-specific integration layers. This ensures the bulk of the code is version-agnostic while isolating the parts that need to change.

### 1.1. Solution Structure
The solution will be organized into the following projects:

-   **`Manicotti.Core`:** A .NET Standard or .NET Framework class library containing all version-agnostic logic. This includes the core interfaces (`ICadImporter`, etc.), business logic, data models, and algorithms that do not directly touch the Revit API. This project will **not** reference `RevitAPI.dll`.

-   **`Manicotti.Revit.Common`:** A project that references a baseline version of the Revit API (e.g., 2020) and contains code that is known to be compatible across all targeted versions. This includes shared UI components (ViewModels, base classes) and common Revit API utility methods.

-   **`Manicotti.Revit_VXXXX` (e.g., `Manicotti.Revit_2023`):** A separate project for each Revit version (or group of versions) that has significant API breaking changes. This project will reference the specific `RevitAPI.dll` for that version. It will contain implementations of version-specific interfaces defined in the common layer.

-   **`Manicotti.Addin`:** The final add-in project that brings everything together. The build process will be configured to generate a separate output folder for each target Revit version, containing the correct combination of common and version-specific DLLs.

### 1.2. Abstraction Layer Design
The core principle is to depend on abstractions, not on concrete, version-specific implementations.

-   **Interfaces:** Any service that makes a direct Revit API call that is known to have changed between versions will be abstracted behind an interface (e.g., `IWallCreator`, `IFloorCreator`). These interfaces will be defined in `Manicotti.Revit.Common`.

-   **Conditional Compilation:** Within the version-specific implementation files, C# conditional compilation directives (`#if REVIT2020`, `#elif REVIT2023`) will be used to switch between different API calls. The corresponding compilation constants (e.g., `REVIT2023`) will be defined in each version-specific `.csproj` file.

-   **Dependency Injection:** The application's entry point (`App.cs`) will be responsible for setting up a simple dependency injection container. At startup, it will register the correct version-specific implementation for each interface, ensuring the rest of the application remains decoupled from any specific Revit API version.

## 2. Build and Deployment
-   The MSBuild process will be configured to build all version-specific targets.
-   A post-build script will be used to copy the correct set of DLLs and the `.addin` manifest into a versioned folder structure (e.g., `bin/2020/`, `bin/2023/`), ready for packaging or deployment. This ensures that the add-in for Revit 2023 only contains DLLs compiled against the Revit 2023 API.

## 3. Performance and Memory Strategy for Mega-Projects

To ensure the plugin remains responsive and can process extremely large files, the following strategies will be implemented.

### 3.1. Multi-Threading
The core workflow will be divided between background threads and the main Revit UI thread to maximize responsiveness.
-   **Background Thread:** All non-Revit-API-dependent, computationally intensive tasks will be executed on a background thread (`Task.Run`). This includes:
    -   Reading the source CAD file from disk.
    -   Parsing the raw geometry and text data.
    -   Running the `RepairEngine` to clean the data.
    -   Performing the initial analysis to categorize and group elements.
-   **Main Revit UI Thread:** All interactions with the Revit API will be executed on the main thread, managed via the `RevitTaskHandler` (`IExternalEventHandler`). This includes:
    -   Creating the preview graphics.
    -   Building the final Revit elements (Walls, Columns, etc.) within transactions.

### 3.2. Partitioning and Streaming (Lazy Loading)
For mega-projects, loading the entire file into memory at once is not feasible. The `ICadImporter` will be designed to support a streaming or partitioned approach.
-   **Strategy:** Instead of returning a single `CadData` object, the importer can be designed to return an `IEnumerable<CadDataChunk>`, where each chunk represents a specific region of the drawing (e.g., determined by a spatial grid) or a set of layers.
-   **Processing:** The main processing pipeline will iterate through these chunks, processing one at a time (Repair -> Analyze -> Build). This keeps the peak memory usage low and allows the UI to show progress as each chunk is completed.

### 3.3. Caching
To avoid redundant calculations, a simple caching mechanism will be used.
-   **What to Cache:**
    -   **Computed Geometry:** Results of expensive calculations, such as the cleaned `CurveLoop` for a complex room boundary.
    -   **Family Symbols:** Once a `FamilySymbol` for a specific type (e.g., a "600x600mm" column) is found or created, it will be cached in a dictionary to avoid repeated lookups or creation.
-   **Implementation:** A simple `Dictionary<string, object>` can serve as an in-memory cache for the duration of the command's execution.
-   **Eviction:** For a single command run, an explicit eviction policy is not strictly necessary. The cache will be naturally cleared when the command finishes. For more advanced, session-long caching, a memory-aware policy (like Least Recently Used - LRU) would be considered.
