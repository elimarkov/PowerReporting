namespace PowerTrading.Reporting.Service.Models;

public class PowerPositionReport
{
    public DateTime ReportTimestamp { get; init; }
    public IReadOnlyList<ReportingPowerPeriod> Periods { get; }
    
    public PowerPositionReport(DateTime reportTimestamp, IEnumerable<ReportingPowerPeriod> periods)
    {
        ArgumentNullException.ThrowIfNull(periods);
        
        ReportTimestamp = reportTimestamp;
        
        var periodsList = periods.ToList();

        // Check for duplicate periods
        var duplicateHours = periodsList
            .GroupBy(p => p.Period.Hour)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateHours.Any())
        {
            var formattedDuplicates = duplicateHours.Select(hour => $"{hour:D2}:00");
            throw new ArgumentException(
                $"Duplicate periods found: {string.Join(", ", formattedDuplicates)}",
                nameof(periods));
        }

        // Store ordered periods - Power trading day starts at 23:00, then 00:00-22:00
        Periods = periodsList
            .OrderBy(p => p)
            .ToList()
            .AsReadOnly();
    }
}