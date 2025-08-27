using Manicotti.Core.Models;

namespace Manicotti.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a CAD file importer.
    /// Its responsibility is to read a source file and extract the raw data.
    /// </summary>
    public interface ICadImporter
    {
        /// <summary>
        /// Imports the specified CAD file.
        /// </summary>
        /// <param name="filePath">The full path to the CAD file (DWG, DXF, etc.).</param>
        /// <returns>A CadData object containing the raw extracted information.</returns>
        CadData Import(string filePath);
    }
}
