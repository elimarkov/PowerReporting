using Microsoft.Extensions.Logging;
using NSubstitute;
using PowerTrading.Reporting.Service.Models;
using PowerTrading.Reporting.Service.Services;
using Microsoft.Extensions.Time.Testing;
using PowerTrading.Reporting.Service.Options;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public sealed class IntradayPositionReporterTests
{
    private ITrigger _mockTrigger = null!;
    private IReportGenerator _mockReportGenerator = null!;
    private IReportExporter _mockReportExporter = null!;
    private ILogger<IntradayPositionReporter> _mockLogger = null!;
    private ResiliencePipelineProvider<string> _resilienceProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockTrigger = Substitute.For<ITrigger>();
        _mockReportGenerator = Substitute.For<IReportGenerator>();
        _mockReportExporter = Substitute.For<IReportExporter>();
        _mockLogger = Substitute.For<ILogger<IntradayPositionReporter>>();

         _resilienceProvider = Substitute.For<ResiliencePipelineProvider<string>>();
         _resilienceProvider.GetPipeline("ReportGeneration")
            .Returns(ResiliencePipeline.Empty);
    }

    [TestMethod]
    public void Constructor_WithNullTrigger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new IntradayPositionReporter(null!, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger));
    }

    [TestMethod]
    public void Constructor_WithNullReportGenerator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new IntradayPositionReporter(_mockTrigger, null!, _mockReportExporter, _resilienceProvider, _mockLogger));
    }

    [TestMethod]
    public void Constructor_WithNullReportExporter_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, null!, _resilienceProvider, _mockLogger));
    }

    [TestMethod]
    public void Constructor_WithNullResilienceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, null!, _mockLogger));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, null!));
    }


    [TestMethod]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var reporter = new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);

        // Assert
        Assert.IsNotNull(reporter);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldGenerateInitialReportAndStartTrigger()
    {
        // Arrange
        var expectedTime = new DateTime(2025, 9, 27, 12, 0, 0, DateTimeKind.Utc);
        var logger = Substitute.For<ILogger<PeriodicTrigger>>();

        var options = new PeriodicTriggerOptions { IntervalMinutes = 10 };
        var mockOptions = Substitute.For<IOptions<PeriodicTriggerOptions>>();
        mockOptions.Value.Returns(options);
        var mockTimeProvider = new FakeTimeProvider();
        mockTimeProvider.SetUtcNow(expectedTime);

        using var trigger = new PeriodicTrigger(mockTimeProvider, mockOptions, logger);

        var reporter = new IntradayPositionReporter(trigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);
        var expectedInitialReport = new PowerPositionReport(expectedTime, new List<ReportingPowerPeriod>());

        _mockReportGenerator.GenerateReportAsync(expectedTime)
            .Returns(Task.FromResult(expectedInitialReport));

        using var cts = new CancellationTokenSource();

        // Act
        await reporter.StartAsync(cts.Token);

        // Allow some time for ExecuteAsync to set up
        await Task.Delay(50);

        // Assert
        await _mockReportGenerator.Received(1).GenerateReportAsync(expectedTime);

        // Verify initial report generation was logged
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Generating report for timestamp")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [TestMethod]
    public async Task ExecuteAsync_OnCancellation_ShouldStopTriggerAndUnsubscribe()
    {
        // Arrange
        var reporter = new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);
            using var cts = new CancellationTokenSource();
        
        // Act
        var startTask = reporter.StartAsync(cts.Token);
        await startTask;
        
        // Allow ExecuteAsync to set up
        await Task.Delay(50);
        
        // Stop the service
        cts.Cancel();
        var stopTask = reporter.StopAsync(CancellationToken.None);
        await stopTask;
        
        // Assert
        _mockTrigger.Received(1).Start();
        _mockTrigger.Received(1).Stop();
    }

    [TestMethod]
    public async Task OnReportTriggered_ShouldCallReportGeneratorWithCorrectTimestamp()
    {
        // Arrange
        var reporter = new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);
        var triggeredAt = DateTime.UtcNow;
        var expectedReport = new PowerPositionReport(triggeredAt, new List<ReportingPowerPeriod>());
        
        _mockReportGenerator.GenerateReportAsync(triggeredAt)
            .Returns(Task.FromResult(expectedReport));
        
        using var cts = new CancellationTokenSource();
        
        // Start the service to set up event subscription
        await reporter.StartAsync(cts.Token);
        await Task.Delay(50); // Allow setup
        
        // Act - Trigger the event
        _mockTrigger.Triggered += Raise.EventWith(_mockTrigger, new TriggerEventArgs { TriggeredAt = triggeredAt });
        
        // Allow async event handler to complete
        await Task.Delay(100);
        
        // Assert
        await _mockReportGenerator.Received(1).GenerateReportAsync(triggeredAt);
        
        // Cleanup
        cts.Cancel();
        await reporter.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task OnReportTriggered_WithReportGenerationSuccess_ShouldLogSuccess()
    {
        // Arrange
        var reporter = new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);
        var triggeredAt = DateTime.UtcNow;
        var periods = new List<ReportingPowerPeriod>
        {
            new ReportingPowerPeriod(1, 100.5),
            new ReportingPowerPeriod(2, 200.0)
        };
        var expectedReport = new PowerPositionReport(triggeredAt, periods);
        
        _mockReportGenerator.GenerateReportAsync(triggeredAt)
            .Returns(Task.FromResult(expectedReport));
        
        using var cts = new CancellationTokenSource();
        
        // Start the service
        await reporter.StartAsync(cts.Token);
        await Task.Delay(50);
        
        // Act
        _mockTrigger.Triggered += Raise.EventWith(_mockTrigger, new TriggerEventArgs { TriggeredAt = triggeredAt });
        await Task.Delay(100);
        
        // Assert - Verify that Export was called
        await _mockReportExporter.Received(1).Export(expectedReport);
        // Assert - Verify that LogInformation was called with the success message
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Report generated and exported successfully") && o.ToString()!.Contains("2 periods")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
        
        // Cleanup
        cts.Cancel();
        await reporter.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task OnReportTriggered_WithReportGenerationException_ShouldLogError()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero
            })
            .Build();
            
         _resilienceProvider.GetPipeline("ReportGeneration")
            .Returns(pipeline);

        var reporter = new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);
        var triggeredAt = DateTime.UtcNow;
        var expectedException = new InvalidOperationException("Test exception");
        
        // Setup mock to always fail
        _mockReportGenerator
            .GenerateReportAsync(Arg.Is<DateTime>(d => d == triggeredAt))
            .Returns(
                x => Task.FromException<PowerPositionReport>(expectedException),
                x => Task.FromException<PowerPositionReport>(expectedException),
                x => Task.FromException<PowerPositionReport>(expectedException));
        
        using var cts = new CancellationTokenSource();
        
        // Start the service
        await reporter.StartAsync(cts.Token);
        await Task.Delay(50);
        
        // Act
        _mockTrigger.Triggered += Raise.EventWith(_mockTrigger, new TriggerEventArgs { TriggeredAt = triggeredAt });
        await Task.Delay(100); // Small delay for async operations
        
        // Assert - Verify that GenerateReportAsync was called 3 times
        await _mockReportGenerator.Received(4).GenerateReportAsync(triggeredAt);
        await _mockReportExporter.DidNotReceive().Export(Arg.Any<PowerPositionReport>());
        
        // Verify error log is called with final exception
        _mockLogger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to generate or export report")),
            Arg.Is<Exception>(e => e.GetType() == typeof(InvalidOperationException)),
            Arg.Any<Func<object, Exception?, string>>());
        
        // Cleanup
        cts.Cancel();
        await reporter.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldLogStartupAndShutdownMessages()
    {
        // Arrange
        var reporter = new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);
        using var cts = new CancellationTokenSource();
        
        // Act
        await reporter.StartAsync(cts.Token);
        await Task.Delay(50);
        
        cts.Cancel();
        await reporter.StopAsync(CancellationToken.None);
        
        // Assert
        _mockLogger.Received().LogInformation("IntradayPositionReporter starting");
        _mockLogger.Received().LogInformation("IntradayPositionReporter started, trigger is now active");
        _mockLogger.Received().LogInformation("IntradayPositionReporter stopped");
    }

    [TestMethod]
    public async Task ExecuteAsync_WithInitialReportGenerationFailure_ShouldLogErrorAndContinue()
    {
        // Arrange

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero
            })
            .Build();
            
         _resilienceProvider.GetPipeline("ReportGeneration")
            .Returns(pipeline);

        var expectedTime = new DateTime(2025, 9, 27, 12, 0, 0, DateTimeKind.Utc);
        var logger = Substitute.For<ILogger<PeriodicTrigger>>();

        var options = new PeriodicTriggerOptions { IntervalMinutes = 10 };
        var mockOptions = Substitute.For<IOptions<PeriodicTriggerOptions>>();
        mockOptions.Value.Returns(options);
        var mockTimeProvider = new FakeTimeProvider();
        mockTimeProvider.SetUtcNow(expectedTime);

        using var trigger = new PeriodicTrigger(mockTimeProvider, mockOptions, logger);

        var reporter = new IntradayPositionReporter(trigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);

        var expectedException = new InvalidOperationException("Initial report generation failed");
        
        // Setup mock to always fail
        _mockReportGenerator.GenerateReportAsync(Arg.Any<DateTime>())
            .Returns(
                x => Task.FromException<PowerPositionReport>(expectedException),
                x => Task.FromException<PowerPositionReport>(expectedException),
                x => Task.FromException<PowerPositionReport>(expectedException));
        
        using var cts = new CancellationTokenSource();
        
        // Act
        var executeTask = reporter.StartAsync(cts.Token);
        await executeTask;
        
        // Allow time for all retries to complete
        await Task.Delay(100);
        
        // Verify GenerateReportAsync was called 3 times
        await _mockReportGenerator.Received(4).GenerateReportAsync(Arg.Any<DateTime>());
        
        // Verify error was logged after all retries
        _mockLogger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to generate or export report for timestamp")),
            Arg.Is<Exception>(e => e.GetType() == typeof(InvalidOperationException)),
            Arg.Any<Func<object, Exception?, string>>());
        
        // Cleanup
        cts.Cancel();
        await reporter.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task OnReportTriggered_ShouldRetryOnFailure_AndEventuallySucceed()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero
            })
            .Build();
            
         _resilienceProvider.GetPipeline("ReportGeneration")
            .Returns(pipeline);

        var reporter = new IntradayPositionReporter(_mockTrigger, _mockReportGenerator, _mockReportExporter, _resilienceProvider, _mockLogger);
        var triggeredAt = DateTime.UtcNow;
        var periods = new List<ReportingPowerPeriod> { new ReportingPowerPeriod(1, 100.5) };
        var expectedReport = new PowerPositionReport(triggeredAt, periods);
        var exception = new InvalidOperationException("Simulated failure");
        
        // Setup mock to fail twice then succeed
        _mockReportGenerator
            .GenerateReportAsync(Arg.Is<DateTime>(d => d == triggeredAt))
            .Returns(
                x => Task.FromException<PowerPositionReport>(exception),
                x => Task.FromException<PowerPositionReport>(exception),
                x => Task.FromResult(expectedReport));
        
        using var cts = new CancellationTokenSource();
        await reporter.StartAsync(cts.Token);
        await Task.Delay(50);

        // Act
        _mockTrigger.Triggered += Raise.EventWith(_mockTrigger, new TriggerEventArgs { TriggeredAt = triggeredAt });
        await Task.Delay(100); // Small delay for async operations
        
        // Assert
        await _mockReportGenerator.Received(3).GenerateReportAsync(Arg.Is<DateTime>(d => d == triggeredAt));
        await _mockReportExporter.Received(1).Export(expectedReport);
        
        // Verify starting log message
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Generating report for timestamp")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        // Verify success log
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Report generated and exported successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
        
        // Cleanup
        cts.Cancel();
        await reporter.StopAsync(CancellationToken.None);
    }
}