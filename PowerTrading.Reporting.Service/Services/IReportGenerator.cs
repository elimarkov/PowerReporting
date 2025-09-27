using PowerTrading.Reporting.Service.Models;

namespace PowerTrading.Reporting.Service.Services;

public interface IReportGenerator
{
    Task<PowerPositionReport> GenerateReportAsync(DateTime extractTime);
}