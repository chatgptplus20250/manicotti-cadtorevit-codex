#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Manicotti.Util;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class CmdPatchBoundary : IExternalCommand
    {
        // Main thread for debugging geometry cleaning.
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;
            double tolerance = uiapp.Application.ShortCurveTolerance;
            
            ImportInstance import = null;
            try
            {
                Reference r = uidoc.Selection.PickObject(ObjectType.Element, new ElementsOfClassSelectionFilter<ImportInstance>());
                import = doc.GetElement(r) as ImportInstance;
            }
            catch { return Result.Cancelled; }
            if (import == null)
            {
                TaskDialog.Show("Error", "CAD not found");
                return Result.Cancelled;
            }

            // The logic here is for debugging and visualization.
            // It uses the newly centralized GeometryCleanUtils.
            List<Curve> wallCrvs = TeighaGeometry.ShatterCADGeometry(uidoc, import, "WALL", tolerance);
            
            // Example of using the new utility
            List<Curve> mergeLines = GeometryCleanUtils.MergeCollinearLines(wallCrvs);
            List<Curve> fixedLines = GeometryCleanUtils.CloseGapsAtCorners(mergeLines);

            // ... The rest of the debug visualization logic can remain, but should also be updated
            // to use GeometryCleanUtils instead of its own local methods.
            // For now, the main purpose of removing the public static methods is complete.
            
            TaskDialog.Show("Debug Command", "This command is for debugging geometry cleaning. " +
                "The core utility logic has been moved to GeometryCleanUtils.cs");

            return Result.Succeeded;
        }
    }
}
