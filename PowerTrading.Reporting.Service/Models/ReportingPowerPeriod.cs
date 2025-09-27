using System.Diagnostics;

namespace PowerTrading.Reporting.Service.Models;

/// <summary>
/// Represents a power trading period with volume data
/// </summary>
[DebuggerDisplay("Period {Period,nq:HH:mm}: {Volume,nq:F2}")]
public record ReportingPowerPeriod : IComparable<ReportingPowerPeriod>
{
    /// <summary>
    /// The time period (e.g., 09:00, 10:00, etc.)
    /// </summary>
    public TimeOnly Period { get; set; }

    /// <summary>
    /// The volume for this period
    /// </summary>
    public double Volume { get; set; }

    public ReportingPowerPeriod(int hour, double volume)
    {
        Period = new TimeOnly(hour, 0);
        Volume = volume;
    }

    /// <summary>
    /// Compares periods using power trading day ordering: 23:00 first, then 00:00-22:00
    /// </summary>
    public int CompareTo(ReportingPowerPeriod? other)
    {
        if (other is null) return 1;

        var thisOrder = GetTradingDayOrder(Period.Hour);
        var otherOrder = GetTradingDayOrder(other.Period.Hour);
        
        return thisOrder.CompareTo(otherOrder);
    }

    private static int GetTradingDayOrder(int hour)
    {
        // 23:00 comes first (order 0), then 00:00 (order 1), 01:00 (order 2), etc.
        return hour == 23 ? 0 : hour + 1;
    }
}