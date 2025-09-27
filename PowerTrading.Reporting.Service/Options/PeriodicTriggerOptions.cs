using System.ComponentModel.DataAnnotations;

namespace PowerTrading.Reporting.Service.Options;

/// <summary>
/// Configuration options for PeriodicTrigger
/// </summary>
public sealed class PeriodicTriggerOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "PeriodicTrigger";

    /// <summary>
    /// Interval in minutes between report triggers. Must be greater than zero.
    /// This value is required and must be explicitly configured.
    /// </summary>
    [Required(ErrorMessage = "IntervalMinutes is required and must be configured in the PeriodicTrigger section")]
    [Range(1, int.MaxValue, ErrorMessage = "IntervalMinutes must be greater than 0")]
    public int IntervalMinutes { get; init; }

    /// <summary>
    /// Gets the interval as a TimeSpan for internal use.
    /// </summary>
    public TimeSpan Interval => TimeSpan.FromMinutes(IntervalMinutes);
}