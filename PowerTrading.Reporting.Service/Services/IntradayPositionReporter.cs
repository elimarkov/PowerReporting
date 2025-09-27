namespace PowerTrading.Reporting.Service.Services;

/// <summary>
/// Background service that listens for trigger events and generates power position reports
/// </summary>
public class IntradayPositionReporter : BackgroundService
{
    private readonly ITrigger _trigger;
    private readonly IReportGenerator _reportGenerator;
    private readonly IReportExporter _reportExporter;
    private readonly ILogger<IntradayPositionReporter> _logger;

    public IntradayPositionReporter(
        ITrigger trigger,
        IReportGenerator reportGenerator,
        IReportExporter reportExporter,
        ILogger<IntradayPositionReporter> logger)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(reportGenerator);
        ArgumentNullException.ThrowIfNull(reportExporter);
        ArgumentNullException.ThrowIfNull(logger);

        _trigger = trigger;
        _reportGenerator = reportGenerator;
        _reportExporter = reportExporter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IntradayPositionReporter starting");
        
        // Subscribe to trigger events and start the trigger for subsequent reports
        _trigger.Triggered += OnReportTriggered;
        _trigger.Start();
        
        _logger.LogInformation("IntradayPositionReporter started, trigger is now active");

        try
        {
            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("IntradayPositionReporter cancellation requested");
        }
        finally
        {
            // Stop the trigger and unsubscribe from events on shutdown
            _trigger.Stop();
            _trigger.Triggered -= OnReportTriggered;
            _logger.LogInformation("IntradayPositionReporter stopped");
        }
    }

    private async void OnReportTriggered(object? _, TriggerEventArgs e)
    {
        DateTime timestamp = e.TriggeredAt;
        try
        {
            _logger.LogInformation("Generating report for timestamp {Timestamp}", timestamp);
            
            var report = await _reportGenerator.GenerateReportAsync(timestamp);
            await _reportExporter.Export(report);
            
            _logger.LogInformation("Report generated and exported successfully with {PeriodCount} periods for timestamp {ReportTimestamp}", 
                report.Periods.Count, report.ReportTimestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate or export report for timestamp {Timestamp}", timestamp);
        }
    }
}
