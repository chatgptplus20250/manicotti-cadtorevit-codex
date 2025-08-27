using Autodesk.Revit.DB;
using Manicotti.Core.Models;

namespace Manicotti.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for the transformation engine.
    /// Its responsibility is to convert cleaned CAD data into native Revit elements.
    /// </summary>
    public interface ITransformEngine
    {
        /// <summary>
        /// Builds Revit elements based on the provided CAD data.
        /// </summary>
        /// <param name="cleanedData">The cleaned CAD data from the repair engine.</param>
        /// <param name="options">The options specifying which elements to build.</param>
        /// <param name="doc">The Revit document to build in.</param>
        /// <returns>A BuildResult object summarizing the outcome.</returns>
        BuildResult Transform(CadData cleanedData, BuildOptions options, Document doc);
    }
}
