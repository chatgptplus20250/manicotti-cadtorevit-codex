# Edge Cases & Worst-Case CAD Patterns

This document specifies the expected behavior for handling common but challenging conditions found in real-world CAD files. The M-CAD plugin must handle these scenarios gracefully.

## 1. Geometric Inaccuracies

| Case | Description | Required Handling |
| :--- | :--- | :--- |
| **Non-Zero Z Values** | Entities in a 2D plan have small, non-zero Z coordinates. | **Repair:** Flatten all geometry to Z=0 before processing, unless explicitly part of a 3D context. |
| **Open Polylines** | Polylines that should represent closed boundaries (e.g., for rooms or slabs) have small gaps between their start and end points. | **Repair:** Automatically close the gap if it is within the `gapToleranceMm` threshold. |
| **Micro-Segments** | Polylines contain extremely short segments (e.g., <50 mm). | **Repair:** Merge/simplify these micro-segments into longer, cleaner lines before tracing walls or boundaries. |
| **Duplicate/Overlapping Entities** | Multiple lines or arcs are drawn directly on top of each other. | **Repair:** Implement an "OVERKILL"-like function to detect and remove duplicate entities, keeping only one. |

## 2. Representation Ambiguity

| Case | Description | Required Handling |
| :--- | :--- | :--- |
| **Curved Elements** | Façades or walls are represented by Arcs or Polylines with bulge segments. | **Handle:** The wall creation algorithm must support `Wall.Create(doc, curve, ...)` with curved profiles. |
| **Non-Uniformly Scaled Blocks**| Door/window blocks have been scaled non-uniformly (e.g., stretched in one direction). | **Handle:** Ignore the block's internal scale. Match the block to a Revit Family Type based on its bounding-box width (± tolerance). |
| **Ambiguous Layers** | A drawing lacks a clear or consistent layering standard. | **Warn:** Process based on best effort using the `layer-map.json`. Log warnings for layers that contain geometry but are not mapped to a known role. |

## 3. File and Project Complexity

| Case | Description | Required Handling |
| :--- | :--- | :--- |
| **Massive Drawings** | The input file contains a very large number of entities (e.g., >= 500k curves). | **Handle:** The system must process the file without crashing or becoming unresponsive, using partitioning and memory management strategies. |
| **Mixed Units** | A drawing contains blocks or XREFs that were created using different units from the main drawing. | **Handle:** The importer must detect and correctly scale geometry from these sources to the main drawing's unit system. |
| **Arbitrary Rotation/UCS** | The entire drawing is rotated relative to the World Coordinate System. | **Handle:** The system must detect the primary orientation of the building (e.g., from grids or bounding box) and apply a transform to align the created Revit model correctly. |
