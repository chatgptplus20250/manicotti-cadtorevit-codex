# Revit API Compatibility Matrix (2020-2026)

This document tracks known breaking changes in the Revit API across the supported versions and outlines the strategy for handling them.

## 1. Conditional Compilation Symbols

The following symbols will be defined in the corresponding version-specific `.csproj` files to enable conditional compilation of API calls. This allows a single codebase to handle different API method signatures.

- `RVT2020`
- `RVT2021`
- `RVT2022`
- `RVT2023`
- `RVT2024`
- `RVT2025`
- `RVT2026`

Additionally, consolidated symbols (e.g., `#if RVT2022_OR_GREATER`) will be used where a change affects multiple subsequent versions.

## 2. API Change Log & Workarounds

This table serves as a living document to track version-specific API calls and the chosen workaround strategy.

| Feature / API Call | Version(s) Affected | Description of Change | Implementation Notes |
| :--- | :--- | :--- | :--- |
| **Unit Conversions** | Pre-2021 vs. 2021+ | `UnitUtils.ConvertFromInternalUnits` and `ConvertToInternalUnits` were deprecated in favor of methods using the `ForgeTypeId` class for units. | Create a wrapper utility method. The wrapper will use conditional compilation (`#if RVT2021_OR_GREATER`) to call the correct underlying `UnitUtils` method, presenting a consistent interface to the rest of the application. |
| **Wall Creation** | Pre-2022 vs. 2022+ | The static method `Wall.Create` signature was changed in Revit 2022 to directly require the `wallTypeId` and `levelId` as arguments. | Abstract wall creation behind an `IWallCreator` interface. The version-specific implementation will use the correct `Wall.Create` overload. This isolates the change and keeps the core logic clean. |
| **.NET Framework** | Pre-2025 vs. 2025+ | Revit 2025 and later use .NET Core (e.g., .NET 8), while previous versions use .NET Framework 4.8. | This is a major breaking change. The solution must use multi-targeting. The core business logic will be in a .NET Standard 2.0 library. The Revit-specific projects will be multi-targeted to build against both .NET Framework 4.8 and the relevant .NET Core version. |
| **BuiltInParameter Naming**| Pre-2023 vs. 2023+ | Some `BuiltInParameter` enum members were renamed for clarity (e.g., `WALL_USER_HEIGHT_PARAM` became `WALL_BASE_CONSTRAINT`). | Use `#if` directives for the specific parameters that changed, or create a helper utility that returns the correct enum value based on the compilation symbols. |
| **Transaction Handling**| All versions | Minor differences in how `TransactionGroup` or sub-transactions behave can occur. | All model-modifying code will be centralized in service classes that are thoroughly tested on all target versions. No direct transaction management from the UI logic. |
| *(More entries to be added as discovered)* | | | |
