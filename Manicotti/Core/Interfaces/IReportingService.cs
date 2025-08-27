using Manicotti.Core.Models;

namespace Manicotti.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that generates reports.
    /// </summary>
    public interface IReportingService
    {
        /// <summary>
        /// Generates a human-readable report in JSON format.
        /// </summary>
        /// <param name="result">The build result to report on.</param>
        /// <returns>A JSON formatted string.</returns>
        string GenerateJsonReport(BuildResult result);

        /// <summary>
        /// Generates a tabular report in CSV format.
        /// </summary>
        /// <param name="result">The build result to report on.</param>
        /// <returns>A CSV formatted string.</returns>
        string GenerateCsvReport(BuildResult result);
    }
}
