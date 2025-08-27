using Manicotti.Core.Models;

namespace Manicotti.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for the CAD geometry repair engine.
    /// Its responsibility is to take raw CAD data and apply cleaning operations.
    /// </summary>
    public interface IRepairEngine
    {
        /// <summary>
        /// Cleans and repairs the raw CAD data.
        /// </summary>
        /// <param name="rawData">The raw data extracted by an ICadImporter.</param>
        /// <param name="options">The settings and tolerances for the repair process.</param>
        /// <returns>A new CadData object with the cleaned geometry.</returns>
        CadData Repair(CadData rawData, RepairOptions options);
    }
}
