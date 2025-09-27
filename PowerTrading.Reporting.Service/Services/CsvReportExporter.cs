using System.Text;
using Microsoft.Extensions.Options;
using PowerTrading.Reporting.Service.Models;
using PowerTrading.Reporting.Service.Options;

public class CsvReportExporter(IOptions<CsvExporterOptions> options) : IReportExporter
{
    public async Task Export(PowerPositionReport report)
    {
        EnsureDirectoryExists();

    var filename = $"PowerPosition_report.{report.ReportTimestamp:yyyyMMdd_HHmm}.csv";
        var fullPath = Path.Combine(options.Value.OutputDirectory, filename);

        var sb = new StringBuilder();
        sb.AppendLine("Local Time,Volume");

        foreach (var period in report.Periods)
        {
            sb.AppendLine($"{period.Period:HH:mm},{Math.Round(period.Volume)}");
        }

        await File.WriteAllTextAsync(fullPath, sb.ToString());
    }

    void EnsureDirectoryExists()
    {
        if (!Directory.Exists(options.Value.OutputDirectory))
        {
            Directory.CreateDirectory(options.Value.OutputDirectory);
        }
    }
}