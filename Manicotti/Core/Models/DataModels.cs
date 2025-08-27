using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Manicotti.Core.Models
{
    /// <summary>
    /// A container for all relevant data extracted from a source CAD file.
    /// </summary>
    public class CadData
    {
        public List<GeometryObject> Geometries { get; set; } = new List<GeometryObject>();
        public List<DBText> TextNotes { get; set; } = new List<DBText>();
        public List<string> Layers { get; set; } = new List<string>();
        public string SourceUnits { get; set; }
        public XYZ BasePoint { get; set; }
    }

    /// <summary>
    /// Defines the settings and tolerances for the geometry repair process.
    /// </summary>
    public class RepairOptions
    {
        public double GapTolerance { get; set; } = 0.01; // in feet
        public double VertexMergeTolerance { get; set; } = 0.005; // in feet
        public bool FlattenToZero { get; set; } = true;
        public bool RemoveDuplicates { get; set; } = true;
    }

    /// <summary>
    /// Defines what elements should be created during the transformation process.
    /// </summary>
    public class BuildOptions
    {
        public bool CreateWalls { get; set; } = true;
        public bool CreateColumns { get; set; } = true;
        public bool CreateBeams { get; set; } = true;
        public bool CreateFloors { get; set; } = true;
        // Feature flags would go here
        public bool EnableCurvedWalls { get; set; } = false;
    }

    /// <summary>
    /// A container for the results of the build process, used for reporting.
    /// </summary>
    public class BuildResult
    {
        public int WallsCreated { get; set; }
        public int ColumnsCreated { get; set; }
        public int BeamsCreated { get; set; }
        public int FloorsCreated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public long DurationMilliseconds { get; set; }
    }
}
