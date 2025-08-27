#region Namespaces
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Manicotti.Util;
#endregion

namespace Manicotti
{
    [Transaction(TransactionMode.Manual)]
    public class CmdCreateAll : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Pick Import Instance
            ImportInstance import = null;
            try
            {
                Reference r = uidoc.Selection.PickObject(ObjectType.Element, new Util.ElementsOfClassSelectionFilter<ImportInstance>());
                import = doc.GetElement(r) as ImportInstance;
            }
            catch
            {
                return Result.Cancelled;
            }
            if (import == null)
            {
                System.Windows.MessageBox.Show("CAD not found", "Tips");
                return Result.Cancelled;
            }


            // Initiate the progress bar
            Views.ProgressBar pb = new Views.ProgressBar("Modeling entire building", "Initiation...", 100);
            if (pb.ProcessCancelled) return Result.Cancelled;


            //////////////////////////////////
            // Check if the families are ready
            pb.CustomizeStatus("Checking families...", 2);
            if (pb.ProcessCancelled) return Result.Cancelled;
            // Consider to add more customized families (in the Setting Panel)
            if (Properties.Settings.Default.name_door == null ||
                Properties.Settings.Default.name_window == null ||
                Properties.Settings.Default.name_columnRect == null ||
                Properties.Settings.Default.name_columnRound == null)
            {
                System.Windows.MessageBox.Show("Please select the column/door/window type in settings", "Tips");
                return Result.Cancelled;
            }
            //if (!File.Exists(Properties.Settings.Default.url_door) && File.Exists(Properties.Settings.Default.url_window)
            //    && File.Exists(Properties.Settings.Default.url_column))
            //{
            //    System.Windows.MessageBox.Show("Please check the family path is solid", "Tips");
            //    return Result.Cancelled;
            //}
            //Family fColumn, fDoor, fWindow = null;
            //using (Transaction tx = new Transaction(doc, "Load necessary families"))
            //{
            //    tx.Start();
            //    if (!doc.LoadFamily(Properties.Settings.Default.url_column, out fColumn))
            //    {
            //        System.Windows.MessageBox.Show("Please check the column family path is solid", "Tips");
            //        return Result.Cancelled;
            //    }
            //    if (!doc.LoadFamily(Properties.Settings.Default.url_door, out fDoor) ||
            //        !doc.LoadFamily(Properties.Settings.Default.url_window, out fWindow))
            //    {
            //        System.Windows.MessageBox.Show("Please check the door/window family path is solid", "Tips");
            //        return Result.Cancelled;
            //    }
            //    tx.Commit();
            //}

