# Project Standards and Assumptions for M-CAD Revit Plugin

This document outlines the assumed standards for development, including layer naming conventions and feature priorities for version 1.0. These are based on common industry practices and can be refined as needed.

## 1. Assumed Layer Naming Conventions

The plugin will use the following layer names to identify and model elements from DWG files. The system supports prefixes and both English and Arabic names.

| Element Type | English Layer Name(s) | Arabic Layer Name(s) | Description |
| :--- | :--- | :--- | :--- |
| **Columns** | `S-COLS`, `A-COL` | `أعمدة`, `ع` | Structural columns. |
| **Beams** | `S-BEAM`, `S-FRAM` | `كمرات`, `ك` | Structural beams and framing. |
| **Slabs** | `S-SLAB`, `A-FLOR-SLAB` | `بلاطات`, `ب` | Structural slabs. |
| **Walls** | `A-WALL` | `جدران`, `ح` | Architectural and structural walls. |
| **Openings** | `A-OPENING` | `فتحات` | General openings in walls or slabs. |
| **Doors** | `A-DOOR` | `أبواب` | Door blocks or outlines. |
| **Windows** | `A-WIND` | `شبابيك` | Window blocks or outlines. |
| **Floor Frame** | `G-FRAME`, `G-BORDER` | `إطار`, `حدود` | Polyline defining the outer boundary of a floor plan. |
| **Slab Thickness**| `A-SLAB-TEXT` | `سماكة-بلاطة` | Text indicating slab thickness. |
| **Room/Space** | `A-ROOM-ID` | `فراغ`, `غرفة` | Text defining a room's name or number. |

## 2. Assumed v1.0 Feature Priorities

This priority list will guide the development of the first major release.

1.  **Priority 1: Core Bug Fixes & Stability**
    *   **Task:** Fix the incorrect placement of special-shaped columns.
    *   **Task:** Improve performance by reusing families for identical special columns.
    *   **Rationale:** A stable and reliable core is essential before adding new features.

2.  **Priority 2: Foundational Beam Modeling**
    *   **Task:** Implement the `CreateBeam` command to model beams from single or double lines on the "BEAM" layer.
    *   **Rationale:** Beams are a fundamental structural element and a major missing feature.

3.  **Priority 3: Revit 2020-2024 Compatibility**
    *   **Task:** Update the project to ensure full compatibility with Revit versions 2020 through 2024.
    *   **Rationale:** Broad version support is critical for market adoption. (2025/2026 will follow).

4.  **Priority 4: Enhanced Slab Modeling**
    *   **Task:** Add support for a "SLAB" layer and detect thickness from text.
    *   **Rationale:** Moves beyond basic floors to more detailed slab modeling, another key user request.

5.  **Priority 5: Basic DWG Error Correction**
    *   **Task:** Implement logic to heal small gaps in polylines and remove duplicate lines.
    *   **Rationale:** Improves the reliability of the modeling process on real-world, imperfect files.
