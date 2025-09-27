using System.ComponentModel.DataAnnotations;

namespace PowerTrading.Reporting.Service.Options;

/// <summary>
/// Configuration options for PeriodicTrigger
/// </summary>
public sealed class CsvExporterOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "CsvExporter";

    /// <summary>
    /// Interval in minutes between report triggers. Must be greater than zero.
    /// This value is required and must be explicitly configured.
    /// </summary>
    [Required(ErrorMessage = "OutputDirectory is required and must be configured in the CsvExporter section")]
    public string OutputDirectory { get; init; } = ".";
}