using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PowerTrading.Reporting.Service.Options;
using PowerTrading.Reporting.Service.Services;

namespace PowerTrading.Reporting.Service.Tests;

[TestClass]
public sealed class PeriodicReportTriggerTests
{
    private ILogger<PeriodicTrigger> _logger = null!;
    private TimeProvider _mockTimeProvider = null!;
    private IOptions<PeriodicTriggerOptions> _mockOptions = null!;
    private readonly TimeSpan _testInterval = TimeSpan.FromMinutes(1);

    [TestInitialize]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<PeriodicTrigger>>();
        _mockTimeProvider = Substitute.For<TimeProvider>();
        _mockOptions = CreateMockOptions(_testInterval);
    }

    private static IOptions<PeriodicTriggerOptions> CreateMockOptions(TimeSpan interval)
    {
        var options = new PeriodicTriggerOptions { IntervalMinutes = (int)interval.TotalMinutes };
        var mockOptions = Substitute.For<IOptions<PeriodicTriggerOptions>>();
        mockOptions.Value.Returns(options);
        return mockOptions;
    }

    [TestMethod]
    public void Constructor_WhenCalled_ShouldCreateTimer()
    {
        // Arrange
        var mockTimer = Substitute.For<System.Threading.ITimer>();
        _mockTimeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), 
            Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        // Act
        using var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);

        // Assert
        _mockTimeProvider.Received(1).CreateTimer(
            Arg.Any<TimerCallback>(),
            Arg.Any<object?>(),
            _testInterval,
            _testInterval);
    }

    [TestMethod]
    public void Constructor_WithNullTimeProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PeriodicTrigger(null!, _mockOptions, _logger));
    }

    [TestMethod]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PeriodicTrigger(_mockTimeProvider, null!, _logger));
    }

    [TestMethod]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() =>
            new PeriodicTrigger(_mockTimeProvider, _mockOptions, null!));
    }

    [TestMethod]
    public void Constructor_WithZeroInterval_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var zeroIntervalOptions = CreateMockOptions(TimeSpan.Zero);

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new PeriodicTrigger(_mockTimeProvider, zeroIntervalOptions, _logger));
    }

    [TestMethod]
    public void Constructor_WithNegativeInterval_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var negativeIntervalOptions = CreateMockOptions(TimeSpan.FromSeconds(-1));

        // Act & Assert
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new PeriodicTrigger(_mockTimeProvider, negativeIntervalOptions, _logger));
    }

    [TestMethod]
    public void TimerCallback_WhenInvoked_ShouldRaiseReportTriggeredEvent()
    {
        // Arrange
        var mockTimer = Substitute.For<System.Threading.ITimer>();
        TimerCallback? capturedCallback = null;

        _mockTimeProvider.CreateTimer(Arg.Do<TimerCallback>(cb => capturedCallback = cb), 
            Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        var eventRaised = false;
        using var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);
        trigger.Triggered += (sender, args) => eventRaised = true;

        // Act
        capturedCallback?.Invoke(null);

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public void Dispose_WhenCalled_ShouldDisposeTimer()
    {
        // Arrange
        var mockTimer = Substitute.For<System.Threading.ITimer>();
        _mockTimeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), 
            Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);

        // Act
        trigger.Dispose();

        // Assert
        mockTimer.Received(1).Dispose();
    }

    [TestMethod]
    public void TimerCallback_AfterDispose_ShouldNotRaiseEvent()
    {
        // Arrange
        var mockTimer = Substitute.For<System.Threading.ITimer>();
        TimerCallback? capturedCallback = null;

        _mockTimeProvider.CreateTimer(Arg.Do<TimerCallback>(cb => capturedCallback = cb), 
            Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(mockTimer);

        var eventRaised = false;
        var trigger = new PeriodicTrigger(_mockTimeProvider, _mockOptions, _logger);
        trigger.Triggered += (sender, args) => eventRaised = true;
        trigger.Dispose();

        // Act
        capturedCallback?.Invoke(null);

        // Assert
        Assert.IsFalse(eventRaised);
    }
}