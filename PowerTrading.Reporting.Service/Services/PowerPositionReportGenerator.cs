using PowerTrading.Reporting.Service.Models;
using Services;

namespace PowerTrading.Reporting.Service.Services;

/// <summary>
/// Implementation of IReportGenerator that retrieves power trading data and generates position reports
/// </summary>
public sealed class PowerPositionReportGenerator : IReportGenerator
{
    static TimeZoneInfo londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    private readonly IPowerService _powerService;
    readonly IMapper<PowerPeriod, ReportingPowerPeriod> _mapper;
    private readonly ILogger<PowerPositionReportGenerator> _logger;

    public PowerPositionReportGenerator(IPowerService powerService, IMapper<PowerPeriod, ReportingPowerPeriod> mapper, ILogger<PowerPositionReportGenerator> logger)
    {
        ArgumentNullException.ThrowIfNull(powerService);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(logger);

        _powerService = powerService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PowerPositionReport> GenerateReportAsync(DateTime extractTime)
    {
        try
        {
            // Get power trades for the specified date
            var standardTime = GetPowerTradeDate(extractTime);

            _logger.LogInformation("Generating power position report for extract time: {ExtractTime}", extractTime);

            var trades = await _powerService.GetTradesAsync(standardTime);
            _logger.LogDebug("Retrieved {TradeCount} trades from power service", trades.Count());

            // Aggregate volumes by period
            var aggregatedPeriods = AggregateTradesByPeriod(trades);
            _logger.LogDebug("Aggregated trades into {PeriodCount} periods", aggregatedPeriods.Count);

            var mapped = Map(aggregatedPeriods);
            // Create and return the report - validation and ordering handled by PowerPositionReport
            var report = new PowerPositionReport(extractTime, mapped);
            _logger.LogInformation("Successfully generated power position report with {PeriodCount} periods", report.Periods.Count);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate power position report for extract time: {ExtractTime}", extractTime);
            throw;
        }
    }

    DateTime GetPowerTradeDate(DateTime from)
    {

        var powerTradeDate = from.Hour < 23
                ? from
                : from.AddDays(1);

        return TimeZoneInfo.ConvertTime(powerTradeDate, londonTimeZone);
    }

    IReadOnlyList<PowerPeriod> AggregateTradesByPeriod(IEnumerable<PowerTrade> trades)
    {
        _logger.LogDebug("Starting aggregation of trades by period");

        var periodVolumes = new Dictionary<int, double>();

        foreach (var trade in trades)
        {
            _logger.LogTrace("Processing trade with {PeriodCount} periods", trade.Periods.Length);

            foreach (var period in trade.Periods)
            {
                if (periodVolumes.ContainsKey(period.Period))
                {
                    periodVolumes[period.Period] += period.Volume;
                    _logger.LogTrace("Added volume {Volume} to existing period {Period}, new total: {Total}",
                        period.Volume, period.Period, periodVolumes[period.Period]);
                }
                else
                {
                    periodVolumes[period.Period] = period.Volume;
                    _logger.LogTrace("Created new period {Period} with volume {Volume}", period.Period, period.Volume);
                }
            }
        }
        return periodVolumes
        .Select(kv => new PowerPeriod { Period = kv.Key, Volume = kv.Value })
        .ToList();
    }

    IEnumerable<ReportingPowerPeriod> Map(IEnumerable<PowerPeriod> powerPeriods)
    {
        foreach (var period in powerPeriods)
        {
            yield return _mapper.Map(period);
        }
    }
}