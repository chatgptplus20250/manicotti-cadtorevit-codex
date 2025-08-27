# Manicotti :burrito:

![Revit API](https://img.shields.io/badge/Revit%20API-2022-red.svg)
![.NET](https://img.shields.io/badge/.NET-4.8-red.svg)

Revit add-in to build up model automatically based on DWG drawings. This is a toy project in progress and any test or joint development are welcome.  
```
manicotti
├ /Demo
│ ├ *.dwg             - DWG file for testing
│ ├ *.jpg             - Screenshot
│ └ *.dll             - Plugin DLL (Revit 2022)
└ /Manicotti
  ├ /Properties       - Assembly info
  ├ /Resources
  │ ├ /ico            - Button icon files
  │ ├ /lib            - Teigha DLL files
  │ └ /rfa            - Revit family files
  ├ /Util
  │ └ *.cs            - Utility methods
  ├ /Views
  ├ ├ *.xaml          - WPF View document
  │ └ *.cs
  ├ Manicotti.csproj  - Configuration XML
  ├ Manicotti.sln     - VS solution file
  ├ App.cs            - Entry point
  ├ *.cs              - Class library
  └ Manicotti.addin   - Application manifest
```


## Test the Revit Add-in
Test the demo by Revit 2022 (en-US) with all default family libraries installed. The default folder for Revit Add-in is `C:\ProgramData\Autodesk\Revit\Addins\2022\` , or `C:\Users\$username$\AppData\Roaming\Autodesk\Revit\Addins\2022\`.  
The build events of the Visual Studio project will copy all necessary files to that directory after you build the source code. In case that you don't have VS, you need to copy those plugin files manually under that directory. Like this:  
```
ProgramData\Autodesk\Revit\Addins\2022\
├ /Manicotti
│ ├ TD_*.dll        - Copy from /Resources/lib
│ └ Manicotti.dll   - Copy from /Demo
└ Manicotti.addin   - Copy from /Manicotti
```
Then start Revit and (video [ref](https://www.bilibili.com/video/BV17N4y1F7c1/?vd_source=9cd60edb139ebf7808403a2205ee49a1)):  
- Insert -> Link CAD -> `...\Demo\Link_floor.dwg`
- Manicotti -> Settings (check if all .rfa files are loaded)
- Manicotti -> Build up model on all Levels
- Select the linked DWG in the floorplan view (process takes almost 1min)


## Compile the source code
The Manicotti add-in has been tested against Revit 2022. To apply it to other versions you need to rebuild it with correct .NET Framework.  
Revit 2022/2021 - .NET **4.8**  
Revit 2020/2019 - .NET 4.7  
Revit 2018      - .NET 4.6  
 
**REFERENCE** | The project hosts two external references, `RevitAPI.dll` and `RevitAPIUI.dll`. You can locate them under `...\Autodesk\Revit 2022\`  

**BUILD EVENTS** | Set additional macros in post-build event to copy the built files to the Revit add-in folder.
```
if exist "$(AppData)\Autodesk\REVIT\Addins\2022" copy "$(ProjectDir)*.addin" "$(AppData)\Autodesk\REVIT\Addins\2022"
if exist "$(AppData)\Autodesk\REVIT\Addins\2022" mkdir "$(AppData)\Autodesk\REVIT\Addins\2022\Manicotti"
copy "$(ProjectDir)$(OutputPath)*.dll" "$(AppData)\Autodesk\REVIT\Addins\2022\Manicotti"
copy "$(ProjectDir)Resources\rfa\*.rfa" "$(AppData)\Autodesk\REVIT\Addins\2022\Manicotti"
```

**DEBUG** | Within the project property, under DEBUG panel set external program as `...\Autodesk\Revit 2022\Revit.exe`


## What's new

This project uses Teigha for temporary development.  

To-do list moved to [TaskBoard](https://github.com/ian-quinn/manicotti/issues/1)  

A demo is online to build up the building model from CAD drawings. Only core components are covered (wall column window door room floor roof). For now the project still needs more cunning & robust algorithms to sort out layers/components and reshape the geometry, which will be the main theme in the next-phase coding.  

<img src="/Demo/Screenshot.jpg?raw=true">

## Configuration

The plugin's behavior can be customized via configuration files located in the `Resources/config` sub-directory of the plugin installation folder.

### Layer Mapping (`layer-map.json`)

To provide maximum flexibility, the plugin uses a JSON file to map the layer names from your DWG files to the functional roles needed to build the Revit model (e.g., WALL, COLUMN, etc.). This allows you to use any layer naming convention without needing to change the plugin's code.

**Location:** `<Plugin_Install_Folder>/Resources/config/layer-map.json`

**Format:**
The file is a standard JSON object.
-   **Keys:** Represent the functional role of the element. The recognized roles are `WALL`, `COLUMN`, `DOOR`, `WINDOW`, `OPENING`, `SLAB`, `GRID`, and `SPACE`.
-   **Values:** An array of strings, where each string is a layer name from your DWG file that corresponds to that role.

**Matching Rules:**
-   Matching is **case-insensitive**.
-   You can use a wildcard `*` at the end of a layer name to match all layers that start with that prefix. For example, `"WALL_*"` will match `WALL_EXTERIOR`, `WALL_INTERIOR`, etc.

**Example `layer-map.json`:**
```json
{
  "WALL":   ["A-WALL", "WALL_*"],
  "COLUMN": ["A-COL", "STR_COL_*"],
  "DOOR":   ["A-DOOR", "DOOR", "D_*"],
  "WINDOW": ["A-WIN", "WINDOW", "WIND_*"],
  "OPENING":["A-OPEN", "VOID"],
  "SLAB":   ["A-SLAB", "FLOOR*"],
  "GRID":   ["A-GRID", "AXIS"],
  "SPACE":  ["RM", "SPACE", "ROOM"]
}
```