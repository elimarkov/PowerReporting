using PowerTrading.Reporting.Service.Models;

public interface IReportExporter
{
    Task Export(PowerPositionReport report);
}