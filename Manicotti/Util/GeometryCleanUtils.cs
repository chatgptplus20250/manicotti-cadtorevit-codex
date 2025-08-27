using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;

namespace Manicotti.Util
{
    public static class GeometryCleanUtils
    {
        private static readonly double _vertexTolerance = 0.005; // ~1.5mm in feet
        private static readonly double _gapTolerance = 0.01; // ~3mm in feet

        /// <summary>
        /// Takes a list of curves that should form a contiguous chain and closes any small gap
        /// between the start and end of the chain by adding a new line segment.
        /// </summary>
        public static List<Curve> CloseCurveChain(List<Curve> profile)
        {
            if (profile == null || profile.Count == 0) return profile;
            XYZ startPoint = profile.First().GetEndPoint(0);
            XYZ endPoint = profile.Last().GetEndPoint(1);
            if (!startPoint.IsAlmostEqualTo(endPoint, _vertexTolerance))
            {
                if (startPoint.DistanceTo(endPoint) < _gapTolerance)
                {
                    profile.Add(Line.CreateBound(endPoint, startPoint));
                }
            }
            return profile;
        }

        /// <summary>
        /// Extend the line to a boundary line. If the line has already surpassed it, trim the line instead.
        /// From CmdPatchBoundary.
        /// </summary>
        public static Curve ExtendLine(Curve line, Curve terminal)
        {
            Line line_unbound = line.Clone() as Line;
            Line terminal_unbound = terminal.Clone() as Line;
            line_unbound.MakeUnbound();
            terminal_unbound.MakeUnbound();
            SetComparisonResult result = line_unbound.Intersect(terminal_unbound, out IntersectionResultArray results);
            if (result == SetComparisonResult.Overlap)
            {
                XYZ sectPt = results.get_Item(0).XYZPoint;
                XYZ extensionVec = (sectPt - line.GetEndPoint(0)).Normalize();
                if (Algorithm.IsPtOnLine(sectPt, line as Line))
                {
                    double distance1 = sectPt.DistanceTo(line.GetEndPoint(0));
                    double distance2 = sectPt.DistanceTo(line.GetEndPoint(1));
                    return distance1 > distance2 ? Line.CreateBound(line.GetEndPoint(0), sectPt) : Line.CreateBound(line.GetEndPoint(1), sectPt);
                }
                else
                {
                    return extensionVec.IsAlmostEqualTo(line_unbound.Direction) ? Line.CreateBound(line.GetEndPoint(0), sectPt) : Line.CreateBound(sectPt, line.GetEndPoint(1));
                }
            }
            return null;
        }

        /// <summary>
        /// Fuse two collinear segments if they are joined or almost joined.
        /// Renamed from CloseGapAtBreakpoint in CmdPatchBoundary.
        /// </summary>
        public static List<Curve> MergeCollinearLines(List<Curve> lines)
        {
            List<List<Curve>> mergeGroups = new List<List<Curve>>();
            if (lines.Count == 0) return lines;
            mergeGroups.Add(new List<Curve>() { lines[0] });
            lines.RemoveAt(0);

            while (lines.Count != 0)
            {
                foreach (Line element in lines)
                {
                    int iterCounter = 0;
                    foreach (List<Curve> sublist in mergeGroups)
                    {
                        iterCounter += 1;
                        if (Algorithm.IsLineAlmostSubsetLines(element, sublist))
                        {
                            sublist.Add(element);
                            lines.Remove(element);
                            goto a;
                        }
                        if (iterCounter == mergeGroups.Count)
                        {
                            mergeGroups.Add(new List<Curve>() { element });
                            lines.Remove(element);
                            goto a;
                        }
                    }
                }
            a:;
            }

            List<Curve> mergeLines = new List<Curve>();
            foreach (List<Curve> mergeGroup in mergeGroups)
            {
                mergeLines.Add(mergeGroup.Count > 1 ? Algorithm.FuseLines(mergeGroup) : mergeGroup[0]);
            }
            return mergeLines;
        }

        /// <summary>
        /// Fix the gap when two lines are not met at the corner.
        /// From CmdPatchBoundary.
        /// </summary>
        public static List<Curve> CloseGapsAtCorners(List<Curve> lines)
        {
            List<Curve> linePatches = new List<Curve>();
            List<int> removeIds = new List<int>();
            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (!Algorithm.IsIntersected(lines[i], lines[j]) && Algorithm.IsAlmostJoined(lines[i], lines[j]))
                    {
                        removeIds.Add(i);
                        removeIds.Add(j);
                        linePatches.Add(ExtendLine(lines[i], lines[j]));
                        linePatches.Add(ExtendLine(lines[j], lines[i]));
                    }
                }
            }
            removeIds.Sort();
            for (int k = removeIds.Count - 1; k >= 0; k--)
            {
                lines.RemoveAt(removeIds[k]);
            }
            lines.AddRange(linePatches);
            return lines;
        }
    }
}
