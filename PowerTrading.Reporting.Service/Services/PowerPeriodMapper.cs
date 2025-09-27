using PowerTrading.Reporting.Service.Models;
using Services;

namespace PowerTrading.Reporting.Service.Services;

public class PowerPeriodMapper : IMapper<PowerPeriod, ReportingPowerPeriod>
{
    public ReportingPowerPeriod  Map(PowerPeriod source)
    {
            // Convert period number to hour (periods are hourly starting from 23:00)
            // Period 1 -> 23:00, Period 2 -> 00:00, Period 3 -> 01:00, ..., Period 24 -> 22:00
            var periodHour = source.Period == 1 
                ? 23 
                : source.Period - 2;

            return new ReportingPowerPeriod(periodHour, source.Volume);
    }
}