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
    /// </summary>
    public int IntervalMinutes { get; init; } = 60;

    /// <summary>
    /// Gets the interval as a TimeSpan for internal use.
    /// </summary>
    public TimeSpan Interval => TimeSpan.FromMinutes(IntervalMinutes);
}