            // Prepare a family for ViewPlan creation
            // It may be a coincidence that the 1st ViewFamilyType is for the FloorPlan
            // Uplift needed here (doomed if it happends to be a CeilingPlan)
            ViewFamilyType viewType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).First() as ViewFamilyType;

            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType)).FirstOrDefault<Element>() as RoofType;

            FloorType floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType)).FirstOrDefault<Element>() as FloorType;


            ////////////////////
            // DATA PREPARATIONS
            pb.CustomizeStatus("Processing geometries...", 3);
            if (pb.ProcessCancelled) return Result.Cancelled;
            // Prepare frames and levels
            // Cluster all geometry elements and texts into datatrees
            // Two procedures are intertwined
            List<GeometryObject> dwg_geos = Util.TeighaGeometry.ExtractElement(uidoc, import);

            // NEW: Extract frames from all geos using the layer map
            List<GeometryObject> dwg_frames = new List<GeometryObject>();
            if (dwg_geos != null)
            {
                foreach (GeometryObject geo in dwg_geos)
                {
                    // A PolyLine is required to be a frame
                    if (geo is PolyLine)
                    {
                        var gStyle = doc.GetElement(geo.GraphicsStyleId) as GraphicsStyle;
                        if (gStyle != null && layerMap.FRAME.Contains(gStyle.GraphicsStyleCategory.Name.ToUpperInvariant()))
                        {
                            dwg_frames.Add(geo);
                        }
                    }
                }
            }
            
            // Terminate if no geometry has been found
            if (dwg_geos == null)
            {
                System.Windows.MessageBox.Show("A drawing frame is mandantory", "Tips");
                return Result.Failed;
            }

            List<PolyLine> closedPolys = new List<PolyLine>();
            List<PolyLine> parentPolys = new List<PolyLine>();

            Debug.Print("Number of geos acting as the framework is " + dwg_frames.Count().ToString());
            
            if (dwg_frames.Count > 0)
            {
                foreach (var obj in dwg_frames)
                {
                    PolyLine poly = obj as PolyLine;
                    // Draw shattered lines in case the object is a PolyLine
                    if (null != poly)
                    {
                        var vertices = poly.GetCoordinates();
                        if (vertices[0].IsAlmostEqualTo(vertices.Last()))
                        {
                            closedPolys.Add(poly);
                        }
                    }
                }

                for (int i = 0; i < closedPolys.Count(); i++)
                {
                    int judgement = 0;
                    int counter = 0;
                    for (int j = 0; j < closedPolys.Count(); j++)
                    {
                        if (i != j)
                        {
                            if (!RegionDetect.PolyInPoly(closedPolys[i], RegionDetect.PolyLineToCurveArray(closedPolys[j], tolerance)))
                            {
                                //Debug.Print("Poly inside poly detected");
                                judgement += 1;
                            }
                            counter += 1;
                        }
                    }
                    if (judgement == counter)
                    {
                        parentPolys.Add(closedPolys[i]);
                    }
                }
            }
            else
            {
                Debug.Print("There is no returning of geometries");
            }
            Debug.Print("Got closedPolys: " + closedPolys.Count().ToString());
            Debug.Print("Got parentPolys: " + parentPolys.Count().ToString());


            string path = Util.TeighaText.GetCADPath(uidoc, import);
            Debug.Print("The path of linked CAD file is: " + path);
            List<Util.TeighaText.CADTextModel> texts = Util.TeighaText.GetCADText(path);


            int level;
            int levelCounter = 0;
            double floorHeight = Misc.MmToFoot(Properties.Settings.Default.floorHeight);
            Dictionary<int, PolyLine> frameDict= new Dictionary<int, PolyLine>(); // cache drawing borders
            Dictionary<int, XYZ> transDict= new Dictionary<int, XYZ>();
            // cache transform vector from the left-bottom corner of the drawing border to the Origin
            Dictionary<int, List<GeometryObject>> geoDict = new Dictionary<int, List<GeometryObject>>();
            // cache geometries of each floorplan
            Dictionary<int, List<Util.TeighaText.CADTextModel>> textDict = new Dictionary<int, List<Util.TeighaText.CADTextModel>>();
            // cache text info of each floorplan

            if (texts.Count > 0)
            {
                foreach (var textmodel in texts)
                {
                    level = Misc.GetLevel(textmodel.Text, "平面图");
                    Debug.Print("Got target label " + textmodel.Text);
                    if (level != -1)
                    {
                        foreach (PolyLine frame in parentPolys)
                        {
                            if (RegionDetect.PointInPoly(RegionDetect.PolyLineToCurveArray(frame, tolerance), textmodel.Location))
                            {
                                XYZ basePt = Algorithm.BubbleSort(frame.GetCoordinates().ToList())[0];
                                XYZ transVec = XYZ.Zero - basePt;
                                Debug.Print("Add level " + level.ToString() + " with transaction (" +
                                    transVec.X.ToString() + ", " + transVec.Y.ToString() + ", " + transVec.Z.ToString() + ")");
                                if (!frameDict.Values.ToList().Contains(frame))
                                {
                                    frameDict.Add(level, frame);
                                    transDict.Add(level, transVec);
                                    levelCounter += 1;
                                }
                            }
                        }
                    }
                }
                // Too complicated using 2 iterations... uplift needed
                for (int i = 1; i <= levelCounter; i++)
                {
                    textDict.Add(i, new List<Util.TeighaText.CADTextModel>());
                    geoDict.Add(i, new List<GeometryObject>());
                    CurveArray tempPolyArray = RegionDetect.PolyLineToCurveArray(frameDict[i], tolerance);
                    foreach (var textmodel in texts)
                    {
                        if (RegionDetect.PointInPoly(tempPolyArray, textmodel.Location))
                        {
                            textDict[i].Add(textmodel);
                        }
                    }
                    foreach (GeometryObject go in dwg_geos)
                    {
                        XYZ centPt = XYZ.Zero;
                        if (go is Line)
                        {
                            //get the revit model coordinates.
                            Line go_line = go as Line;
                            centPt = (go_line.GetEndPoint(0) + go_line.GetEndPoint(1)).Divide(2);
                        }
                        else if (go is Arc)
                        {
                            Arc go_arc = go as Arc;
                            centPt = go_arc.Center;
                        }
                        else if (go is PolyLine)
                        {
                            PolyLine go_poly = go as PolyLine;
                            centPt = go_poly.GetCoordinate(0);
                        }
                        // Assignment
                        if (RegionDetect.PointInPoly(tempPolyArray, centPt) && centPt != XYZ.Zero)
                        {
                            geoDict[i].Add(go);
                        }
                    }
                }
            }
            Debug.Print("All levels: " + levelCounter.ToString());
            Debug.Print("frameDict: " + frameDict.Count().ToString());
            Debug.Print("transDict: " + transDict.Count().ToString());
            for (int i = 1; i <= levelCounter; i++)
            {
                Debug.Print("geoDict-{0}: {1}", i, geoDict[i].Count().ToString());
            }
            Debug.Print("textDict: " + textDict.Count().ToString() + " " + textDict[1].Count().ToString());


            ////////////////////
            // MAIN TRANSACTIONS

            // Prepare a family and configurations for TextNote (Put it inside transactions)
            /*
            TextNoteType tnt = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType)).First() as TextNoteType;
            TextNoteOptions options = new TextNoteOptions();
            options.HorizontalAlignment = HorizontalTextAlignment.Center;
            options.TypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            BuiltInParameter paraIndex = BuiltInParameter.TEXT_SIZE;
            Parameter textSize = tnt.get_Parameter(paraIndex);
            textSize.Set(0.3); // in feet

            XYZ centPt = RegionDetect.PolyCentPt(frameDict[i]);
            TextNoteOptions opts = new TextNoteOptions(tnt.Id);
            
            // The note may only show in the current view
            // no matter we still need it anyway
            TextNote txNote = TextNote.Create(doc, active_view.Id, centPt, floorView.Name, options);
            txNote.ChangeTypeId(tnt.Id);

            // Draw model lines of frames as notation
            Plane Geomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero + transDict[i] + XYZ.BasisZ * (i - 1) * floorHeight);
            SketchPlane sketch = SketchPlane.Create(doc, Geomplane);
            CurveArray shatters = RegionDetect.PolyLineToCurveArray(frameDict[i], tolerance);
            Transform alignModelLine = Transform.CreateTranslation(transDict[i] + XYZ.BasisZ * (i - 1) * floorHeight);
            foreach (Curve shatter in shatters)
            {
                Curve alignedCrv = shatter.CreateTransformed(alignModelLine);
                ModelCurve modelline = doc.Create.NewModelCurve(alignedCrv, sketch) as ModelCurve;
            }
            */


            // ITERATION FLAG
            for (int i = 1; i <= levelCounter; i++)
            {
                pb.CustomizeStatus("On Floor " + i.ToString() + "... with lines", 5);
                if (pb.ProcessCancelled) return Result.Cancelled;

                TransactionGroup tg = new TransactionGroup(doc, "Generate on floor-" + i.ToString());
                {
                    try
                    {
                        tg.Start();

                        // MILESTONE
                        // Align the labels to the origin
                        Transform alignment = Transform.CreateTranslation(transDict[i]);
                        foreach (Util.TeighaText.CADTextModel label in textDict[i])
                        {
                            label.Location = label.Location + transDict[i];
                        }

                        // MILESTONE
                        // Sort out lines
                        List<Curve> wallCrvs = new List<Curve>();
                        List<Curve> columnCrvs = new List<Curve>();
                        List<Curve> doorCrvs = new List<Curve>();
                        List<Curve> windowCrvs = new List<Curve>();
                        foreach (GeometryObject go in geoDict[i])
                        {
                            var gStyle = doc.GetElement(go.GraphicsStyleId) as GraphicsStyle;
                            if (gStyle == null) continue;
                            string layerNameUpper = gStyle.GraphicsStyleCategory.Name.ToUpperInvariant();

                            if (layerMap.WALL.Contains(layerNameUpper))
                            {
                                if (go.GetType().Name == "Line")
                                {
                                    Curve wallLine = go as Curve;
                                    wallCrvs.Add(wallLine.CreateTransformed(alignment) as Line);
                                }
                                if (go.GetType().Name == "PolyLine")
                                {
                                    CurveArray wallPolyLine_shattered = RegionDetect.PolyLineToCurveArray(go as PolyLine, tolerance);
                                    foreach (Curve crv in wallPolyLine_shattered)
                                    {
                                        wallCrvs.Add(crv.CreateTransformed(alignment) as Line);
                                    }
                                }
                            }
                            if (layerMap.COLUMN.Contains(layerNameUpper))
                            {
                                if (go.GetType().Name == "Line")
                                {
                                    Curve columnLine = go as Curve;
                                    columnCrvs.Add(columnLine.CreateTransformed(alignment));
                                }
                                if (go.GetType().Name == "PolyLine")
                                {
                                    CurveArray columnPolyLine_shattered = RegionDetect.PolyLineToCurveArray(go as PolyLine, tolerance);
                                    foreach (Curve crv in columnPolyLine_shattered)
                                    {
                                        columnCrvs.Add(crv.CreateTransformed(alignment));
                                    }
                                }
                            }
                            if (layerMap.DOOR.Contains(layerNameUpper))
                            {
                                Curve doorCrv = go as Curve;
                                PolyLine poly = go as PolyLine;
                                if (null != doorCrv)
                                {
                                    doorCrvs.Add(doorCrv.CreateTransformed(alignment));
                                }
                                if (null != poly)
                                {
                                    CurveArray columnPolyLine_shattered = RegionDetect.PolyLineToCurveArray(poly, tolerance);
                                    foreach (Curve crv in columnPolyLine_shattered)
                                    {
                                        doorCrvs.Add(crv.CreateTransformed(alignment) as Line);
                                    }
                                }
                            }
                            if (layerMap.WINDOW.Contains(layerNameUpper))
                            {
                                Curve windowCrv = go as Curve;
                                PolyLine poly = go as PolyLine;
                                if (null != windowCrv)
                                {
                                    windowCrvs.Add(windowCrv.CreateTransformed(alignment));
                                }
                                if (null != poly)
                                {
                                    CurveArray columnPolyLine_shattered = RegionDetect.PolyLineToCurveArray(poly, tolerance);
                                    foreach (Curve crv in columnPolyLine_shattered)
                                    {
                                        windowCrvs.Add(crv.CreateTransformed(alignment) as Line);
                                    }
                                }
                            }
                        }

                        // MILESTONE
                        // Create additional levels (ignore what's present)
                        // Consider to use sub-transaction here
                        using (var t_level = new Transaction(doc))
                        {
                            t_level.Start("Create levels");
                            Level floor = Level.Create(doc, (i - 1) * floorHeight);
                            ViewPlan floorView = ViewPlan.Create(doc, viewType.Id, floor.Id);
                            floorView.Name = "F-" + i.ToString();
                            t_level.Commit();
                        }

                        // Grab the current building level
                        FilteredElementCollector colLevels = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .OfCategory(BuiltInCategory.INVALID)
                            .OfClass(typeof(Level));
                        Level currentLevel = colLevels.LastOrDefault() as Level;
                        // The newly created level will append to the list,
                        // but this is not a safe choice

                        // Sub-transactions are packed within these functions.
                        // The family names should be defined by the user in WPF
                        // MILESTONE
                        pb.CustomizeStatus("On Floor " + i.ToString() + "... with Walls", 90 / levelCounter / 4);
                        if (pb.ProcessCancelled) return Result.Cancelled;

                        // Refactored Wall Creation Logic
                        List<Wall> createdWalls = new List<Wall>();
                        using (var t_wall = new Transaction(doc, "Create Walls"))
                        {
                            t_wall.Start();
                            // Logic from CreateWall.cs
                            List<Curve> axes = new List<Curve>();
                            double bias = Misc.MmToFoot(20);
                            var doubleLines = Misc.CrvsToLines(wallCrvs);
                            for (int k = 0; k < doubleLines.Count; k++)
                            {
                                for (int j = 0; j < doubleLines.Count - k; j++)
                                {
                                    if (Algorithm.IsParallel(doubleLines[k], doubleLines[k + j])
                                        && !Algorithm.IsIntersected(doubleLines[k], doubleLines[k + j]))
                                    {
                                        if (Algorithm.LineSpacing(doubleLines[k], doubleLines[k + j]) < Misc.MmToFoot(200) + bias
                                        && Algorithm.LineSpacing(doubleLines[k], doubleLines[k + j]) > Misc.MmToFoot(200) - bias
                                        && Algorithm.IsShadowing(doubleLines[k], doubleLines[k + j]))
                                        {
                                            if (Algorithm.GenerateAxis(doubleLines[k], doubleLines[k + j]) != null)
                                            {
                                                axes.Add(Algorithm.GenerateAxis(doubleLines[k], doubleLines[k + j]));
                                            }
                                        }
                                    }
                                }
                            }
                            List<Curve> mergedAxes = Algorithm.MergeAxes(axes);
                            foreach (Curve axis in mergedAxes)
                            {
                                Wall newWall = Wall.Create(doc, axis, currentLevel.Id, true);
                                createdWalls.Add(newWall);
                            }
                            t_wall.Commit();
                        }

                        // MILESTONE
                        pb.CustomizeStatus("On Floor " + i.ToString() + "... with Columns", 90 / levelCounter / 4);
                        if (pb.ProcessCancelled) return Result.Cancelled;

                        // Refactored Column Creation Logic from CreateColumn.cs
                        using (var t_col = new Transaction(doc, "Create Columns"))
                        {
                            t_col.Start();

                            List<List<Curve>> columnRect = new List<List<Curve>>();
                            List<Arc> columnRound = new List<Arc>();
                            List<List<Curve>> columnSpecialShaped = new List<List<Curve>>();
                            List<Curve> sortedLines = new List<Curve>();
                            foreach (Curve columnLine in columnCrvs)
                            {
                                if (columnLine is Arc) { columnRound.Add(columnLine as Arc); }
                                else { sortedLines.Add(columnLine); }
                            }
                            List<List<Curve>> columnGroups = Algorithm.ClusterByIntersect(sortedLines);
                            foreach (List<Curve> columnGroup in columnGroups)
                            {
                                if (Algorithm.GetPtsOfCrvs(columnGroup).Count == columnGroup.Count)
                                {
                                    if (Algorithm.IsRectangle(columnGroup)) { columnRect.Add(columnGroup); }
                                    else { columnSpecialShaped.Add(columnGroup); }
                                }
                            }

                            // Rectangular columns
                            foreach (List<Curve> baselines in columnRect)
                            {
                                double width = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item1;
                                double depth = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item2;
                                double angle = Algorithm.GetSizeOfRectangle(Misc.CrvsToLines(baselines)).Item3;
                                FamilySymbol fs = NewRectColumnType(doc, Properties.Settings.Default.name_columnRect, width, depth);
                                if (fs != null && !fs.IsActive) { fs.Activate(); }
                                XYZ columnCenterPt = Algorithm.GetCenterPt(baselines);
                                Line columnCenterAxis = Line.CreateBound(columnCenterPt, columnCenterPt.Add(-XYZ.BasisZ));
                                FamilyInstance fi = doc.Create.NewFamilyInstance(columnCenterPt, fs, currentLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                                ElementTransformUtils.RotateElement(doc, fi.Id, columnCenterAxis, angle);
                            }

                            // Round columns
                            foreach (Arc baseline in columnRound)
                            {
                                XYZ basePt = baseline.Center;
                                double diameter = Misc.FootToMm(Math.Round(2 * baseline.Radius, 2));
                                FamilySymbol fs = NewRoundColumnType(doc, Properties.Settings.Default.name_columnRound, diameter);
                                if (fs != null && !fs.IsActive) { fs.Activate(); }
                                FamilyInstance fi = doc.Create.NewFamilyInstance(basePt, fs, currentLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            }

                            // Special shaped columns (Requires a separate transaction for each due to family loading)
                            // This part will be handled outside the main transaction loop for columns.

                            t_col.Commit();
                        }

                        // Special shaped columns must be handled one-by-one because they involve creating and loading a new family.
                        foreach (List<Curve> baselines in columnSpecialShaped)
                        {
                            using (var t_special_col = new Transaction(doc, "Create Special Shaped Column"))
                            {
                                t_special_col.Start();
                                var boundary = Algorithm.RectifyPolygon(Misc.CrvsToLines(baselines));
                                FamilySymbol fs = NewSpecialShapedColumnType(app, doc, boundary);
                                if (fs != null)
                                {
                                    if (!fs.IsActive) { fs.Activate(); }
                                    // BUG FIX: Calculate the center point for correct placement
                                    XYZ centerPt = Algorithm.GetCenterPt(baselines);
                                    FamilyInstance fi = doc.Create.NewFamilyInstance(centerPt, fs, currentLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                                }
                                t_special_col.Commit();
                            }
                        }

                        // MILESTONE
                        pb.CustomizeStatus("On Floor " + i.ToString() + "... with Openings", 90 / levelCounter / 4);
                        if (pb.ProcessCancelled) return Result.Cancelled;

                        // Refactored Opening Creation Logic
                        using (var t_open = new Transaction(doc, "Create Openings"))
                        {
                            t_open.Start();

                            var doorClusters = Algorithm.ClusterByIntersect(doorCrvs);
                            List<List<Curve>> doorBlocks = new List<List<Curve>>();
                            foreach (List<Curve> cluster in doorClusters)
                            {
                                if (null != Algorithm.CreateBoundingBox2D(cluster)) { doorBlocks.Add(Algorithm.CreateBoundingBox2D(cluster)); }
                            }
                            List<Curve> doorAxes = new List<Curve>();
                            foreach (List<Curve> doorBlock in doorBlocks)
                            {
                                for (int k = 0; k < doorBlock.Count; k++)
                                {
                                    int sectCount = 0;
                                    List<Curve> fenses = new List<Curve>();
                                    foreach (Curve line in wallCrvs)
                                    {
                                        Curve testCrv = doorBlock[k].Clone();
                                        if (RegionDetect.ExtendCrv(testCrv, 0.01).Intersect(line, out IntersectionResultArray results) == SetComparisonResult.Overlap)
                                        {
                                            sectCount += 1;
                                            fenses.Add(line);
                                        }
                                    }
                                    if (sectCount == 2)
                                    {
                                        XYZ projecting = fenses[0].Evaluate(0.5, true);
                                        XYZ projected = fenses[1].Project(projecting).XYZPoint;
                                        if (fenses[0].Length > fenses[1].Length)
                                        {
                                            projecting = fenses[1].Evaluate(0.5, true);
                                            projected = fenses[0].Project(projecting).XYZPoint;
                                        }
                                        doorAxes.Add(Line.CreateBound(projecting, projected));
                                    }
                                }
                            }

                            var windowClusters = Algorithm.ClusterByIntersect(windowCrvs);
                            List<List<Curve>> windowBlocks = new List<List<Curve>>();
                            foreach (List<Curve> cluster in windowClusters)
                            {
                                if (null != Algorithm.CreateBoundingBox2D(cluster)) { windowBlocks.Add(Algorithm.CreateBoundingBox2D(cluster)); }
                            }
                            List<Curve> windowAxes = new List<Curve>();
                            foreach (List<Curve> windowBlock in windowBlocks)
                            {
                                Line axis1 = Line.CreateBound((windowBlock[0].GetEndPoint(0) + windowBlock[0].GetEndPoint(1)).Divide(2), (windowBlock[2].GetEndPoint(0) + windowBlock[2].GetEndPoint(1)).Divide(2));
                                Line axis2 = Line.CreateBound((windowBlock[1].GetEndPoint(0) + windowBlock[1].GetEndPoint(1)).Divide(2), (windowBlock[3].GetEndPoint(0) + windowBlock[3].GetEndPoint(1)).Divide(2));
                                windowAxes.Add(axis1.Length > axis2.Length ? axis1 : axis2);
                            }

                            // Create door instances
                            foreach (Curve doorAxis in doorAxes)
                            {
                                XYZ basePt = (doorAxis.GetEndPoint(0) + doorAxis.GetEndPoint(1)).Divide(2);
                                Wall hostWall = FindHostWall(basePt, createdWalls);
                                if (hostWall == null) continue;

                                double width = Math.Round(Misc.FootToMm(doorAxis.Length), 0);
                                double height = 2000; // Default height
                                XYZ insertPt = basePt + XYZ.BasisZ * currentLevel.Elevation;

                                FamilySymbol fs = NewOpeningType(doc, Properties.Settings.Default.name_door, width, height, "Door");
                                if (fs == null) continue;
                                if (!fs.IsActive) fs.Activate();

                                doc.Create.NewFamilyInstance(insertPt, fs, hostWall, currentLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            }

                            // Create window instances
                            foreach (Curve windowAxis in windowAxes)
                            {
                                XYZ basePt = (windowAxis.GetEndPoint(0) + windowAxis.GetEndPoint(1)).Divide(2);
                                Wall hostWall = FindHostWall(basePt, createdWalls);
                                if (hostWall == null) continue;

                                double width = Math.Round(Misc.FootToMm(windowAxis.Length), 0);
                                double height = 1500; // Default height
                                XYZ insertPt = basePt + XYZ.BasisZ * (Misc.MmToFoot(Properties.Settings.Default.sillHeight) + currentLevel.Elevation);

                                FamilySymbol fs = NewOpeningType(doc, Properties.Settings.Default.name_window, width, height, "Window");
                                if (fs == null) continue;
                                if (!fs.IsActive) fs.Activate();

                                FamilyInstance fi = doc.Create.NewFamilyInstance(insertPt, fs, hostWall, currentLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                            }

                            t_open.Commit();
                        }

                        // Create floor
                        // MILESTONE
                        var footprint = CreateRegion.Execute(doc, wallCrvs, columnCrvs, windowCrvs, doorCrvs);
                        using (var t_floor = new Transaction(doc))
                        {
                            t_floor.Start("Generate Floor");

                            Floor newFloor = doc.Create.NewFloor(footprint, floorType, currentLevel, false, XYZ.BasisZ);
                            newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(0);

                            t_floor.Commit();
                        }

                        // Generate rooms after the topology is established
                        pb.CustomizeStatus("On Floor " + i.ToString() + "... with Rooms", 90 / levelCounter / 4);
                        if (pb.ProcessCancelled) return Result.Cancelled;
                        // MILESTONE
                        using (var t_space = new Transaction(doc))
                        {
                            t_space.Start("Create rooms");

                            doc.Regenerate();

                            PlanTopology planTopology = doc.get_PlanTopology(currentLevel);
                            if (doc.ActiveView.ViewType == ViewType.FloorPlan)
                            {
                                foreach (PlanCircuit circuit in planTopology.Circuits)
                                {
                                    if (null != circuit && !circuit.IsRoomLocated)
                                    {
                                        Room room = doc.Create.NewRoom(null, circuit);
                                        room.LimitOffset = floorHeight;
                                        room.BaseOffset = 0;
                                        string roomName = "";
                                        foreach (Util.TeighaText.CADTextModel label in textDict[i])
                                        {
                                            if (layerMap.SPACE.Contains(label.Layer.ToUpperInvariant()))
                                            {
                                                if (room.IsPointInRoom(label.Location + XYZ.BasisZ * (i - 1) * floorHeight))
                                                {
                                                    roomName += label.Text;
                                                }
                                            }
                                        }
                                        if (roomName != "") { room.Name = roomName; }
                                    }
                                }
                            }
                            t_space.Commit();
                        }
                        


                        // Create roof when iterating to the last level
                        // MILESTONE
                        if (i == levelCounter)
                        {
                            using (var t_roof = new Transaction(doc))
                            {
                                t_roof.Start("Create roof");
                                Level roofLevel = Level.Create(doc, i * floorHeight);
                                ViewPlan floorView = ViewPlan.Create(doc, viewType.Id, roofLevel.Id);
                                floorView.Name = "Roof";

                                ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                                FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, roofLevel, roofType,
                                    out footPrintToModelCurveMapping);

                                //ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                                /*
                                iterator.Reset();
                                while (iterator.MoveNext())
                                {
                                    ModelCurve modelCurve = iterator.Current as ModelCurve;
                                    footprintRoof.set_DefinesSlope(modelCurve, true);
                                    footprintRoof.set_SlopeAngle(modelCurve, 0.5);
                                }
                                */
                                t_roof.Commit();
                            }
                        }
                        tg.Assimilate();
                    }
                    catch
                    {
                        System.Windows.MessageBox.Show("Error", "Tips");
                        tg.RollBack();
                    }
                }
                pb.JobCompleted();
            }

            return Result.Succeeded;
        }

        #region Element Creation Helpers
        // Helper methods moved from CreateColumn.cs and CreateOpening.cs

        private Wall FindHostWall(XYZ openingCenter, List<Wall> walls)
        {
            Wall closestWall = null;
            double minDistance = double.MaxValue;
            foreach (Wall wall in walls)
            {
                double distance = (wall.Location as LocationCurve).Curve.Distance(openingCenter);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestWall = wall;
                }
            }
            // If the closest wall is more than 5ft away, it's probably not the host.
            if (minDistance > 5.0)
            {
                return null;
            }
            return closestWall;
        }

        private static FamilySymbol NewOpeningType(Document doc, string familyName, double width, double height, string type = "")
        {
            string defaultPath = "";
            if (type == "Door") defaultPath = Properties.Settings.Default.url_door;
            if (type == "Window") defaultPath = Properties.Settings.Default.url_window;

            Family f = FamilyLoaderUtils.GetAndLoadFamily(doc, familyName, defaultPath);
            if (f == null)
            {
                TaskDialog.Show("Family Load Error", $"The required family '{familyName}' could not be loaded.");
                return null;
            }

            // Logic to find or create the specific type (FamilySymbol)
            string typeName = $"{width} x {height}mm";
            FamilySymbol s = f.GetFamilySymbolIds().Select(id => doc.GetElement(id) as FamilySymbol).FirstOrDefault(fs => fs.Name == typeName);
            if (s != null) return s;

            // If type does not exist, duplicate an existing one and modify it
            var symbolToDuplicate = doc.GetElement(f.GetFamilySymbolIds().First()) as FamilySymbol;
            using (var tx = new Transaction(doc, $"Create Type: {typeName}"))
            {
                tx.Start();
                s = symbolToDuplicate.Duplicate(typeName) as FamilySymbol;
                s.LookupParameter("Width").Set(Misc.MmToFoot(width));
                s.LookupParameter("Height").Set(Misc.MmToFoot(height));
                tx.Commit();
            }
            return s;
        }

        private static FamilySymbol NewRectColumnType(Document doc, string familyName, double width, double depth)
        {
            Family f = FamilyLoaderUtils.GetAndLoadFamily(doc, familyName, Properties.Settings.Default.url_columnRect);
            if (f == null)
            {
                TaskDialog.Show("Family Load Error", $"The required family '{familyName}' could not be loaded.");
                return null;
            }

            string typeName = $"{width} x {depth}mm";
            FamilySymbol s = f.GetFamilySymbolIds().Select(id => doc.GetElement(id) as FamilySymbol).FirstOrDefault(fs => fs.Name == typeName);
            if (s != null) return s;

            var symbolToDuplicate = doc.GetElement(f.GetFamilySymbolIds().First()) as FamilySymbol;
            using (var tx = new Transaction(doc, $"Create Type: {typeName}"))
            {
                tx.Start();
                s = symbolToDuplicate.Duplicate(typeName) as FamilySymbol;
                s.LookupParameter("Width").Set(Misc.MmToFoot(width));
                s.LookupParameter("Depth").Set(Misc.MmToFoot(depth));
                tx.Commit();
            }
            return s;
        }

        private static FamilySymbol NewRoundColumnType(Document doc, string familyName, double diameter)
        {
            Family f = FamilyLoaderUtils.GetAndLoadFamily(doc, familyName, Properties.Settings.Default.url_columnRound);
            if (f == null)
            {
                TaskDialog.Show("Family Load Error", $"The required family '{familyName}' could not be loaded.");
                return null;
            }

            string typeName = $"{diameter}mm Diameter";
            FamilySymbol s = f.GetFamilySymbolIds().Select(id => doc.GetElement(id) as FamilySymbol).FirstOrDefault(fs => fs.Name == typeName);
            if (s != null) return s;

            var symbolToDuplicate = doc.GetElement(f.GetFamilySymbolIds().First()) as FamilySymbol;
            using (var tx = new Transaction(doc, $"Create Type: {typeName}"))
            {
                tx.Start();
                s = symbolToDuplicate.Duplicate(typeName) as FamilySymbol;
                s.LookupParameter("Diameter").Set(Misc.MmToFoot(diameter));
                tx.Commit();
            }
            return s;
        }

        private static FamilySymbol NewSpecialShapedColumnType(Application app, Document doc, CurveArray boundary)
        {
            // Convert CurveArray to List<Curve> for cleaning
            List<Curve> boundaryCurves = new List<Curve>();
            foreach (Curve c in boundary) { boundaryCurves.Add(c); }

            // Use the new utility to ensure the boundary is closed
            boundaryCurves = GeometryCleanUtils.CloseCurveChain(boundaryCurves);

            // Convert back to CurveArray for the Revit API
            CurveArray closedBoundary = new CurveArray();
            foreach (Curve c in boundaryCurves) { closedBoundary.Append(c); }

            Document familyDoc = app.NewFamilyDocument(Properties.Settings.Default.url_columnFamily);
            using (Transaction tx_createFamily = new Transaction(familyDoc, "Create family"))
            {
                tx_createFamily.Start();
                Plane familyGeomplane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                SketchPlane sketch = SketchPlane.Create(familyDoc, familyGeomplane);
                CurveArrArray curveArrArray = new CurveArrArray();
                curveArrArray.Append(closedBoundary);
                Extrusion extrusion = familyDoc.FamilyCreate.NewExtrusion(true, curveArrArray, sketch, Misc.MmToFoot(4000));
                familyDoc.FamilyManager.NewType("Type 0");
                familyDoc.Regenerate();

                Reference topFaceRef = null;
                Options opt = new Options();
                opt.ComputeReferences = true;
                opt.DetailLevel = ViewDetailLevel.Fine;
                GeometryElement gelm = extrusion.get_Geometry(opt);
                foreach (GeometryObject gobj in gelm)
                {
                    if (gobj is Solid)
                    {
                        Solid solid = gobj as Solid;
                        foreach (Face face in solid.Faces)
                        {
                            if (face.ComputeNormal(UV.Zero).IsAlmostEqualTo(XYZ.BasisZ))
                            {
                                topFaceRef = face.Reference;
                            }
                        }
                    }
                }
                View v = GetView(familyDoc);
                Reference r = GetUpperRefLevel(familyDoc);
                Dimension d = familyDoc.FamilyCreate.NewAlignment(v, r, topFaceRef);
                d.IsLocked = true;
                tx_createFamily.Commit();
            }

            Family f = familyDoc.LoadFamily(doc) as Family;
            FamilySymbol s = null;
            foreach (ElementId id in f.GetFamilySymbolIds())
            {
                s = doc.GetElement(id) as FamilySymbol;
            }
            familyDoc.Close(false);
            return s;
        }

        private static View GetView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            View v = collector.OfClass(typeof(View)).First(m => m.Name == "Front") as View;
            return v;
        }

        private static Reference GetUpperRefLevel(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Level lvl = collector.OfClass(typeof(Level)).First(m => m.Name == "Upper Ref Level") as Level;
            return new Reference(lvl);
        }

        #endregion
    }
}